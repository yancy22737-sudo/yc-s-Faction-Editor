using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace FactionGearCustomizer.UI
{
    public class Window_StringFilter : Window
    {
        private List<string> allItems;
        private HashSet<string> selectedItems;
        private HashSet<string> tempSelectedItems;
        private Action onSelectionChanged;
        private string searchText = "";
        private Vector2 scrollPosition;
        private string title;

        public override Vector2 InitialSize => new Vector2(400f, 600f);

        public Window_StringFilter(string title, List<string> allItems, HashSet<string> selectedItems, Action onSelectionChanged)
        {
            this.title = title;
            this.allItems = allItems;
            this.selectedItems = selectedItems;
            this.tempSelectedItems = new HashSet<string>(selectedItems);
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
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), title);
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
            var filteredItems = allItems.Where(m => string.IsNullOrEmpty(searchText) || m.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Buttons
            Rect btnRect = new Rect(0f, y, inRect.width, 30f);
            float btnWidth = (btnRect.width - 10f) / 3f;

            if (Widgets.ButtonText(new Rect(btnRect.x, btnRect.y, btnWidth, btnRect.height), LanguageManager.Get("All")))
            {
                foreach (var item in filteredItems) tempSelectedItems.Add(item);
            }
            if (Widgets.ButtonText(new Rect(btnRect.x + btnWidth + 5f, btnRect.y, btnWidth, btnRect.height), LanguageManager.Get("None")))
            {
                foreach (var item in filteredItems) tempSelectedItems.Remove(item);
            }
            if (Widgets.ButtonText(new Rect(btnRect.x + (btnWidth + 5f) * 2, btnRect.y, btnWidth, btnRect.height), LanguageManager.Get("Invert")))
            {
                foreach (var item in filteredItems)
                {
                    if (tempSelectedItems.Contains(item)) tempSelectedItems.Remove(item);
                    else tempSelectedItems.Add(item);
                }
            }
            y += 35f;

            // List
            Rect listRect = new Rect(0f, y, inRect.width, inRect.height - y - 40f);
            
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, filteredItems.Count * 28f);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            float curY = 0f;
            
            // Optimization: Calculate visible range
            float scrollViewHeight = listRect.height;
            float scrollY = scrollPosition.y;
            
            for (int i = 0; i < filteredItems.Count; i++)
            {
                string item = filteredItems[i];
                if (curY + 28f >= scrollY && curY <= scrollY + scrollViewHeight)
                {
                    bool selected = tempSelectedItems.Contains(item);
                    bool newSelected = selected;
                    
                    Rect rowRect = new Rect(0f, curY, viewRect.width, 24f);
                    Widgets.CheckboxLabeled(rowRect, item, ref newSelected);
                    
                    if (newSelected != selected)
                    {
                        if (newSelected) tempSelectedItems.Add(item);
                        else tempSelectedItems.Remove(item);
                    }
                }
                curY += 28f;
            }
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

        private void ApplyChanges()
        {
            selectedItems.Clear();
            foreach (var item in tempSelectedItems)
            {
                selectedItems.Add(item);
            }
            onSelectionChanged?.Invoke();
        }
    }
}
