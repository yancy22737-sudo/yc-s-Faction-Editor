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
        public bool ShowRangeDamage;
        public bool ShowCategoryFilter;
        public List<string> SortOptions;
        public int IdSeed;
        public Action OnChanged;
        public PickerSearchDebouncer SearchDebouncer;
    }

    public static class ThingPickerFilterBar
    {
        public static float Draw(Rect inRect, float y, ThingPickerFilterState state, ThingPickerFilterBarConfig cfg)
        {
            float height = GetHeight(cfg);
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
            DrawSortRow(listing, state, cfg);
            listing.Gap(4f);
            DrawRanges(listing, state, cfg);

            listing.End();
            return y + height + 6f;
        }

        private static float GetHeight(ThingPickerFilterBarConfig cfg)
        {
            float rows = 4f;
            if (cfg.ShowCategoryFilter) rows += 1f;
            if (cfg.ShowRangeDamage) rows += 2f;
            return rows * 24f + 24f;
        }

        private static void DrawSearch(Listing_Standard listing, ThingPickerFilterState state, ThingPickerFilterBarConfig cfg)
        {
            Rect searchRect = listing.GetRect(24f);
            Rect inputRect = searchRect;

            string currentText = state.SearchText ?? "";
            string next = Widgets.TextField(inputRect, currentText);
            if (string.IsNullOrEmpty(next))
            {
                var anchor = Text.Anchor;
                var color = GUI.color;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                Widgets.Label(new Rect(inputRect.x + 5f, inputRect.y, inputRect.width - 30f, inputRect.height), LanguageManager.Get("Search") + "...");
                GUI.color = color;
                Text.Anchor = anchor;
            }

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
                Rect clearButtonRect = new Rect(inputRect.xMax - 22f, inputRect.y + 4f, 16f, 16f);
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
                string s = state.SelectedMods.First();
                modText = s.Length > 10 ? s.Substring(0, 8) + "..." : s;
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
            DrawRange(listing, cfg.IdSeed + 100, ref state.MarketValue, 0f, Mathf.Max(1f, state.MaxMarketValue), LanguageManager.Get("Value"), ToStringStyle.Money, cfg.OnChanged);
            if (!cfg.ShowRangeDamage) return;
            DrawRange(listing, cfg.IdSeed + 101, ref state.Range, 0f, Mathf.Max(1f, state.MaxRange), LanguageManager.Get("Range"), ToStringStyle.FloatOne, cfg.OnChanged);
            DrawRange(listing, cfg.IdSeed + 102, ref state.Damage, 0f, Mathf.Max(1f, state.MaxDamage), LanguageManager.Get("Damage"), ToStringStyle.FloatOne, cfg.OnChanged);
        }

        private static void DrawRange(Listing_Standard listing, int id, ref FloatRange range, float min, float max, string label, ToStringStyle style, Action onChanged)
        {
            Rect rect = listing.GetRect(24f);
            Rect leftRect = new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height);
            Rect rightRect = new Rect(rect.x + rect.width * 0.4f, rect.y, rect.width * 0.6f, rect.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(leftRect, label + ":");
            Text.Anchor = TextAnchor.UpperLeft;

            FloatRange next = range;
            WidgetsUtils.FloatRange(rightRect, id, ref next, min, max, null, style);
            TooltipHandler.TipRegion(rightRect, LanguageManager.Get("RangeFilterTooltip", label));
            if (Math.Abs(next.min - range.min) > 0.0001f || Math.Abs(next.max - range.max) > 0.0001f)
            {
                range = next;
                onChanged?.Invoke();
            }
        }
    }
}
