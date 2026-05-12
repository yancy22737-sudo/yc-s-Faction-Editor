using System;
using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.UI;
using FactionGearCustomizer.UI.Utils;
using FactionGearModification.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Pickers
{
    public class ThingPickerFilterBarConfig
    {
        public List<string> AllMods;
        public List<string> AllAmmoSets;
        public bool ShowAmmoFilter;
        public bool ShowFoodFilter;
        public List<string> FoodSubCategories;
        public bool ShowRangeDamage;
        public bool ShowCategoryFilter;
        public bool ShowSortRow = true;
        public List<string> SortOptions;
        public int IdSeed;
        public Action OnChanged;
        public PickerSearchDebouncer SearchDebouncer;
    }

    public static class ThingPickerFilterBar
    {
        public static float Draw(Rect inRect, float y, ThingPickerFilterState state, ThingPickerFilterBarConfig cfg)
        {
            float height = GetHeight(cfg, state);
            Rect rect = new Rect(0f, y, inRect.width, height);
            Widgets.DrawMenuSection(rect);

            Rect inner = rect.ContractedBy(6f);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inner);

            DrawSearch(listing, state, cfg);
            listing.Gap(4f);
            DrawFilterRow(listing, state, cfg);
            listing.Gap(4f);
            if (cfg.ShowCategoryFilter)
            {
                DrawCategoryRow(listing, state, cfg);
                listing.Gap(4f);
            }
            // Hide sort row on page 1 of ammo sub-filter, show on page 2+
            bool showSort = cfg.ShowSortRow && !(state.SelectedCategory == ItemCategoryFilter.Ammo && subFilterPage == 0);
            if (showSort)
            {
                DrawSortRow(listing, state, cfg);
            }
            if (cfg.ShowRangeDamage)
            {
                listing.Gap(4f);
                DrawRanges(listing, state, cfg);
            }

            listing.End();
            return y + height + 6f;
        }

        private static float GetHeight(ThingPickerFilterBarConfig cfg, ThingPickerFilterState state = null)
        {
            bool isAmmo = state != null && state.SelectedCategory == ItemCategoryFilter.Ammo;
            float h = 0f;

            // DrawSearch
            h += 24f;
            h += 4f; // Gap after search

            // DrawFilterRow
            h += 24f;
            h += 4f; // Gap after filter row (line 44 in Draw, always drawn)

            // Category row
            if (cfg.ShowCategoryFilter)
            {
                h += 28f; // CategoryRow uses GetRect(28f)

                // Sub-filter rows (drawn inside DrawCategoryRow)
                if (isAmmo && cfg.ShowAmmoFilter
                    && cfg.AllAmmoSets != null && cfg.AllAmmoSets.Count > 0)
                {
                    h += 2f; // gap between parent and child
                    float defaultWidth = 500f;
                    int subRows = GetSubFilterRowCount(cfg.AllAmmoSets, defaultWidth);
                    h += subRows * 23f; // 20f + 3f gap per row
                    int perPage = GetSubFilterButtonsPerRow(cfg.AllAmmoSets, defaultWidth, MaxSubFilterRows);
                    if (cfg.AllAmmoSets.Count > perPage)
                        h += 20f; // pagination row
                }
                if (!isAmmo && state != null && state.SelectedCategory == ItemCategoryFilter.Food && cfg.ShowFoodFilter
                    && cfg.FoodSubCategories != null && cfg.FoodSubCategories.Count > 0)
                {
                    h += 2f; // gap between parent and child
                    float defaultWidth = 500f;
                    int subRows = GetSubFilterRowCount(cfg.FoodSubCategories, defaultWidth);
                    h += subRows * 23f;
                }

                h += 4f; // Gap after category row (line 48 in Draw, inside if)
            }

            // Sort row
            bool showSort = cfg.ShowSortRow && !(isAmmo && subFilterPage == 0);
            if (showSort)
            {
                h += 4f; // Gap before sort
                h += 24f;
            }

            // DrawRanges (only when ShowRangeDamage)
            if (cfg.ShowRangeDamage)
            {
                h += 4f; // Gap before ranges
                h += 24f; // Range row
                h += 24f; // Damage row
            }

            return h + 12f; // +12 for ContractedBy(6f) top+bottom
        }

        private static void DrawSearch(Listing_Standard listing, ThingPickerFilterState state, ThingPickerFilterBarConfig cfg)
        {
            Rect searchRect = listing.GetRect(24f);
            
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(searchRect.x, searchRect.y, 60f, searchRect.height);
            Widgets.Label(labelRect, LanguageManager.Get("Search") + ":");
            Text.Anchor = TextAnchor.UpperLeft;
            
            Rect inputRect = new Rect(searchRect.x + 65f, searchRect.y, searchRect.width - 90f, searchRect.height);
            
            GUI.SetNextControlName("SearchField");
            string currentText = state.SearchText ?? "";
            string next = Widgets.TextField(inputRect, currentText);
            
            if (next != currentText)
            {
                state.SearchText = next;
                if (cfg.SearchDebouncer != null)
                {
                    cfg.SearchDebouncer.SetSearchText(next);
                }
                else
                {
                    cfg.OnChanged?.Invoke();
                }
            }

            if (!string.IsNullOrEmpty(state.SearchText))
            {
                Rect clearButtonRect = new Rect(inputRect.xMax + 5f, inputRect.y + 4f, 16f, 16f);
                if (Widgets.ButtonImage(clearButtonRect, TexButton.CloseXSmall))
                {
                    state.SearchText = "";
                    if (cfg.SearchDebouncer != null)
                    {
                        cfg.SearchDebouncer.SetSearchText("");
                    }
                    cfg.OnChanged?.Invoke();
                }
                TooltipHandler.TipRegion(clearButtonRect, LanguageManager.Get("ClearSearch"));
            }
        }

        private static void DrawCategoryRow(Listing_Standard listing, ThingPickerFilterState state, ThingPickerFilterBarConfig cfg)
        {
            Rect rowRect = listing.GetRect(28f);

            var categories = new[] {
                (ItemCategoryFilter?)null,
                ItemCategoryFilter.Food,
                ItemCategoryFilter.Medicine,
                ItemCategoryFilter.SocialDrug,
                ItemCategoryFilter.HardDrug,
                ItemCategoryFilter.Ammo
            };
            
            var labels = new[] {
                LanguageManager.Get("CategoryAll"),
                LanguageManager.Get("Category_Food"),
                LanguageManager.Get("Category_Medicine"),
                LanguageManager.Get("Category_SocialDrug"),
                LanguageManager.Get("Category_HardDrug"),
                LanguageManager.Get("Category_Ammo")
            };
            
            float buttonWidth = (rowRect.width - (categories.Length - 1) * 4f) / categories.Length;
            float x = rowRect.x;
            
            for (int i = 0; i < categories.Length; i++)
            {
                Rect btnRect = new Rect(x, rowRect.y, buttonWidth, rowRect.height);
                bool isSelected = state.SelectedCategory == categories[i];
                
                Color bgColor = isSelected ? new Color(0.3f, 0.5f, 0.3f) : new Color(0.2f, 0.2f, 0.22f);
                Widgets.DrawBoxSolid(btnRect, bgColor);
                
                if (isSelected)
                {
                    GUI.color = Color.green;
                    Widgets.DrawBox(btnRect, 1);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.4f, 0.45f);
                    Widgets.DrawBox(btnRect, 1);
                    GUI.color = Color.white;
                }
                
                Text.Anchor = TextAnchor.MiddleCenter;
                Color textColor = isSelected ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                GUI.color = textColor;
                Widgets.Label(btnRect, labels[i]);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                
                if (Widgets.ButtonInvisible(btnRect))
                {
                    if (state.SelectedCategory == categories[i])
                    {
                        state.SelectedCategory = null;
                    }
                    else
                    {
                        state.SelectedCategory = categories[i];
                    }
                    cfg.OnChanged?.Invoke();
                }
                
                TooltipHandler.TipRegion(btnRect, labels[i]);
                x += buttonWidth + 4f;
            }

            // Caliber sub-filter (compact, indented style)
            if (state.SelectedCategory == ItemCategoryFilter.Ammo && cfg.ShowAmmoFilter
                && cfg.AllAmmoSets != null && cfg.AllAmmoSets.Count > 0)
            {
                listing.Gap(2f); // small gap between parent and child
                DrawSubFilterRow(listing, state.SelectedAmmoSets, cfg.AllAmmoSets, () => cfg.OnChanged?.Invoke());
            }

            // Food sub-filter
            if (state.SelectedCategory == ItemCategoryFilter.Food && cfg.ShowFoodFilter
                && cfg.FoodSubCategories != null && cfg.FoodSubCategories.Count > 0)
            {
                listing.Gap(2f);
                DrawSubFilterRow(listing, state.SelectedFoodCategories, cfg.FoodSubCategories, () => cfg.OnChanged?.Invoke());
            }
        }

        private const int MaxSubFilterRows = 3;
        private static int subFilterPage = 0;

        private static float CalcButtonWidth(string text)
        {
            Text.Font = GameFont.Tiny;
            float w = Text.CalcSize(text).x + 12f;
            Text.Font = GameFont.Small;
            return Mathf.Clamp(w, 30f, 140f);
        }

        private static int GetSubFilterRowCount(List<string> options, float rowWidth)
        {
            if (options == null || options.Count == 0) return 0;
            float indent = 16f;
            float gap = 3f;
            string allLabel = LanguageManager.Get("CategoryAll");
            float allBtnW = CalcButtonWidth(allLabel);
            float availableWidth = rowWidth - indent;
            float totalNeeded = allBtnW + gap;
            foreach (var opt in options)
                totalNeeded += CalcButtonWidth(opt) + gap;
            return Mathf.Min(MaxSubFilterRows, Mathf.Max(1, Mathf.CeilToInt(totalNeeded / availableWidth)));
        }

        private static int GetSubFilterButtonsPerRow(List<string> options, float rowWidth, int maxRows)
        {
            if (options == null || options.Count == 0) return 1;
            float indent = 16f;
            float gap = 3f;
            string allLabel = LanguageManager.Get("CategoryAll");
            float allBtnW = CalcButtonWidth(allLabel);
            float avgBtnW = 0f;
            foreach (var opt in options) avgBtnW += CalcButtonWidth(opt);
            avgBtnW /= options.Count;
            float availableWidth = rowWidth - indent - allBtnW - gap;
            int perRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (avgBtnW + gap)));
            return perRow * maxRows;
        }

        private static void DrawSubFilterRow(Listing_Standard listing, HashSet<string> selected, List<string> options, Action onChanged)
        {
            float rowHeight = 20f;
            float gap = 3f;
            float indent = 16f;
            float rowWidth = listing.ColumnWidth;
            string allLabel = LanguageManager.Get("CategoryAll");
            float allBtnW = CalcButtonWidth(allLabel);

            int optionsPerPage = GetSubFilterButtonsPerRow(options, rowWidth, MaxSubFilterRows);
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)options.Count / optionsPerPage));

            if (subFilterPage >= totalPages) subFilterPage = totalPages - 1;
            if (subFilterPage < 0) subFilterPage = 0;

            int startIdx = subFilterPage * optionsPerPage;
            int endIdx = Mathf.Min(startIdx + optionsPerPage, options.Count);

            // Calculate actual rows for visible items
            float totalW = allBtnW + gap;
            for (int i = startIdx; i < endIdx; i++)
                totalW += CalcButtonWidth(options[i]) + gap;
            int rows = Mathf.Min(MaxSubFilterRows, Mathf.Max(1, Mathf.CeilToInt(totalW / (rowWidth - indent))));

            float areaHeight = rows * (rowHeight + gap) + (totalPages > 1 ? 20f : 0f);
            Rect areaRect = listing.GetRect(areaHeight);

            float x = areaRect.x + indent;
            float cy = areaRect.y;

            // "All" button
            bool allSelected = selected.Count == 0;
            DrawSubButton(new Rect(x, cy, allBtnW, rowHeight), LanguageManager.Get("CategoryAll"), allSelected, () =>
            {
                selected.Clear();
                onChanged();
            });
            x += allBtnW + gap;

            // Option buttons (current page)
            for (int i = startIdx; i < endIdx; i++)
            {
                string opt = options[i];
                float btnW = CalcButtonWidth(opt);
                if (x + btnW > areaRect.xMax)
                {
                    x = areaRect.x + indent;
                    cy += rowHeight + gap;
                }
                bool isSelected = selected.Contains(opt);
                DrawSubButton(new Rect(x, cy, btnW, rowHeight), opt, isSelected, () =>
                {
                    if (selected.Contains(opt)) selected.Remove(opt);
                    else selected.Add(opt);
                    onChanged();
                });
                TooltipHandler.TipRegion(new Rect(x, cy, btnW, rowHeight), opt);
                x += btnW + gap;
            }

            // Pagination controls
            if (totalPages > 1)
            {
                float pageY = areaRect.yMax - 18f;
                float pageX = areaRect.x + indent;

                if (subFilterPage > 0)
                {
                    Rect prevBtn = new Rect(pageX, pageY, 30f, 16f);
                    if (Widgets.ButtonText(prevBtn, "<"))
                    {
                        subFilterPage--;
                        onChanged();
                    }
                    pageX += 34f;
                }

                // Page indicator
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.6f, 0.6f, 0.65f);
                Widgets.Label(new Rect(pageX, pageY, 80f, 16f), $"{subFilterPage + 1}/{totalPages}");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                pageX += 84f;

                // Next button
                if (subFilterPage < totalPages - 1)
                {
                    Rect nextBtn = new Rect(pageX, pageY, 30f, 16f);
                    if (Widgets.ButtonText(nextBtn, ">"))
                    {
                        subFilterPage++;
                        onChanged();
                    }
                }
            }
        }

        private static void DrawSubButton(Rect btnRect, string label, bool isSelected, Action onClick)
        {
            // Sub-filter uses darker, more muted colors to distinguish from main categories
            Color bgColor = isSelected ? new Color(0.25f, 0.35f, 0.45f) : new Color(0.17f, 0.17f, 0.19f);
            Widgets.DrawBoxSolid(btnRect, bgColor);

            Color borderColor = isSelected ? new Color(0.4f, 0.55f, 0.75f) : new Color(0.3f, 0.3f, 0.35f);
            GUI.color = borderColor;
            Widgets.DrawBox(btnRect, 1);
            GUI.color = Color.white;

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Color textColor = isSelected ? new Color(0.8f, 0.9f, 1f) : new Color(0.6f, 0.6f, 0.65f);
            GUI.color = textColor;
            Widgets.Label(btnRect, label);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonInvisible(btnRect))
            {
                onClick();
            }
        }

        private static void DrawFilterRow(Listing_Standard listing, ThingPickerFilterState state, ThingPickerFilterBarConfig cfg)
        {
            Rect rowRect = listing.GetRect(24f);

            Rect modRect;
            Rect techRect;
            Rect ammoRect = Rect.zero;

            if (cfg.ShowAmmoFilter)
            {
                WidgetsUtils.SplitRow3(rowRect, 4f, 0.4f, 0.3f, out modRect, out techRect, out ammoRect);
            }
            else
            {
                WidgetsUtils.SplitRow2(rowRect, 4f, 0.55f, out modRect, out techRect);
            }

            string modText;
            if (state.SelectedMods.Count == 0 || state.SelectedMods.Count == (cfg.AllMods?.Count ?? 0))
            {
                modText = LanguageManager.Get("ModAll");
            }
            else if (state.SelectedMods.Count == 1)
            {
                modText = state.SelectedMods.First();
            }
            else
            {
                modText = LanguageManager.Get("ModCountSelected", state.SelectedMods.Count);
            }

            if (Widgets.ButtonText(modRect, modText))
            {
                Find.WindowStack.Add(new Window_ModFilter(cfg.AllMods ?? new List<string>(), state.SelectedMods, cfg.OnChanged));
            }
            TooltipHandler.TipRegion(modRect, LanguageManager.Get("FilterByModTooltip"));

            string techText = !state.SelectedTechLevel.HasValue ? LanguageManager.Get("TechAll") : ((string)("TechLevel_" + state.SelectedTechLevel.Value).Translate());
            if (Widgets.ButtonText(techRect, techText))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(LanguageManager.Get("All"), () => { state.SelectedTechLevel = null; cfg.OnChanged?.Invoke(); }));
                foreach (TechLevel level in Enum.GetValues(typeof(TechLevel)))
                {
                    if (level == TechLevel.Undefined) continue;
                    TechLevel captured = level;
                    options.Add(new FloatMenuOption(((string)("TechLevel_" + captured).Translate()), () => { state.SelectedTechLevel = captured; cfg.OnChanged?.Invoke(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(techRect, LanguageManager.Get("FilterByTechLevelTooltip"));

            if (cfg.ShowAmmoFilter)
            {
                string ammoText;
                if (state.SelectedAmmoSets.Count == 0 || state.SelectedAmmoSets.Count == (cfg.AllAmmoSets?.Count ?? 0))
                {
                    ammoText = LanguageManager.Get("AmmoAll");
                }
                else if (state.SelectedAmmoSets.Count == 1)
                {
                    string s = state.SelectedAmmoSets.First();
                    ammoText = s.Length > 8 ? s.Substring(0, 6) + "..." : s;
                }
                else
                {
                    ammoText = LanguageManager.Get("AmmoCountSelected", state.SelectedAmmoSets.Count);
                }

                if (Widgets.ButtonText(ammoRect, ammoText))
                {
                    Find.WindowStack.Add(new Window_StringFilter(LanguageManager.Get("AmmoFilter"), cfg.AllAmmoSets ?? new List<string>(), state.SelectedAmmoSets, cfg.OnChanged));
                }
                TooltipHandler.TipRegion(ammoRect, LanguageManager.Get("FilterByAmmoTooltip"));
            }
        }

        private static void DrawSortRow(Listing_Standard listing, ThingPickerFilterState state, ThingPickerFilterBarConfig cfg)
        {
            Rect sortRowRect = listing.GetRect(24f);
            Rect sortFieldRect = new Rect(sortRowRect.x, sortRowRect.y, sortRowRect.width - 26f, sortRowRect.height);
            Rect sortOrderRect = new Rect(sortRowRect.xMax - 24f, sortRowRect.y, 24f, sortRowRect.height);

            if (cfg.SortOptions != null && cfg.SortOptions.Count > 0 && !cfg.SortOptions.Contains(state.SortField))
            {
                state.SortField = cfg.SortOptions[0];
            }

            string sortFieldLabel = LanguageManager.Get("SortField_" + state.SortField, state.SortField);
            if (Widgets.ButtonText(sortFieldRect, LanguageManager.Get("SortBy", sortFieldLabel)))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string option in cfg.SortOptions ?? new List<string> { "Name" })
                {
                    string captured = option;
                    string label = LanguageManager.Get("SortField_" + captured, captured);
                    options.Add(new FloatMenuOption(label, () => { state.SortField = captured; cfg.OnChanged?.Invoke(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(sortFieldRect, LanguageManager.Get("SortFieldTooltip"));

            if (Widgets.ButtonText(sortOrderRect, state.SortAscending ? "^" : "v"))
            {
                state.SortAscending = !state.SortAscending;
                cfg.OnChanged?.Invoke();
            }
            TooltipHandler.TipRegion(sortOrderRect, state.SortAscending ? LanguageManager.Get("SortAscending") : LanguageManager.Get("SortDescending"));
        }

        private static void DrawRanges(Listing_Standard listing, ThingPickerFilterState state, ThingPickerFilterBarConfig cfg)
        {
            if (!cfg.ShowRangeDamage) return;
            DrawRange(listing, cfg.IdSeed + 101, ref state.Range, 0f, Mathf.Max(1f, state.MaxRange), LanguageManager.Get("Range"), ToStringStyle.FloatOne, cfg.OnChanged);
            DrawRange(listing, cfg.IdSeed + 102, ref state.Damage, 0f, Mathf.Max(1f, state.MaxDamage), LanguageManager.Get("Damage"), ToStringStyle.FloatOne, cfg.OnChanged);
        }

        private static void DrawRange(Listing_Standard listing, int id, ref FloatRange range, float min, float max, string label, ToStringStyle style, Action onChanged)
        {
            Rect rect = listing.GetRect(24f);
            Rect labelRect = new Rect(rect.x, rect.y, 60f, rect.height);
            Rect valueRect = new Rect(rect.x + 62f, rect.y, rect.width - 62f, rect.height);

            // Label
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            Widgets.Label(labelRect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // Slider
            FloatRange next = range;
            WidgetsUtils.FloatRange(valueRect, id, ref next, min, max, null, style);
            TooltipHandler.TipRegion(valueRect, LanguageManager.Get("RangeFilterTooltip", label));
            if (Math.Abs(next.min - range.min) > 0.0001f || Math.Abs(next.max - range.max) > 0.0001f)
            {
                range = next;
                onChanged?.Invoke();
            }
        }
    }
}
