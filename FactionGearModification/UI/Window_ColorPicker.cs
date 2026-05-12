using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class Window_ColorPicker : Window
    {
        private Color color;
        private readonly Color originalColor;
        private readonly Action<Color> callback;
        private float hue, saturation, value;

        private Texture2D colorFieldTex;
        private Texture2D hueSliderTex;
        private float cachedHueForField = -1f;

        private string bufferR, bufferG, bufferB;

        private static List<Color> recentColors = new List<Color>();
        private const int MaxHistory = 16;

        private const float FieldSize = 170f;
        private const float HueBarWidth = 22f;
        private const float SwatchSize = 18f;
        private const float SwatchGap = 3f;

        private static readonly Color[] CommonPresets =
        {
            new Color(1f, 0f, 0f), new Color(1f, 0.5f, 0f), new Color(1f, 1f, 0f),
            new Color(0f, 1f, 0f), new Color(0f, 1f, 1f), new Color(0f, 0.5f, 1f),
            new Color(0.5f, 0f, 1f), new Color(1f, 0f, 1f), new Color(1f, 0.7f, 0.7f),
            new Color(1f, 0.85f, 0.7f), new Color(1f, 1f, 0.7f), new Color(0.7f, 1f, 0.7f),
            new Color(0.7f, 1f, 1f), new Color(0.7f, 0.85f, 1f), new Color(0.8f, 0.7f, 1f),
            new Color(1f, 0.7f, 1f), new Color(0.55f, 0.27f, 0.07f), new Color(0.8f, 0.6f, 0.4f),
            new Color(1f, 0.9f, 0.8f), new Color(0.9f, 0.8f, 0.7f), new Color(0.7f, 0.55f, 0.4f),
            new Color(0.4f, 0.3f, 0.2f), new Color(0.6f, 0.8f, 0.4f), new Color(0.5f, 0.5f, 0.3f),
            new Color(1f, 1f, 1f), new Color(0.8f, 0.8f, 0.8f), new Color(0.55f, 0.55f, 0.55f),
            new Color(0.35f, 0.35f, 0.35f), new Color(0.15f, 0.15f, 0.15f), new Color(0f, 0f, 0f),
            new Color(0.3f, 0.2f, 0.15f), new Color(0.15f, 0.1f, 0.25f),
        };

        private const int PresetCols = 5;
        private int PresetRows => (CommonPresets.Length + PresetCols - 1) / PresetCols;

        public override Vector2 InitialSize => new Vector2(590f, 420f);

        public Window_ColorPicker(Color initialColor, Action<Color> onSelected)
        {
            originalColor = initialColor;
            color = initialColor;
            callback = onSelected;
            Color.RGBToHSV(color, out hue, out saturation, out value);
            UpdateBuffers();
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            EnsureTextures();
        }

        private void UpdateBuffers()
        {
            bufferR = ((int)(color.r * 255)).ToString();
            bufferG = ((int)(color.g * 255)).ToString();
            bufferB = ((int)(color.b * 255)).ToString();
        }

        private void EnsureTextures()
        {
            if (hueSliderTex == null)
            {
                hueSliderTex = new Texture2D(1, 256) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 256; y++)
                    hueSliderTex.SetPixel(0, 255 - y, Color.HSVToRGB(y / 255f, 1f, 1f));
                hueSliderTex.Apply();
            }
            UpdateColorFieldTex();
        }

        private void UpdateColorFieldTex()
        {
            if (Math.Abs(cachedHueForField - hue) < 0.001f && colorFieldTex != null) return;
            if (colorFieldTex == null)
                colorFieldTex = new Texture2D(256, 256) { wrapMode = TextureWrapMode.Clamp };

            Color baseColor = Color.HSVToRGB(hue, 1f, 1f);
            for (int x = 0; x < 256; x++)
            {
                float s = x / 255f;
                Color saturated = Color.Lerp(Color.white, baseColor, s);
                for (int y = 0; y < 256; y++)
                    colorFieldTex.SetPixel(x, 255 - y, Color.Lerp(Color.black, saturated, 1f - y / 255f));
            }
            colorFieldTex.Apply();
            cachedHueForField = hue;
        }

        private void ApplyColorFromHSV()
        {
            color = Color.HSVToRGB(hue, saturation, value);
            UpdateBuffers();
            UpdateColorFieldTex();
        }

        private void ApplyColorFromRGB()
        {
            Color.RGBToHSV(color, out hue, out saturation, out value);
            UpdateBuffers();
            UpdateColorFieldTex();
        }

        public override void DoWindowContents(Rect inRect)
        {
            float pad = 10f;
            float colW = inRect.width - pad * 2f;

            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(pad, 4f, colW, 28f), LanguageManager.Get("ColorPicker"));
            Text.Font = GameFont.Small;

            // Preview
            float prevY = 32f;
            Widgets.DrawBoxSolid(new Rect(pad, prevY, 64f, 22f), originalColor);
            Widgets.DrawBox(new Rect(pad, prevY, 64f, 22f), 1);
            Widgets.Label(new Rect(pad + 68f, prevY + 2f, 16f, 18f), "→");
            Widgets.DrawBoxSolid(new Rect(pad + 86f, prevY, 120f, 22f), color);
            Widgets.DrawBox(new Rect(pad + 86f, prevY, 120f, 22f), 1);
            Widgets.Label(new Rect(pad + 212f, prevY + 2f, 90f, 18f), "#" + ColorUtility.ToHtmlStringRGB(color));

            // Content area — no scroll, everything fits
            float y = 60f;
            float rightX = FieldSize + HueBarWidth + 18f;

            // --- Color Field ---
            Rect fieldRect = new Rect(pad, y, FieldSize, FieldSize);
            Widgets.DrawBox(fieldRect.ExpandedBy(1f), 1);
            GUI.DrawTexture(fieldRect, colorFieldTex);

            float indX = fieldRect.x + saturation * fieldRect.width;
            float indY = fieldRect.y + (1f - value) * fieldRect.height;
            GUI.color = value > 0.5f ? Color.black : Color.white;
            Widgets.DrawBox(new Rect(indX - 4f, indY - 4f, 8f, 8f), 2);
            GUI.color = Color.white;

            if (Event.current.type == EventType.MouseDown && Mouse.IsOver(fieldRect))
            {
                PickFromField(fieldRect); Event.current.Use();
            }
            if (Event.current.type == EventType.MouseDrag && Mouse.IsOver(fieldRect))
            {
                PickFromField(fieldRect); Event.current.Use();
            }

            // --- Hue Slider ---
            Rect hueRect = new Rect(pad + FieldSize + 8f, y, HueBarWidth, FieldSize);
            Widgets.DrawBox(hueRect.ExpandedBy(1f), 1);
            GUI.DrawTexture(hueRect, hueSliderTex);
            float hueIndY_ = hueRect.y + (1f - hue) * hueRect.height;
            Widgets.DrawBoxSolid(new Rect(hueRect.x - 2f, hueIndY_ - 2f, hueRect.width + 4f, 4f), Color.white);
            Widgets.DrawBox(new Rect(hueRect.x - 2f, hueIndY_ - 2f, hueRect.width + 4f, 4f), 1);

            if (Event.current.type == EventType.MouseDown && Mouse.IsOver(hueRect))
            {
                PickFromHueBar(hueRect); Event.current.Use();
            }
            if (Event.current.type == EventType.MouseDrag && Mouse.IsOver(hueRect))
            {
                PickFromHueBar(hueRect); Event.current.Use();
            }

            // --- Right panel: Presets + History ---
            float rx = pad + rightX;
            float presetGridW = PresetCols * (SwatchSize + SwatchGap);
            float histW = colW - rightX - presetGridW - 8f;

            GUI.color = Color.gray;
            Widgets.Label(new Rect(rx, y, presetGridW, 20f), LanguageManager.Get("ColorPresets"));
            GUI.color = Color.white;

            // History title — always visible
            GUI.color = Color.gray;
            float histTx = rx + presetGridW + 8f;
            Widgets.Label(new Rect(histTx, y, histW, 20f), LanguageManager.Get("ColorHistory"));
            GUI.color = Color.white;

            float ry = y + 22f;

            // Presets grid
            for (int i = 0; i < CommonPresets.Length; i++)
            {
                int row = i / PresetCols;
                int col = i % PresetCols;
                Rect sw = new Rect(rx + col * (SwatchSize + SwatchGap),
                    ry + row * (SwatchSize + SwatchGap), SwatchSize, SwatchSize);
                Widgets.DrawBoxSolid(sw, CommonPresets[i]);
                Widgets.DrawBox(sw, 1);
                if (Mouse.IsOver(sw)) { Widgets.DrawHighlight(sw); Widgets.DrawBox(sw, 1); }
                if (Widgets.ButtonInvisible(sw)) { color = CommonPresets[i]; ApplyColorFromRGB(); }
            }

            // History swatches
            if (recentColors.Count > 0)
            {
                float hy = ry;
                // Calculate how many history cols fit in histW
                int histCols = Mathf.Max(1, (int)(histW / (SwatchSize + SwatchGap)));
                for (int i = 0; i < Mathf.Min(recentColors.Count, MaxHistory); i++)
                {
                    int row = i / histCols;
                    int col = i % histCols;
                    Rect hr = new Rect(histTx + col * (SwatchSize + SwatchGap),
                        hy + row * (SwatchSize + SwatchGap), SwatchSize, SwatchSize);
                    Widgets.DrawBoxSolid(hr, recentColors[i]);
                    Widgets.DrawBox(hr, 1);
                    if (Mouse.IsOver(hr)) { Widgets.DrawHighlight(hr); Widgets.DrawBox(hr, 1); }
                    if (Widgets.ButtonInvisible(hr)) { color = recentColors[i]; ApplyColorFromRGB(); }
                }
            }
            else
            {
                GUI.color = new Color(0.4f, 0.4f, 0.4f);
                Widgets.Label(new Rect(histTx, ry, histW, 20f), "(" + LanguageManager.Get("NoHistory") + ")");
                GUI.color = Color.white;
            }

            // --- RGB Sliders ---
            float sliderY = y + FieldSize + 12f;
            DrawColorSlider(ref sliderY, pad, colW, "R", ref color.r, ref bufferR, Color.red, () => ApplyColorFromRGB());
            DrawColorSlider(ref sliderY, pad, colW, "G", ref color.g, ref bufferG, Color.green, () => ApplyColorFromRGB());
            DrawColorSlider(ref sliderY, pad, colW, "B", ref color.b, ref bufferB, Color.blue, () => ApplyColorFromRGB());

            // Bottom buttons
            float btnW = 110f;
            float btnGap = 16f;
            float btnX = (inRect.width - (btnW * 2 + btnGap)) / 2f;
            float btnY = inRect.height - 36f;
            if (Widgets.ButtonText(new Rect(btnX, btnY, btnW, 30f), LanguageManager.Get("ColorPickerApply")))
            {
                AddToHistory(color);
                callback?.Invoke(color);
                Close();
            }
            if (Widgets.ButtonText(new Rect(btnX + btnW + btnGap, btnY, btnW, 30f), LanguageManager.Get("Cancel")))
                Close();
        }

        private void PickFromField(Rect fieldRect)
        {
            Vector2 mouse = Event.current.mousePosition;
            saturation = Mathf.Clamp01((mouse.x - fieldRect.x) / fieldRect.width);
            value = Mathf.Clamp01(1f - (mouse.y - fieldRect.y) / fieldRect.height);
            ApplyColorFromHSV();
        }

        private void PickFromHueBar(Rect hueRect)
        {
            Vector2 mouse = Event.current.mousePosition;
            hue = Mathf.Clamp01(1f - (mouse.y - hueRect.y) / hueRect.height);
            ApplyColorFromHSV();
        }

        private void DrawColorSlider(ref float y, float x, float width, string label,
            ref float val, ref string buffer, Color tint, Action onChanged)
        {
            Rect lbl = new Rect(x, y, 16f, 24f);
            GUI.color = tint;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(lbl, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect sliderR = new Rect(x + 20f, y + 4f, width - 105f, 16f);
            float newVal = GUI.HorizontalSlider(sliderR, val, 0f, 1f);

            Rect textR = new Rect(sliderR.xMax + 6f, y + 1f, 75f, 22f);
            string newText = Widgets.TextField(textR, buffer);
            if (newText != buffer)
            {
                buffer = newText;
                if (float.TryParse(newText, out float parsed))
                {
                    val = Mathf.Clamp(parsed / 255f, 0f, 1f);
                    onChanged();
                }
            }

            if (Math.Abs(newVal - val) > 0.0001f)
            {
                val = newVal;
                buffer = ((int)(val * 255)).ToString();
                onChanged();
            }
            y += 24f;
        }

        private static void AddToHistory(Color c)
        {
            recentColors.RemoveAll(rc =>
                Math.Abs(rc.r - c.r) < 0.01f &&
                Math.Abs(rc.g - c.g) < 0.01f &&
                Math.Abs(rc.b - c.b) < 0.01f);
            recentColors.Insert(0, c);
            if (recentColors.Count > MaxHistory)
                recentColors.RemoveAt(recentColors.Count - 1);
        }
    }
}
