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
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), "Mod Filter");
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

            if (Widgets.ButtonText(new Rect(btnRect.x, btnRect.y, btnWidth, btnRect.height), "All"))
            {
                foreach (var mod in filteredMods) tempSelectedMods.Add(mod);
            }
            if (Widgets.ButtonText(new Rect(btnRect.x + btnWidth + 5f, btnRect.y, btnWidth, btnRect.height), "None"))
            {
                foreach (var mod in filteredMods) tempSelectedMods.Remove(mod);
            }
            if (Widgets.ButtonText(new Rect(btnRect.x + (btnWidth + 5f) * 2, btnRect.y, btnWidth, btnRect.height), "Invert"))
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
            foreach (var mod in filteredMods)
            {
                bool selected = tempSelectedMods.Contains(mod);
                bool newSelected = selected;
                
                Rect rowRect = new Rect(0f, curY, viewRect.width, 24f);
                
                // Only draw visible rows
                if (curY + 24f >= scrollPosition.y && curY <= scrollPosition.y + listRect.height)
                {
                    Widgets.CheckboxLabeled(rowRect, mod, ref newSelected);
                }
                
                if (newSelected != selected)
                {
                    if (newSelected) tempSelectedMods.Add(mod);
                    else tempSelectedMods.Remove(mod);
                }
                curY += 28f;
            }
            Widgets.EndScrollView();

            // Bottom Buttons
            float bottomY = inRect.height - 35f;
            float buttonWidth = 120f;
            float spacing = 20f;
            float startX = (inRect.width - (buttonWidth * 2 + spacing)) / 2f;

            if (Widgets.ButtonText(new Rect(startX, bottomY, buttonWidth, 30f), "Apply"))
            {
                ApplyChanges();
                Close();
            }

            if (Widgets.ButtonText(new Rect(startX + buttonWidth + spacing, bottomY, buttonWidth, 30f), "Cancel"))
            {
                Close();
            }
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
