using System;
using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.UI.Pickers;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI
{
    public class Dialog_ApparelPicker : Window
    {
        private readonly Action<ThingDef> onPickSingle;
        private readonly List<SpecRequirementEdit> targetList;
        private readonly bool multiSelect;

        private List<ThingDef> allCandidates = new List<ThingDef>();
        private List<ThingDef> filteredCandidates = new List<ThingDef>();
        private HashSet<ThingDef> selected = new HashSet<ThingDef>();

        private List<string> allMods = new List<string>();
        private HashSet<string> existingDefNames;

        private ThingPickerFilterState filterState;
        private bool filterDirty = true;
        private Vector2 scrollPos;

        private IntRange defaultCountRange = new IntRange(1, 1);
        private ApparelSelectionMode defaultMode = ApparelSelectionMode.AlwaysTake;
        private float defaultChance = 1f;
        private bool skipExisting = true;

        private const float RowHeight = 28f;

        public override Vector2 InitialSize => new Vector2(780f, 760f);

        public Dialog_ApparelPicker(List<SpecRequirementEdit> targetList)
        {
            this.targetList = targetList;
            multiSelect = true;
            InitCommon();
        }

        public Dialog_ApparelPicker(Action<ThingDef> onPickSingle)
        {
            this.onPickSingle = onPickSingle;
            multiSelect = false;
            InitCommon();
        }

        private void InitCommon()
        {
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            draggable = true;
            resizeable = true;
            filterState = PickerSession.Apparel;
            BuildCandidates();
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 35f), multiSelect ? LanguageManager.Get("ApparelPickerTitle") : LanguageManager.Get("SelectItem"));
            Text.Font = GameFont.Small;
            y += 40f;

            EnsureFilterUpToDate();
            y = ThingPickerFilterBar.Draw(inRect, y, filterState, new ThingPickerFilterBarConfig
            {
                AllMods = allMods,
                AllAmmoSets = null,
                ShowAmmoFilter = false,
                ShowRangeDamage = false,
                SortOptions = GetSortOptions(),
                IdSeed = GetHashCode(),
                OnChanged = OnFilterChanged
            });

            if (multiSelect) DrawDefaultsRow(inRect, ref y);
            if (multiSelect) DrawSelectionRow(inRect, ref y);

            float bottomHeight = multiSelect ? 40f : 36f;
            Rect listRect = new Rect(0f, y, inRect.width, inRect.height - y - bottomHeight);
            DrawList(listRect);

            if (multiSelect) DrawBottomButtonsMulti(inRect);
            else DrawBottomButtonsSingle(inRect);
        }

        private List<string> GetSortOptions()
        {
            return new List<string> { "Name", "MarketValue", "TechLevel", "ModSource", "Armor_Sharp", "Armor_Blunt" };
        }

        private void DrawDefaultsRow(Rect inRect, ref float y)
        {
            Rect rowRect = new Rect(0f, y, inRect.width, 28f);
            float third = (rowRect.width - 10f) / 3f;

            Rect countRect = new Rect(rowRect.x, rowRect.y, third, rowRect.height);
            Rect modeRect = new Rect(countRect.xMax + 5f, rowRect.y, third, rowRect.height);
            Rect chanceRect = new Rect(modeRect.xMax + 5f, rowRect.y, third, rowRect.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(countRect, LanguageManager.Get("DefaultCountRange") + ":");
            Text.Anchor = TextAnchor.UpperLeft;

            Rect countSliderRect = new Rect(countRect.x + 120f, countRect.y, countRect.width - 120f, countRect.height);
            Widgets.IntRange(countSliderRect, GetHashCode(), ref defaultCountRange, 1, 500);

            if (Widgets.ButtonText(modeRect, LanguageManager.Get("DefaultSelectionMode", GetModeLabel(defaultMode))))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (ApparelSelectionMode m in Enum.GetValues(typeof(ApparelSelectionMode)))
                {
                    ApparelSelectionMode mode = m;
                    options.Add(new FloatMenuOption(GetModeLabel(mode), () => defaultMode = mode));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (defaultMode == ApparelSelectionMode.AlwaysTake)
            {
                GUI.color = Color.gray;
                Widgets.Label(chanceRect, LanguageManager.Get("DefaultChance") + ": 100%");
                GUI.color = Color.white;
            }
            else
            {
                defaultChance = Widgets.HorizontalSlider(chanceRect, defaultChance, 0f, 1f, true, LanguageManager.Get("DefaultChance") + ": " + defaultChance.ToString("P0"));
            }

            y += 34f;
        }

        private void DrawSelectionRow(Rect inRect, ref float y)
        {
            Rect rowRect = new Rect(0f, y, inRect.width, 28f);

            float btnW = 110f;
            Rect allRect = new Rect(rowRect.x, rowRect.y, btnW, rowRect.height);
            Rect noneRect = new Rect(allRect.xMax + 6f, rowRect.y, btnW, rowRect.height);
            Rect invertRect = new Rect(noneRect.xMax + 6f, rowRect.y, btnW, rowRect.height);

            if (Widgets.ButtonText(allRect, LanguageManager.Get("SelectAll")))
            {
                selected.Clear();
                foreach (var def in filteredCandidates) selected.Add(def);
            }
            if (Widgets.ButtonText(noneRect, LanguageManager.Get("SelectNone")))
            {
                selected.Clear();
            }
            if (Widgets.ButtonText(invertRect, LanguageManager.Get("Invert")))
            {
                HashSet<ThingDef> next = new HashSet<ThingDef>();
                foreach (var def in filteredCandidates)
                {
                    if (!selected.Contains(def)) next.Add(def);
                }
                selected = next;
            }

            Rect dupRect = new Rect(invertRect.xMax + 18f, rowRect.y, 240f, rowRect.height);
            Widgets.CheckboxLabeled(dupRect, LanguageManager.Get("SkipExistingItems"), ref skipExisting);

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rowRect.xMax - 220f, rowRect.y, 220f, rowRect.height), $"{LanguageManager.Get("Selected")}: {selected.Count}");
            Text.Anchor = TextAnchor.UpperLeft;

            y += 34f;
        }

        private void DrawList(Rect listRect)
        {
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, filteredCandidates.Count * RowHeight);
            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);

            int first = Mathf.Max(0, Mathf.FloorToInt(scrollPos.y / RowHeight) - 1);
            int visible = Mathf.CeilToInt(listRect.height / RowHeight) + 3;
            int last = Mathf.Min(filteredCandidates.Count, first + visible);

            for (int i = first; i < last; i++)
            {
                ThingDef def = filteredCandidates[i];
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);

                if (i % 2 == 1) Widgets.DrawAltRect(rowRect);
                if (Mouse.IsOver(rowRect)) Widgets.DrawHighlight(rowRect);

                DrawRow(rowRect, def);
            }

            Widgets.EndScrollView();
        }

        private void DrawRow(Rect rowRect, ThingDef def)
        {
            string modName = FactionGearManager.GetModSource(def);
            string tip = $"{def.LabelCap}\n{def.defName}\n{modName}";
            TooltipHandler.TipRegion(rowRect, tip);

            Rect inner = rowRect.ContractedBy(4f, 2f);

            float x = inner.x;
            if (multiSelect)
            {
                Rect cbRect = new Rect(x, inner.y, 24f, 24f);
                bool isSelected = selected.Contains(def);
                bool old = isSelected;
                Widgets.Checkbox(new Vector2(cbRect.x, cbRect.y), ref isSelected);
                if (isSelected != old)
                {
                    if (isSelected) selected.Add(def);
                    else selected.Remove(def);
                }
                x = cbRect.xMax + 6f;
            }

            Rect iconRect = new Rect(x, inner.y + 2f, 20f, 20f);
            if (def.uiIcon != null) Widgets.DrawTextureFitted(iconRect, def.uiIcon, 1f);
            x = iconRect.xMax + 8f;

            if (Current.Game != null)
            {
                Widgets.InfoCardButton(x, inner.y, def);
                x += 28f;
            }

            Rect labelRect = new Rect(x, inner.y, inner.width - (x - inner.x), inner.height);
            Rect rightRect = labelRect.RightPartPixels(210f);
            Rect leftRect = new Rect(labelRect.x, labelRect.y, labelRect.width - 210f, labelRect.height);

            Widgets.Label(leftRect, def.LabelCap.ToString());

            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = Color.gray;
            Widgets.Label(rightRect, modName);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            if (multiSelect && existingDefNames != null && existingDefNames.Contains(def.defName))
            {
                Rect tagRect = new Rect(rowRect.xMax - 88f, rowRect.y + 4f, 80f, 20f);
                GUI.color = new Color(1f, 1f, 1f, 0.55f);
                Widgets.Label(tagRect, LanguageManager.Get("AlreadyAdded"));
                GUI.color = Color.white;
            }

            if (Widgets.ButtonInvisible(rowRect))
            {
                if (!multiSelect)
                {
                    onPickSingle?.Invoke(def);
                    Close();
                }
                else
                {
                    if (selected.Contains(def)) selected.Remove(def);
                    else selected.Add(def);
                }
            }
        }

        private void DrawBottomButtonsMulti(Rect inRect)
        {
            Rect bottom = new Rect(0f, inRect.height - 36f, inRect.width, 36f);
            float btnW = 180f;
            float gap = 14f;
            float x = bottom.xMax - (btnW * 2 + gap);

            Rect addRect = new Rect(x, bottom.y, btnW, 32f);
            Rect closeRect = new Rect(addRect.xMax + gap, bottom.y, btnW, 32f);

            if (Widgets.ButtonText(addRect, LanguageManager.Get("AddSelected")))
            {
                AddSelectedToTarget();
            }
            if (Widgets.ButtonText(closeRect, LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }

        private void DrawBottomButtonsSingle(Rect inRect)
        {
            Rect bottom = new Rect(0f, inRect.height - 34f, inRect.width, 34f);
            float btnW = 180f;
            Rect closeRect = new Rect(bottom.xMax - btnW, bottom.y, btnW, 30f);
            if (Widgets.ButtonText(closeRect, LanguageManager.Get("Cancel")))
            {
                Close();
            }
        }

        private void AddSelectedToTarget()
        {
            if (targetList == null || selected.Count == 0) return;

            if (existingDefNames == null)
            {
                existingDefNames = new HashSet<string>(targetList.Where(x => x?.Thing != null).Select(x => x.Thing.defName));
            }

            int added = 0;
            int skipped = 0;
            foreach (var def in selected.ToList())
            {
                if (def == null) continue;
                if (skipExisting && existingDefNames.Contains(def.defName))
                {
                    skipped++;
                    continue;
                }

                targetList.Add(new SpecRequirementEdit
                {
                    Thing = def,
                    CountRange = defaultCountRange,
                    SelectionMode = defaultMode,
                    SelectionChance = defaultChance
                });
                existingDefNames.Add(def.defName);
                added++;
            }

            if (added > 0)
            {
                FactionGearEditor.MarkDirty();
            }

            Messages.Message(LanguageManager.Get("ApparelItemsAddedMessage", added, skipped), MessageTypeDefOf.PositiveEvent, false);
            selected.Clear();
        }

        private void BuildCandidates()
        {
            allCandidates = DefDatabase<ThingDef>.AllDefs
                .Where(t => t != null && t.IsApparel)
                .OrderBy(t => t.LabelCap.ToString() ?? t.defName)
                .ToList();

            allMods = allCandidates.Select(FactionGearManager.GetModSource).Distinct().OrderBy(x => x).ToList();
            filterState.SyncAvailableMods(allMods);

            if (multiSelect && targetList != null)
            {
                existingDefNames = new HashSet<string>(targetList.Where(x => x?.Thing != null).Select(x => x.Thing.defName));
            }

            filterDirty = true;
            EnsureFilterUpToDate();
        }

        private void OnFilterChanged()
        {
            scrollPos = Vector2.zero;
            filterDirty = true;
        }

        private void EnsureFilterUpToDate()
        {
            if (!filterDirty) return;
            RecalculateBounds();
            filteredCandidates = ApplyFilterAndSort();
            filterDirty = false;
        }

        private void RecalculateBounds()
        {
            float oldMax = filterState.MaxMarketValue;
            bool wasFull = filterState.MarketValue.min <= 0.0001f && Math.Abs(filterState.MarketValue.max - oldMax) <= 0.01f;

            IEnumerable<ThingDef> source = allCandidates;
            if (filterState.SelectedMods.Count > 0 && filterState.SelectedMods.Count != allMods.Count)
            {
                source = source.Where(d => filterState.SelectedMods.Contains(FactionGearManager.GetModSource(d)));
            }
            if (filterState.SelectedTechLevel.HasValue)
            {
                TechLevel level = filterState.SelectedTechLevel.Value;
                source = source.Where(d => d.techLevel == level);
            }

            float nextMax = 1f;
            if (source.Any())
            {
                nextMax = Mathf.Max(1f, source.Max(d => d.BaseMarketValue));
            }
            filterState.MaxMarketValue = nextMax;

            if (wasFull)
            {
                filterState.MarketValue = new FloatRange(0f, filterState.MaxMarketValue);
            }
            filterState.ClampRanges();
        }

        private List<ThingDef> ApplyFilterAndSort()
        {
            IEnumerable<ThingDef> items = allCandidates;
            if (filterState.SelectedMods.Count > 0 && filterState.SelectedMods.Count != allMods.Count)
            {
                items = items.Where(d => filterState.SelectedMods.Contains(FactionGearManager.GetModSource(d)));
            }
            if (filterState.SelectedTechLevel.HasValue)
            {
                TechLevel level = filterState.SelectedTechLevel.Value;
                items = items.Where(d => d.techLevel == level);
            }

            items = items.Where(d => d.BaseMarketValue >= filterState.MarketValue.min && d.BaseMarketValue <= filterState.MarketValue.max);

            string term = (filterState.SearchText ?? "").Trim();
            if (!string.IsNullOrEmpty(term))
            {
                string lower = term.ToLowerInvariant();
                items = items.Where(def =>
                {
                    if (def == null) return false;
                    string mod = FactionGearManager.GetModSource(def) ?? "";
                    if ((def.LabelCap.ToString() ?? "").ToLowerInvariant().Contains(lower)) return true;
                    if ((def.defName ?? "").ToLowerInvariant().Contains(lower)) return true;
                    if (mod.ToLowerInvariant().Contains(lower)) return true;
                    return false;
                });
            }

            return Sort(items).ToList();
        }

        private IEnumerable<ThingDef> Sort(IEnumerable<ThingDef> items)
        {
            switch (filterState.SortField)
            {
                case "Name":
                    return filterState.SortAscending ? items.OrderBy(t => t.LabelCap.ToString() ?? t.defName) : items.OrderByDescending(t => t.LabelCap.ToString() ?? t.defName);
                case "MarketValue":
                    return filterState.SortAscending ? items.OrderBy(t => t.BaseMarketValue) : items.OrderByDescending(t => t.BaseMarketValue);
                case "TechLevel":
                    return filterState.SortAscending ? items.OrderBy(t => (int)t.techLevel) : items.OrderByDescending(t => (int)t.techLevel);
                case "ModSource":
                    return filterState.SortAscending ? items.OrderBy(t => FactionGearManager.GetModSource(t)) : items.OrderByDescending(t => FactionGearManager.GetModSource(t));
                case "Armor_Sharp":
                    return SortBy(items, FactionGearManager.GetArmorRatingSharp);
                case "Armor_Blunt":
                    return SortBy(items, FactionGearManager.GetArmorRatingBlunt);
                default:
                    return filterState.SortAscending ? items.OrderBy(t => t.LabelCap.ToString() ?? t.defName) : items.OrderByDescending(t => t.LabelCap.ToString() ?? t.defName);
            }
        }

        private IEnumerable<ThingDef> SortBy(IEnumerable<ThingDef> items, Func<ThingDef, float> selector)
        {
            var projected = items.Select(t => new { t, v = selector(t) });
            return filterState.SortAscending ? projected.OrderBy(x => x.v).Select(x => x.t) : projected.OrderByDescending(x => x.v).Select(x => x.t);
        }

        private static string GetModeLabel(ApparelSelectionMode mode)
        {
            if (mode == ApparelSelectionMode.AlwaysTake) return LanguageManager.Get("SelectionModeAlwaysTake");
            if (mode == ApparelSelectionMode.RandomChance) return LanguageManager.Get("SelectionModeRandomChance");
            int pool = (int)mode - (int)ApparelSelectionMode.FromPool1 + 1;
            if (pool >= 1 && pool <= 4) return LanguageManager.Get("SelectionModeFromPool", pool);
            return mode.ToString();
        }
    }
}
