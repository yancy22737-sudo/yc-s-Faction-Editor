using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI
{
    public class Window_DrawingBoard : Window
    {
        private readonly Action<string> onSave;
        private Texture2D canvas;
        private Color currentColor = Color.white;
        private Color fillColor = Color.white;
        private int brushSize = 9;
        private string iconName;
        private bool isDrawing;
        private bool fillMode;

        private int canvasSize = 128;
        private const int DisplayScale = 2;

        private Stack<Color[]> undoStack = new Stack<Color[]>();
        private const int MaxUndo = 20;

        private static readonly int[] CanvasSizes = { 64, 128, 256 };
        private static readonly int[] BrushSizes = { 1, 3, 5, 9 };

        private static readonly Color[] QuickPalette =
        {
            Color.white, Color.black, Color.red, Color.green, Color.blue,
            Color.yellow, Color.cyan, Color.magenta,
            new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f),
            new Color(1f, 0.9f, 0.8f), new Color(0.55f, 0.27f, 0.07f),
            new Color(0.6f, 0.6f, 0.6f), Color.clear,
        };

        public override Vector2 InitialSize => new Vector2(
            Mathf.Max(480f, canvasSize * DisplayScale + 195f),
            Mathf.Max(420f, canvasSize * DisplayScale + 110f));

        public Window_DrawingBoard(Action<string> onSaveCallback)
        {
            onSave = onSaveCallback;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            iconName = "FactionIcon_" + DateTime.Now.Ticks.ToString("X");
            CreateCanvas();
        }

        private void CreateCanvas()
        {
            if (canvas != null) UnityEngine.Object.Destroy(canvas);
            canvas = new Texture2D(canvasSize, canvasSize, TextureFormat.ARGB32, false);
            canvas.filterMode = FilterMode.Point;
            canvas.wrapMode = TextureWrapMode.Clamp;
            canvas.anisoLevel = 0;
            ClearCanvas();
        }

        private void ClearCanvas()
        {
            Color[] pixels = new Color[canvasSize * canvasSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0, 0, 0, 0);
            canvas.SetPixels(pixels);
            canvas.Apply();
            undoStack.Clear();
        }

        private void PushUndo()
        {
            undoStack.Push(canvas.GetPixels());
            if (undoStack.Count > MaxUndo)
            {
                var arr = undoStack.ToArray();
                undoStack.Clear();
                for (int i = Math.Min(arr.Length - 1, MaxUndo - 1); i >= 0; i--)
                    undoStack.Push(arr[i]);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.control && Event.current.keyCode == KeyCode.Z)
            {
                DoUndo(); Event.current.Use();
            }

            float pad = 10f;
            float colW = inRect.width - pad * 2f;
            float displaySize = canvasSize * DisplayScale;

            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(pad, 4f, colW, 28f), LanguageManager.Get("DrawingBoard"));
            Text.Font = GameFont.Small;

            float contentY = 34f;
            float rightX = pad + displaySize + 14f;
            float rightW = colW - displaySize - 14f;

            // === Canvas ===
            Rect canvasRect = new Rect(pad, contentY, displaySize, displaySize);
            DrawCheckerBackground(canvasRect);
            GUI.DrawTexture(canvasRect, canvas);

            if (Event.current.type == EventType.MouseDown && Mouse.IsOver(canvasRect))
            {
                if (fillMode)
                {
                    PushUndo();
                    FloodFillAtMouse(canvasRect);
                    canvas.Apply();
                    fillMode = false;
                }
                else
                {
                    isDrawing = true; PushUndo();
                    DrawAtMouse(canvasRect); canvas.Apply();
                }
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseDrag && isDrawing && !fillMode && Mouse.IsOver(canvasRect))
            {
                DrawAtMouse(canvasRect); canvas.Apply(); Event.current.Use();
            }
            if (Event.current.type == EventType.MouseUp) isDrawing = false;

            // === Right panel ===
            float ry = contentY;
            float swSz = 18f, swGap = 3f;

            // Canvas size
            Widgets.Label(new Rect(rightX, ry, 100f, 18f), LanguageManager.Get("CanvasSize"));
            ry += 20f;
            float bsx = rightX;
            foreach (int cs in CanvasSizes)
            {
                bool sel = canvasSize == cs;
                Rect b = new Rect(bsx, ry, 34f, 22f);
                if (sel) Widgets.DrawHighlightSelected(b);
                if (Widgets.ButtonText(b, cs.ToString()) && canvasSize != cs)
                {
                    canvasSize = cs; undoStack.Clear(); CreateCanvas();
                    windowRect = new Rect(windowRect.x, windowRect.y,
                        Mathf.Max(480f, canvasSize * DisplayScale + 195f),
                        Mathf.Max(420f, canvasSize * DisplayScale + 110f));
                }
                bsx += 40f;
            }
            ry += 30f;

            // Current color + Fill color in same row
            Widgets.Label(new Rect(rightX, ry, 100f, 18f), LanguageManager.Get("CurrentColor"));
            ry += 20f;

            Rect curColorRect = new Rect(rightX, ry, 36f, 36f);
            Widgets.DrawBoxSolid(curColorRect, currentColor);
            Widgets.DrawBox(curColorRect, 1);
            if (Widgets.ButtonInvisible(curColorRect))
                Find.WindowStack.Add(new Window_ColorPicker(currentColor, c => currentColor = c));

            // Fill color swatch next to current color
            Rect fillColorRect = new Rect(rightX + 44f, ry + 4f, 28f, 28f);
            Widgets.DrawBoxSolid(fillColorRect, fillColor);
            Widgets.DrawBox(fillColorRect, 1);
            if (Widgets.ButtonInvisible(fillColorRect))
                Find.WindowStack.Add(new Window_ColorPicker(fillColor, c => fillColor = c));
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rightX + 78f, ry + 6f, 40f, 22f), "Fill");
            GUI.color = Color.white;

            ry += 44f;

            // Quick palette
            int qpCols = Mathf.Max(3, (int)(rightW / (swSz + swGap)));
            for (int i = 0; i < QuickPalette.Length; i++)
            {
                int col = i % qpCols;
                int row = i / qpCols;
                float sx = rightX + col * (swSz + swGap);
                float sy = ry + row * (swSz + swGap);
                Rect sw = new Rect(sx, sy, swSz, swSz);
                if (QuickPalette[i] == Color.clear)
                    DrawCheckerBackground(sw);
                else
                    Widgets.DrawBoxSolid(sw, QuickPalette[i]);
                Widgets.DrawBox(sw, 1);
                if (Mouse.IsOver(sw)) Widgets.DrawHighlight(sw);
                if (Widgets.ButtonInvisible(sw))
                    currentColor = QuickPalette[i];
            }
            int qpRows = (QuickPalette.Length + qpCols - 1) / qpCols;
            ry += qpRows * (swSz + swGap) + 8f;

            // More Colors
            if (Widgets.ButtonText(new Rect(rightX, ry, 130f, 26f), LanguageManager.Get("MoreColors")))
                Find.WindowStack.Add(new Window_ColorPicker(currentColor, c => currentColor = c));

            ry += 34f;

            // Brush size
            Widgets.Label(new Rect(rightX, ry, 100f, 18f), LanguageManager.Get("BrushSize"));
            ry += 20f;
            bsx = rightX;
            foreach (int bs in BrushSizes)
            {
                bool sel = brushSize == bs;
                Rect b = new Rect(bsx, ry, 32f, 24f);
                if (sel) { Widgets.DrawHighlightSelected(b); GUI.color = Color.white; }
                else GUI.color = new Color(0.4f, 0.4f, 0.45f);
                if (Widgets.ButtonText(b, bs.ToString()))
                    brushSize = bs;
                GUI.color = Color.white;
                bsx += 38f;
            }

            // === Bottom buttons ===
            float btnY = inRect.height - 34f;
            float btnW = 80f;
            float btnGap = 6f;

            if (Widgets.ButtonText(new Rect(pad, btnY, btnW, 28f), LanguageManager.Get("Undo")))
                DoUndo();
            if (Widgets.ButtonText(new Rect(pad + btnW + btnGap, btnY, btnW, 28f), LanguageManager.Get("Clear")))
                ClearCanvas();

            // Fill button in middle — green when active
            float fillBtnX = pad + (btnW + btnGap) * 2;
            if (fillMode)
            {
                GUI.color = new Color(0.3f, 0.7f, 0.3f);
                Widgets.DrawHighlightSelected(new Rect(fillBtnX, btnY, btnW, 28f));
            }
            if (Widgets.ButtonText(new Rect(fillBtnX, btnY, btnW, 28f), LanguageManager.Get("Fill")))
            {
                fillMode = !fillMode;
            }
            GUI.color = Color.white;

            float rightBtnX = inRect.width - pad - btnW * 2 - btnGap;
            if (Widgets.ButtonText(new Rect(rightBtnX, btnY, btnW, 28f), LanguageManager.Get("Apply")))
                SaveAndClose();
            if (Widgets.ButtonText(new Rect(rightBtnX + btnW + btnGap, btnY, btnW, 28f), LanguageManager.Get("Cancel")))
                Close();
        }

        private void FloodFillAtMouse(Rect canvasRect)
        {
            Vector2 mouse = Event.current.mousePosition;
            int cx = Mathf.FloorToInt((mouse.x - canvasRect.x) / DisplayScale);
            int cy = canvasSize - 1 - Mathf.FloorToInt((mouse.y - canvasRect.y) / DisplayScale);
            if (cx < 0 || cx >= canvasSize || cy < 0 || cy >= canvasSize) return;

            Color targetColor = fillColor;
            Color[] pixels = canvas.GetPixels();
            Color targetToReplace = pixels[cy * canvasSize + cx];

            // If same as fill color, skip
            if (ColorsMatch(targetToReplace, targetColor)) return;

            // Stack-based flood fill (4-directional)
            Stack<int> stack = new Stack<int>();
            stack.Push(cx);
            stack.Push(cy);

            while (stack.Count > 0)
            {
                int y = stack.Pop();
                int x = stack.Pop();
                if (x < 0 || x >= canvasSize || y < 0 || y >= canvasSize) continue;
                int idx = y * canvasSize + x;
                if (!ColorsMatch(pixels[idx], targetToReplace)) continue;

                pixels[idx] = targetColor;

                stack.Push(x - 1); stack.Push(y);
                stack.Push(x + 1); stack.Push(y);
                stack.Push(x); stack.Push(y - 1);
                stack.Push(x); stack.Push(y + 1);
            }

            canvas.SetPixels(pixels);
        }

        private static bool ColorsMatch(Color a, Color b)
        {
            return Math.Abs(a.r - b.r) < 0.004f &&
                   Math.Abs(a.g - b.g) < 0.004f &&
                   Math.Abs(a.b - b.b) < 0.004f &&
                   Math.Abs(a.a - b.a) < 0.004f;
        }

        private void DoUndo()
        {
            if (undoStack.Count > 0) { canvas.SetPixels(undoStack.Pop()); canvas.Apply(); }
        }

        private void DrawAtMouse(Rect canvasRect)
        {
            Vector2 mouse = Event.current.mousePosition;
            int px = Mathf.FloorToInt((mouse.x - canvasRect.x) / DisplayScale);
            int py = canvasSize - 1 - Mathf.FloorToInt((mouse.y - canvasRect.y) / DisplayScale);
            if (px >= 0 && px < canvasSize && py >= 0 && py < canvasSize)
                DrawBrush(px, py);
        }

        private void DrawBrush(int cx, int cy)
        {
            int r = Mathf.Max(0, brushSize / 2);
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || x >= canvasSize || y < 0 || y >= canvasSize) continue;

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float maxDist = r + 0.3f;
                    if (dist > maxDist) continue;

                    float strength = 1f - (dist / maxDist);
                    if (currentColor == Color.clear)
                    {
                        Color existing = canvas.GetPixel(x, y);
                        float newA = Mathf.Max(0, existing.a - strength);
                        canvas.SetPixel(x, y, new Color(existing.r, existing.g, existing.b, newA));
                    }
                    else
                    {
                        Color existing = canvas.GetPixel(x, y);
                        float srcA = strength * currentColor.a;
                        float outA = srcA + existing.a * (1 - srcA);
                        if (outA < 0.001f)
                            canvas.SetPixel(x, y, new Color(0, 0, 0, 0));
                        else
                        {
                            Color src = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
                            Color outC = (src * srcA + new Color(existing.r, existing.g, existing.b, 1f) * existing.a * (1f - srcA)) / outA;
                            canvas.SetPixel(x, y, new Color(outC.r, outC.g, outC.b, outA));
                        }
                    }
                }
            }
        }

        private static void DrawCheckerBackground(Rect rect)
        {
            int cSize = 8;
            Color c1 = new Color(0.65f, 0.65f, 0.65f);
            Color c2 = new Color(0.45f, 0.45f, 0.45f);
            GUI.BeginGroup(rect);
            for (float cx = 0; cx < rect.width; cx += cSize)
                for (float cy = 0; cy < rect.height; cy += cSize)
                {
                    bool isC1 = ((int)(cx / cSize) + (int)(cy / cSize)) % 2 == 0;
                    Widgets.DrawBoxSolid(new Rect(cx, cy,
                        Mathf.Min(cSize, rect.width - cx), Mathf.Min(cSize, rect.height - cy)), isC1 ? c1 : c2);
                }
            GUI.EndGroup();
        }

        private void SaveAndClose()
        {
            byte[] pngData = canvas.EncodeToPNG();
            string path = System.IO.Path.Combine(CustomIconManager.SaveDir, iconName + ".png");
            System.IO.File.WriteAllBytes(path, pngData);
            CustomIconManager.GetIcon(iconName);
            onSave?.Invoke("Custom:" + iconName);
            Close();
        }
    }
}
