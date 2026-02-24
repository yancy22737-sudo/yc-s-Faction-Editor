using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer; // Added to access FactionGearEditor

namespace FactionGearCustomizer.UI
{
    public class Dialog_PawnKindPicker : Window
    {
        private Action<List<PawnKindDef>> onSelect;
        private FactionDef factionDef;
        private string search = "";
        private Vector2 scroll;
        private List<PawnKindDef> filteredList;
        private List<PawnKindDef> allKinds;
        private HashSet<string> selectedKindDefNames = new HashSet<string>();
        private bool filterByFaction = true;
        private bool sortByCombatPower = false;
        private bool sortDescending = false;
        private PawnKindCategory categoryFilter = PawnKindCategory.All;
        private ModContentPack modFilter = null;
        private static System.Reflection.FieldInfo defaultFactionTypeField;

        private enum PawnKindCategory
        {
            All,
            Humanlike,
            Animal,
            Mechanoid,
            Other
        }

        public override Vector2 InitialSize => new Vector2(800f, 700f);

        private HashSet<string> excludeKindDefNames = null;

        public Dialog_PawnKindPicker(Action<List<PawnKindDef>> onSelect, FactionDef faction = null, HashSet<string> excludeKinds = null)
        {
            this.onSelect = onSelect;
            this.factionDef = faction;
            this.excludeKindDefNames = excludeKinds;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.allKinds = DefDatabase<PawnKindDef>.AllDefsListForReading?.ToList() ?? new List<PawnKindDef>();

            if (defaultFactionTypeField == null)
            {
                defaultFactionTypeField = typeof(PawnKindDef).GetField("defaultFactionType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            if (this.factionDef == null) filterByFaction = false;

            UpdateList();
        }

        private PawnKindCategory GetCategory(PawnKindDef kind)
        {
            if (kind.RaceProps.Humanlike) return PawnKindCategory.Humanlike;
            if (kind.RaceProps.Animal) return PawnKindCategory.Animal;
            if (kind.RaceProps.IsMechanoid) return PawnKindCategory.Mechanoid;
            return PawnKindCategory.Other;
        }

        private string GetCategoryLabel(PawnKindCategory cat)
        {
            return LanguageManager.Get("Category_" + cat.ToString());
        }

        private void UpdateList()
        {
            IEnumerable<PawnKindDef> source = allKinds;

            // 排除已存在的兵种
            if (excludeKindDefNames != null && excludeKindDefNames.Count > 0)
            {
                source = source.Where(k => !excludeKindDefNames.Contains(k.defName));
            }

            // 优化：当过滤派系单位时，使用缓存避免重复计算
            if (filterByFaction && factionDef != null)
            {
                var factionKinds = FactionGearEditor.GetFactionKinds(factionDef);
                if (factionKinds != null && factionKinds.Any())
                {
                    source = factionKinds;
                }
            }

            // 优化：先应用搜索过滤，减少后续处理的数据量
            if (!string.IsNullOrEmpty(search))
            {
                string lowerSearch = search.ToLowerInvariant();
                source = source.Where(k => (k.LabelCap != null && k.LabelCap.ToString().ToLowerInvariant().Contains(lowerSearch)) ||
                                           (k.defName != null && k.defName.ToLowerInvariant().Contains(lowerSearch)));
            }

            if (categoryFilter != PawnKindCategory.All)
            {
                source = source.Where(k => GetCategory(k) == categoryFilter);
            }

            if (modFilter != null)
            {
                source = source.Where(k => k.modContentPack == modFilter);
            }

            // 优化：延迟执行排序直到最后
            if (sortByCombatPower)
            {
                source = sortDescending
                    ? source.OrderBy(k => k.combatPower)
                    : source.OrderByDescending(k => k.combatPower);
            }
            else
            {
                source = sortDescending
                    ? source.OrderByDescending(k => k.LabelCap.ToString())
                    : source.OrderBy(k => k.LabelCap.ToString());
            }

            // 优化：使用延迟执行，只在需要时获取结果
            filteredList = source.Take(300).ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Search
            Rect searchRect = new Rect(0, 0, inRect.width, 30f);
            string newSearch = Widgets.TextField(searchRect, search);
            if (newSearch != search)
            {
                search = newSearch;
                UpdateList();
            }

            // Filters & Sorts
            float toggleY = 35f;
            float rowHeight = 24f;
            float colWidth = inRect.width / 4f;

            // Row 1: Faction Filter, Sort Power, Sort Desc, Category
            
            // 1. Filter By Faction
            Rect toggleRect = new Rect(0, toggleY, colWidth, rowHeight);
            if (factionDef != null)
            {
                bool newFilter = filterByFaction;
                Widgets.CheckboxLabeled(toggleRect, LanguageManager.Get("FilterByFaction"), ref newFilter);
                if (newFilter != filterByFaction)
                {
                    filterByFaction = newFilter;
                    UpdateList();
                }
            }

            // 2. Sort By Power
            Rect sortRect = new Rect(colWidth, toggleY, colWidth, rowHeight);
            bool newSort = sortByCombatPower;
            Widgets.CheckboxLabeled(sortRect, LanguageManager.Get("SortByPower"), ref newSort);
            if (newSort != sortByCombatPower)
            {
                sortByCombatPower = newSort;
                UpdateList();
            }

            // 3. Sort Descending
            Rect descRect = new Rect(colWidth * 2, toggleY, colWidth * 2, rowHeight);
            bool newDesc = sortDescending;
            Widgets.CheckboxLabeled(descRect, LanguageManager.Get("SortDescending"), ref newDesc);
            if (newDesc != sortDescending)
            {
                sortDescending = newDesc;
                UpdateList();
            }

            // Row 2: Mod Filter
            float row2Y = toggleY + 30f;
            Rect modRect = new Rect(0, row2Y, inRect.width / 2, rowHeight);
            string modLabel = modFilter == null ? LanguageManager.Get("ModAll") : modFilter.Name;
            if (Widgets.ButtonText(modRect, "Mod: " + modLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(LanguageManager.Get("ModAll"), () => { modFilter = null; UpdateList(); }));
                
                var mods = allKinds.Select(k => k.modContentPack).Where(m => m != null).Distinct().OrderBy(m => m.Name);
                foreach (var mod in mods)
                {
                    options.Add(new FloatMenuOption(mod.Name, () => { modFilter = mod; UpdateList(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // List Header
            float headerY = row2Y + 30f;
            Rect headerRect = new Rect(0, headerY, inRect.width - 16f, 24f);
            GUI.color = Color.gray;
            
            float curX = 5f;
            
            // Checkbox (30)
            Widgets.Label(new Rect(curX, headerRect.y, 30f, 24f), "");
            curX += 35f;
            
            // PawnKind (200)
            Widgets.Label(new Rect(curX, headerRect.y, 200f, 24f), LanguageManager.Get("PawnKind"));
            curX += 210f;
            
            // Race (100)
            Widgets.Label(new Rect(curX, headerRect.y, 100f, 24f), LanguageManager.Get("Race"));
            curX += 110f;

            // Faction (150)
            Widgets.Label(new Rect(curX, headerRect.y, 150f, 24f), LanguageManager.Get("Factions"));
            curX += 160f;

            // Power (60)
            Widgets.Label(new Rect(curX, headerRect.y, 60f, 24f), LanguageManager.Get("Power"));
            curX += 70f;
            
            // Points (60)
            Widgets.Label(new Rect(curX, headerRect.y, 60f, 24f), LanguageManager.Get("Points"));
            
            GUI.color = Color.white;
            Widgets.DrawLineHorizontal(0, headerY + 24f, inRect.width);

            // List
            float topY = headerY + 30f;
            float confirmButtonHeight = 40f;
            float listRowHeight = 28f;
            Rect outRect = new Rect(0, topY, inRect.width, inRect.height - topY - confirmButtonHeight - 10f);
            Rect viewRect = new Rect(0, 0, outRect.width - 16f, filteredList.Count * listRowHeight);

            // 优化：使用可见性检查，只渲染可见区域的项目
            float viewTop = scroll.y;
            float viewBottom = scroll.y + outRect.height;

            Widgets.BeginScrollView(outRect, ref scroll, viewRect);
            float y = 0f;
            for (int i = 0; i < filteredList.Count; i++)
            {
                var kind = filteredList[i];

                // 可见性检查：只渲染可见区域的项目
                if (y + listRowHeight < viewTop - listRowHeight || y > viewBottom + listRowHeight)
                {
                    y += listRowHeight;
                    continue;
                }

                Rect row = new Rect(0, y, viewRect.width, listRowHeight);

                if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);

                float rowX = 5f;

                // Checkbox
                bool isSelected = selectedKindDefNames.Contains(kind.defName);
                bool newSelected = isSelected;
                Widgets.Checkbox(new Vector2(rowX, y + 2f), ref newSelected);
                if (newSelected != isSelected)
                {
                    if (newSelected)
                    {
                        selectedKindDefNames.Add(kind.defName);
                    }
                    else
                    {
                        selectedKindDefNames.Remove(kind.defName);
                    }
                }
                rowX += 35f;

                // Name
                if (Current.Game != null)
                {
                    Widgets.InfoCardButton(rowX, y, kind);
                    rowX += 28f;
                }
                Widgets.Label(new Rect(rowX, y, 200f - (rowX - 5f), 28f), kind.LabelCap);
                rowX = 215f;

                // Race
                string raceLabel = kind.race?.LabelCap ?? LanguageManager.Get("Unknown");
                Widgets.Label(new Rect(rowX, y, 100f, 28f), raceLabel);
                rowX += 110f;

                // Faction
                string factionLabel = GetFactionLabelForKind(kind);
                Widgets.Label(new Rect(rowX, y, 150f, 28f), factionLabel);
                rowX += 160f;

                // Combat Power
                Widgets.Label(new Rect(rowX, y, 60f, 28f), kind.combatPower.ToString("F0"));
                rowX += 70f;

                // Points Cost
                Widgets.Label(new Rect(rowX, y, 60f, 28f), kind.combatPower.ToString("F0"));

                y += listRowHeight;
            }
            Widgets.EndScrollView();

            // Confirm Button
            Rect confirmRect = new Rect(0, inRect.height - confirmButtonHeight - 10f, inRect.width, confirmButtonHeight);
            if (Widgets.ButtonText(confirmRect, LanguageManager.Get("Confirm")))
            {
                var selectedKinds = filteredList.Where(k => selectedKindDefNames.Contains(k.defName)).ToList();
                onSelect(selectedKinds);
                Close();
            }
        }

        private string GetFactionLabelForKind(PawnKindDef kind)
        {
            if (kind == null) return "-";

            // 1. 尝试从 defaultFactionType 字段获取
            if (defaultFactionTypeField != null)
            {
                var faction = defaultFactionTypeField.GetValue(kind) as FactionDef;
                if (faction != null) return faction.LabelCap;
            }

            // 2. 如果当前有指定派系，检查兵种是否属于该派系
            if (factionDef != null)
            {
                var factionKinds = FactionGearEditor.GetFactionKinds(factionDef);
                if (factionKinds != null && factionKinds.Any(k => k.defName == kind.defName))
                {
                    return factionDef.LabelCap;
                }
            }

            // 3. 尝试从所有派系中查找该兵种所属的派系
            foreach (var faction in DefDatabase<FactionDef>.AllDefsListForReading)
            {
                if (faction.pawnGroupMakers != null)
                {
                    foreach (var pgm in faction.pawnGroupMakers)
                    {
                        if (pgm.options != null)
                        {
                            foreach (var opt in pgm.options)
                            {
                                if (opt.kind != null && opt.kind.defName == kind.defName)
                                {
                                    return faction.LabelCap;
                                }
                            }
                        }
                    }
                }
            }

            return "-";
        }
    }
}
