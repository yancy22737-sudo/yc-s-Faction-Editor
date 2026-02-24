using System;
using System.IO;
using UnityEngine;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.UI
{
    public class Dialog_ImportIcon : Window
    {
        private string filePath = "";
        private Texture2D originalTexture;
        private float scale = 1f;
        private Vector2 offset = Vector2.zero;
        private const int TargetSize = 128; // Final icon size
        private const float ViewportSize = 256f; // Display size in UI
        
        private Action onIconSaved;

        public override Vector2 InitialSize => new Vector2(400f, 550f);

        public Dialog_ImportIcon(Action onSaved)
        {
            this.onIconSaved = onSaved;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            float curY = 0f;

            // Header: Path Input
            Widgets.Label(new Rect(0, curY, inRect.width, 24f), LanguageManager.Get("ImagePathPrompt"));
            curY += 24f;
            
            filePath = Widgets.TextField(new Rect(0, curY, inRect.width - 60f, 30f), filePath);
            if (Widgets.ButtonText(new Rect(inRect.width - 55f, curY, 55f, 30f), LanguageManager.Get("LoadImage")))
            {
                LoadImage(filePath);
            }
            curY += 35f;

            // Error Message
            if (originalTexture == null)
            {
                Widgets.Label(new Rect(0, curY, inRect.width, 24f), LanguageManager.Get("NoImageLoaded"));
            }
            else
            {
                // Viewport (Preview Area)
                Rect viewRect = new Rect((inRect.width - ViewportSize) / 2f, curY, ViewportSize, ViewportSize);
                Widgets.DrawBoxSolid(viewRect, Color.black);
                
                // Draw Image with Scale & Offset
                // We use GUI.BeginGroup to clip the texture
                GUI.BeginGroup(viewRect);
                
                float w = originalTexture.width * scale;
                float h = originalTexture.height * scale;
                
                // Calculate position to center the image based on offset
                // offset (0,0) means center of image is at center of viewport
                float x = (ViewportSize - w) / 2f + offset.x;
                float y = (ViewportSize - h) / 2f + offset.y;
                
                GUI.DrawTexture(new Rect(x, y, w, h), originalTexture);
                
                GUI.EndGroup();
                
                Widgets.DrawBox(viewRect); // Border
                
                // Handle Dragging
                if (Mouse.IsOver(viewRect) && Event.current.type == EventType.MouseDrag)
                {
                    offset += Event.current.delta;
                    Event.current.Use();
                }

                curY += ViewportSize + 10f;

                // Controls
                Widgets.Label(new Rect(0, curY, 50f, 24f), LanguageManager.Get("Scale") + ":");
                scale = Widgets.HorizontalSlider(new Rect(60f, curY + 5f, inRect.width - 70f, 16f), scale, 0.1f, 5f);
                curY += 30f;
                
                if (Widgets.ButtonText(new Rect(0, curY, 100f, 30f), LanguageManager.Get("Reset")))
                {
                    scale = 1f;
                    offset = Vector2.zero;
                }
                
                // Save Button
                if (Widgets.ButtonText(new Rect(inRect.width - 120f, curY, 120f, 30f), LanguageManager.Get("SaveIcon")))
                {
                    SaveIcon();
                }
            }
            
            // Close Button
            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 40f, inRect.height - 40f, 80f, 30f), LanguageManager.Get("Close")))
            {
                Close();
            }
        }

        private void LoadImage(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            
            // Remove quotes if user copied path as "C:\..."
            path = path.Trim('"');
            
            if (File.Exists(path))
            {
                try 
                {
                    byte[] data = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(data))
                    {
                        originalTexture = tex;
                        scale = Math.Min(ViewportSize / tex.width, ViewportSize / tex.height); // Auto fit
                        offset = Vector2.zero;
                        filePath = path; // Update field with cleaned path
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to load image: {ex.Message}");
                }
            }
            else
            {
                Messages.Message(LanguageManager.Get("FileNotFound", path), MessageTypeDefOf.RejectInput, false);
            }
        }

        private void SaveIcon()
        {
            if (originalTexture == null) return;

            try
            {
                // Create target texture
                Texture2D result = new Texture2D(TargetSize, TargetSize, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[TargetSize * TargetSize];
                
                // Center of the source image in UV space
                float centerX = originalTexture.width / 2f;
                float centerY = originalTexture.height / 2f;
                
                // For each pixel in target
                for (int y = 0; y < TargetSize; y++)
                {
                    for (int x = 0; x < TargetSize; x++)
                    {
                        // Map target pixel (x,y) to source pixel
                        // (x - TargetSize/2) is distance from center of target
                        // We divide by scale to get distance in source pixels
                        // We subtract offset (scaled) because moving image right (positive offset) means we sample from left (negative)
                        
                        // Let's think in terms of display space (Viewport) mapped to Target
                        // The Viewport shows a 256x256 area. The Target is 128x128.
                        // So 1 pixel in Target corresponds to (ViewportSize/TargetSize) pixels in Viewport.
                        float ratio = ViewportSize / TargetSize;
                        
                        // Position in Viewport relative to center
                        float viewX = (x - TargetSize / 2f) * ratio;
                        float viewY = (TargetSize / 2f - y) * ratio; // Y is flipped in GUI vs Texture usually? 
                        // Wait, GUI (0,0) is top-left, Texture (0,0) is bottom-left.
                        // In GUI.DrawTexture, y increases downwards.
                        // In Texture2D.GetPixel, y increases upwards.
                        // Let's stick to Texture coordinates logic for sampling.
                        
                        // Let's assume we want to capture exactly what is shown in the viewport center square.
                        // But Viewport is 256, Target is 128. 
                        
                        // Let's re-derive:
                        // We want to sample the original texture such that it matches the visual representation.
                        // Center of original texture is at (w/2, h/2).
                        // In UI, this center is at (Viewport/2 + offset).
                        // We want to sample at UI position (Viewport/2 + relative_pos).
                        // So: UI_Pos = Center_Image_UI + relative_pos
                        // UI_Pos = (Viewport/2 + offset) + relative_pos
                        
                        // Inverse:
                        // Image_Pos_UI = UI_Pos - offset - (Viewport/2 - w/2) ? No.
                        
                        // Let's use UV mapping.
                        // Target UV (0..1)
                        float u = x / (float)TargetSize;
                        float v = y / (float)TargetSize; // 0 at bottom, 1 at top
                        
                        // Convert to centered coords (-0.5 .. 0.5)
                        float u_c = u - 0.5f;
                        float v_c = v - 0.5f;
                        
                        // Apply scale and offset
                        // If scale is 2x, we see 0.5 of the image. So we traverse 0.5 UV units.
                        // So we divide by scale.
                        // Offset: If we moved image right (positive x), we are looking at the left part.
                        // So we subtract offset.
                        // Offset is in screen pixels (Viewport space).
                        // Need to convert offset to UV space.
                        // ViewportSize corresponds to "1.0" in viewport UV.
                        // But we are mapping to Image UV.
                        
                        // Image UV = (Target_UV_Centered / Scale) * (ViewportSize / ImageSize) + 0.5 - (Offset / ImageSize / Scale)
                        
                        // Let's try simpler:
                        // Target pixel (x,y) corresponds to screen offset from center:
                        float screenDX = (x - TargetSize / 2f) * (ViewportSize / TargetSize);
                        float screenDY = (y - TargetSize / 2f) * (ViewportSize / TargetSize); // This is y-up (if y=0 is bottom)
                        
                        // We need to know which pixel of originalTexture is at (Center + screenDX, Center + screenDY)
                        // The image is drawn at:
                        // X = (Viewport - w*scale)/2 + offset.x
                        // Y = (Viewport - h*scale)/2 + offset.y (This is Top-Left in GUI)
                        
                        // Center of image in GUI:
                        // CenterX = X + w*scale/2 = Viewport/2 + offset.x
                        // CenterY = Y + h*scale/2 = Viewport/2 + offset.y
                        
                        // We want the pixel that is at GUI coordinates:
                        // GUI_X = Viewport/2 + screenDX
                        // GUI_Y = Viewport/2 - screenDY (GUI is y-down)
                        
                        // Relation:
                        // GUI_X = CenterX + (Image_X - Image_Width/2) * scale
                        // (Image_X - Image_Width/2) * scale = GUI_X - CenterX
                        // Image_X = (GUI_X - CenterX) / scale + Image_Width/2
                        
                        // Substitute:
                        // Image_X = (Viewport/2 + screenDX - (Viewport/2 + offset.x)) / scale + Image_Width/2
                        // Image_X = (screenDX - offset.x) / scale + Image_Width/2
                        
                        // Similarly for Y (GUI y-down):
                        // Image_Y (from top) = (screenDY_GUI - offset.y) / scale + Image_Height/2
                        // But Texture2D uses y-up.
                        // GUI Y (0..256) maps to Texture Y (h..0).
                        
                        // Let's use GetPixel (x, y-up).
                        // GUI_Y for target pixel y (0..127, 0 is bottom):
                        // GUI_Y = Viewport/2 + (TargetSize/2 - y) * (Viewport/TargetSize)  [Wait, y=0 -> bottom -> GUI_Y large]
                        // Let's verify: y=0 (bottom) -> screenDY = -64. GUI_Y = 128 + 64*2 = 256 (Bottom). Correct.
                        // y=127 (top) -> screenDY = 63. GUI_Y = 128 - 63*2 = 2. (Top). Correct.
                        
                        float gui_x = ViewportSize / 2f + (x - TargetSize / 2f) * (ViewportSize / TargetSize);
                        float gui_y = ViewportSize / 2f - (y - TargetSize / 2f) * (ViewportSize / TargetSize);
                        
                        // Image Top-Left in GUI:
                        float img_x0 = (ViewportSize - originalTexture.width * scale) / 2f + offset.x;
                        float img_y0 = (ViewportSize - originalTexture.height * scale) / 2f + offset.y;
                        
                        // Relative to Image Top-Left:
                        float rel_x = gui_x - img_x0;
                        float rel_y = gui_y - img_y0;
                        
                        // Unscale:
                        float src_x = rel_x / scale;
                        float src_y = rel_y / scale;
                        
                        // Flip Y for Texture2D (src_y is from top, we need from bottom):
                        float actual_src_y = originalTexture.height - src_y;
                        
                        // Sample
                        Color col = Color.clear;
                        if (src_x >= 0 && src_x < originalTexture.width && actual_src_y >= 0 && actual_src_y < originalTexture.height)
                        {
                            col = originalTexture.GetPixelBilinear(src_x / originalTexture.width, actual_src_y / originalTexture.height);
                        }
                        
                        pixels[y * TargetSize + x] = col;
                    }
                }
                
                result.SetPixels(pixels);
                result.Apply();
                
                // Save
                string filename = "CustomIcon_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                CustomIconManager.SaveIcon(result, filename);
                
                Messages.Message(LanguageManager.Get("IconSaved"), MessageTypeDefOf.PositiveEvent, false);
                onIconSaved?.Invoke();
                Close();
            }
            catch (Exception ex)
            {
                Log.Error("Error saving icon: " + ex);
                Messages.Message(LanguageManager.Get("IconSaveError"), MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
