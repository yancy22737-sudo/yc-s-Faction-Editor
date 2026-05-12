using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace FactionGearCustomizer.UI
{
    public class Window_ModFilter : Window
    {
        private List<string> allMods;
        private HashSet<string> selectedMods;
        private HashSet<string> tempSelectedMods;
        private Action onSelectionChanged;
        private string searchText = "";
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(400f, 600f);

        public Window_ModFilter(List<string> allMods, HashSet<string> selectedMods, Action onSelectionChanged)
        {
            this.allMods = allMods;
            this.selectedMods = selectedMods;
            this.tempSelectedMods = new HashSet<string>(selectedMods);
            this.onSelectionChanged = onSelectionChanged;
            this.doCloseX = true;
            this.forcePause = false;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.resizeable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Handle Ctrl+S shortcut for applying changes
            if (Event.current.type == EventType.KeyDown && Event.current.control && Event.current.keyCode == KeyCode.S)
            {
                ApplyChanges();
                Close();
                Event.current.Use();
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), LanguageManager.Get("ModFilter"));
            Text.Font = GameFont.Small;

            // Search
            float y = 40f;
            Rect searchRect = new Rect(0f, y, inRect.width, 30f);
            string newSearch = Widgets.TextField(searchRect, searchText);
            if (newSearch != searchText)
            {
                searchText = newSearch;
            }
            y += 35f;

            // Filter list
            var filteredMods = allMods.Where(m => string.IsNullOrEmpty(searchText) || m.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Buttons
            Rect btnRect = new Rect(0f, y, inRect.width, 30f);
            float btnWidth = (btnRect.width - 10f) / 3f;

            if (Widgets.ButtonText(new Rect(btnRect.x, btnRect.y, btnWidth, btnRect.height), LanguageManager.Get("All")))
            {
                foreach (var mod in filteredMods) tempSelectedMods.Add(mod);
            }
            if (Widgets.ButtonText(new Rect(btnRect.x + btnWidth + 5f, btnRect.y, btnWidth, btnRect.height), LanguageManager.Get("None")))
            {
                foreach (var mod in filteredMods) tempSelectedMods.Remove(mod);
            }
            if (Widgets.ButtonText(new Rect(btnRect.x + (btnWidth + 5f) * 2, btnRect.y, btnWidth, btnRect.height), LanguageManager.Get("Invert")))
            {
                foreach (var mod in filteredMods)
                {
                    if (tempSelectedMods.Contains(mod)) tempSelectedMods.Remove(mod);
                    else tempSelectedMods.Add(mod);
                }
            }
            y += 35f;

            // List
            Rect listRect = new Rect(0f, y, inRect.width, inRect.height - y - 40f);

            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, filteredMods.Count * 28f);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            float curY = 0f;
            Text.WordWrap = false;
            foreach (var mod in filteredMods)
            {
                bool selected = tempSelectedMods.Contains(mod);
                bool newSelected = selected;

                Rect rowRect = new Rect(0f, curY, viewRect.width, 24f);

                // Only draw visible rows
                if (curY + 24f >= scrollPosition.y && curY <= scrollPosition.y + listRect.height)
                {
                    // Manual checkbox + truncated label to keep single-line with "..."
                    Rect cbRect = new Rect(rowRect.x, rowRect.y + 2f, 24f, 20f);
                    Widgets.Checkbox(new Vector2(cbRect.x, cbRect.y), ref newSelected, 20f);

                    float labelX = cbRect.xMax + 4f;
                    float labelMaxWidth = rowRect.xMax - labelX;
                    string displayText = TruncateWithEllipsis(mod, labelMaxWidth);

                    Rect labelRect = new Rect(labelX, rowRect.y, labelMaxWidth, rowRect.height);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(labelRect, displayText);
                    Text.Anchor = TextAnchor.UpperLeft;

                    if (Widgets.ButtonInvisible(rowRect))
                    {
                        newSelected = !newSelected;
                    }
                }

                if (newSelected != selected)
                {
                    if (newSelected) tempSelectedMods.Add(mod);
                    else tempSelectedMods.Remove(mod);
                }
                curY += 28f;
            }
            Text.WordWrap = true;
            Widgets.EndScrollView();

            // Bottom Buttons
            float bottomY = inRect.height - 35f;
            float buttonWidth = 120f;
            float spacing = 20f;
            float startX = (inRect.width - (buttonWidth * 2 + spacing)) / 2f;

            if (Widgets.ButtonText(new Rect(startX, bottomY, buttonWidth, 30f), LanguageManager.Get("Apply")))
            {
                ApplyChanges();
                Close();
            }

            if (Widgets.ButtonText(new Rect(startX + buttonWidth + spacing, bottomY, buttonWidth, 30f), LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }

        private static string TruncateWithEllipsis(string text, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0f) return text ?? "";
            if (Text.CalcSize(text).x <= maxWidth) return text;

            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (Text.CalcSize(text.Substring(0, mid) + "...").x <= maxWidth)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo > 0 ? text.Substring(0, lo) + "..." : "...";
        }

        private void ApplyChanges()
        {
            selectedMods.Clear();
            foreach (var mod in tempSelectedMods)
            {
                selectedMods.Add(mod);
            }
            onSelectionChanged?.Invoke();
        }
    }
}
