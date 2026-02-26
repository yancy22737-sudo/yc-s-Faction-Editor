using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI
{
    public class Dialog_HediffPicker : Window
    {
        private readonly Action<HediffDef> onPickSingle;
        private readonly List<ForcedHediff> targetList;
        private readonly KindGearData kindData;
        private readonly bool multiSelect;

        private List<HediffDef> allCandidates = new List<HediffDef>();
        private List<HediffDef> filteredCandidates = new List<HediffDef>();
        private HashSet<HediffDef> selected = new HashSet<HediffDef>();

        private List<string> allCategories = new List<string>();
        private HashSet<string> existingDefNames;

        private HediffPickerFilterState filterState;
        private bool filterDirty = true;
        private Vector2 scrollPos;

        private bool skipExisting = true;

        private const float RowHeight = 28f;

        public override Vector2 InitialSize => new Vector2(720f, 760f);

        public Dialog_HediffPicker(List<ForcedHediff> targetList, KindGearData kindData = null)
        {
            this.targetList = targetList;
            this.kindData = kindData;
            multiSelect = true;
            InitCommon();
        }

        public Dialog_HediffPicker(Action<HediffDef> onPickSingle)
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
            filterState = PickerSession.Hediffs;
            BuildCandidates();
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 35f), multiSelect ? LanguageManager.Get("HediffPickerTitle") : LanguageManager.Get("SelectHediff"));
            Text.Font = GameFont.Small;
            y += 40f;

            EnsureFilterUpToDate();
            y = DrawFilterBar(inRect, y);
            if (multiSelect) DrawSelectionRow(inRect, ref y);

            float bottomHeight = multiSelect ? 40f : 36f;
            Rect listRect = new Rect(0f, y, inRect.width, inRect.height - y - bottomHeight);
            DrawList(listRect);

            if (multiSelect) DrawBottomButtonsMulti(inRect);
            else DrawBottomButtonsSingle(inRect);
        }

        private float DrawFilterBar(Rect inRect, float y)
        {
            float height = 84f;
            Rect rect = new Rect(0f, y, inRect.width, height);
            Widgets.DrawMenuSection(rect);

            Rect inner = rect.ContractedBy(6f);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inner);

            Rect searchRect = listing.GetRect(24f);
            string next = Widgets.TextField(searchRect, filterState.SearchText ?? "");
            if (string.IsNullOrEmpty(next))
            {
                var anchor = Text.Anchor;
                var color = GUI.color;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                Widgets.Label(new Rect(searchRect.x + 5f, searchRect.y, searchRect.width - 5f, searchRect.height), LanguageManager.Get("Search") + "...");
                GUI.color = color;
                Text.Anchor = anchor;
            }

            if (next != filterState.SearchText)
            {
                filterState.SearchText = next;
                OnFilterChanged();
            }

            if (!string.IsNullOrEmpty(filterState.SearchText))
            {
                Rect clearButtonRect = new Rect(searchRect.xMax - 18f, searchRect.y + 2f, 16f, 16f);
                if (Widgets.ButtonImage(clearButtonRect, Widgets.CheckboxOffTex))
                {
                    filterState.SearchText = "";
                    OnFilterChanged();
                }
                TooltipHandler.TipRegion(clearButtonRect, LanguageManager.Get("ClearSearch"));
            }

            listing.Gap(4f);

            Rect row = listing.GetRect(24f);
            string catText;
            if (filterState.SelectedCategories.Count == 0 || filterState.SelectedCategories.Count == allCategories.Count)
            {
                catText = LanguageManager.Get("CategoryAll");
            }
            else if (filterState.SelectedCategories.Count == 1)
            {
                catText = filterState.SelectedCategories.First();
            }
            else
            {
                catText = LanguageManager.Get("CategoryCountSelected", filterState.SelectedCategories.Count);
            }

            if (Widgets.ButtonText(row, catText))
            {
                Find.WindowStack.Add(new Window_StringFilter(LanguageManager.Get("CategoryFilter"), allCategories, filterState.SelectedCategories, OnFilterChanged));
            }

            listing.End();
            return y + height + 6f;
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
                HashSet<HediffDef> next = new HashSet<HediffDef>();
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
                HediffDef def = filteredCandidates[i];
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);

                if (i % 2 == 1) Widgets.DrawAltRect(rowRect);
                if (Mouse.IsOver(rowRect)) Widgets.DrawHighlight(rowRect);

                DrawRow(rowRect, def);
            }

            Widgets.EndScrollView();
        }

        private void DrawRow(Rect rowRect, HediffDef def)
        {
            string cat = GetHediffCategory(def);
            string tip = $"{def.LabelCap}\n{def.defName}\n{cat}";
            TooltipHandler.TipRegion(rowRect, tip);

            Rect inner = rowRect.ContractedBy(6f, 2f);
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

            if (Current.Game != null)
            {
                Widgets.InfoCardButton(x, inner.y, def);
                x += 28f;
            }

            Rect leftRect = new Rect(x, inner.y, inner.width - (x - inner.x) - 160f, inner.height);
            Rect rightRect = new Rect(leftRect.xMax + 6f, inner.y, 154f, inner.height);

            Widgets.Label(leftRect, def.LabelCap.ToString());

            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = Color.gray;
            Widgets.Label(rightRect, cat);
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

            // Record undo state before adding
            if (kindData != null)
            {
                UndoManager.RecordState(kindData);
                kindData.isModified = true;
            }

            if (existingDefNames == null)
            {
                existingDefNames = new HashSet<string>(targetList.Where(x => x?.HediffDef != null).Select(x => x.HediffDef.defName));
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

                targetList.Add(new ForcedHediff
                {
                    HediffDef = def,
                    chance = 1f,
                    maxPartsRange = new IntRange(1, 3),
                    severityRange = new FloatRange(0.5f, 1f)
                });
                existingDefNames.Add(def.defName);
                added++;
            }

            if (added > 0)
            {
                FactionGearEditor.MarkDirty();
            }

            Messages.Message(LanguageManager.Get("HediffsAddedMessage", added, skipped), MessageTypeDefOf.PositiveEvent, false);
            selected.Clear();
        }

        private void BuildCandidates()
        {
            allCandidates = DefDatabase<HediffDef>.AllDefs
                .Where(IsCandidate)
                .OrderBy(t => t.LabelCap.ToString() ?? t.defName)
                .ToList();

            allCategories = allCandidates.Select(GetHediffCategory).Distinct().OrderBy(x => x).ToList();
            filterState.SyncAvailableCategories(allCategories);

            if (multiSelect && targetList != null)
            {
                existingDefNames = new HashSet<string>(targetList.Where(x => x?.HediffDef != null).Select(x => x.HediffDef.defName));
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
            filteredCandidates = ApplyFilter();
            filterDirty = false;
        }

        private List<HediffDef> ApplyFilter()
        {
            IEnumerable<HediffDef> items = allCandidates;

            if (filterState.SelectedCategories.Count > 0 && filterState.SelectedCategories.Count != allCategories.Count)
            {
                items = items.Where(d => filterState.SelectedCategories.Contains(GetHediffCategory(d)));
            }

            string term = (filterState.SearchText ?? "").Trim();
            if (!string.IsNullOrEmpty(term))
            {
                string lower = term.ToLowerInvariant();
                items = items.Where(def =>
                {
                    if (def == null) return false;
                    string cat = GetHediffCategory(def) ?? "";
                    if ((def.LabelCap.ToString() ?? "").ToLowerInvariant().Contains(lower)) return true;
                    if ((def.defName ?? "").ToLowerInvariant().Contains(lower)) return true;
                    if (cat.ToLowerInvariant().Contains(lower)) return true;
                    return false;
                });
            }

            return items.OrderBy(d => d.LabelCap.ToString() ?? d.defName).ToList();
        }

        private static bool IsCandidate(HediffDef h)
        {
            if (h == null) return false;
            return true;
        }

        public static string GetHediffCategory(HediffDef def)
        {
            if (def == null) return "";
            if (def.countsAsAddedPartOrImplant) return "Implants";
            if (def.isBad && def.defName.Contains("Missing")) return "Missing Parts";
            if (def.isBad) return "Debuffs";
            if (def.makesSickThought) return "Illnesses";
            if (def.defName.ToLower().Contains("high")) return "DrugHighs";
            if (!def.isBad) return "Buffs";
            return "Other";
        }
    }
}
