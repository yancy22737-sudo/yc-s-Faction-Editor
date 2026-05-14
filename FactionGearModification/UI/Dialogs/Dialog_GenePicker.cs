using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_GenePicker : Window
    {
        private readonly List<ForcedGene> targetList;

        private List<GeneDef> allGenes = new List<GeneDef>();
        private Dictionary<GeneCategoryDef, List<GeneDef>> genesByCategory = new Dictionary<GeneCategoryDef, List<GeneDef>>();
        private List<GeneCategoryDef> sortedCategories = new List<GeneCategoryDef>();
        private HashSet<GeneDef> selected = new HashSet<GeneDef>();
        private HashSet<string> existingDefNames;
        private Vector2 scrollPos;
        private string searchText = "";
        private bool skipExisting = true;

        private const float RowHeight = 34f;
        private const float CategoryHeaderHeight = 28f;

        public override Vector2 InitialSize => new Vector2(680f, 750f);

        public Dialog_GenePicker(List<ForcedGene> targetList)
        {
            this.targetList = targetList;
            InitCommon();
        }

        private void InitCommon()
        {
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            draggable = true;
            resizeable = true;

            allGenes = DefDatabase<GeneDef>.AllDefs.ToList();

            // Group by category
            genesByCategory.Clear();
            var allCats = DefDatabase<GeneCategoryDef>.AllDefs.ToList();
            foreach (var gene in allGenes)
            {
                var cat = gene.displayCategory;
                if (cat == null) continue;
                if (!genesByCategory.ContainsKey(cat))
                    genesByCategory[cat] = new List<GeneDef>();
                genesByCategory[cat].Add(gene);
            }
            foreach (var kv in genesByCategory)
                kv.Value.Sort((a, b) => (a.LabelCap.ToString()).CompareTo(b.LabelCap.ToString()));

            sortedCategories = genesByCategory.Keys
                .OrderByDescending(c => c.displayPriorityInXenotype)
                .ToList();

            existingDefNames = new HashSet<string>(targetList.Where(x => x?.GeneDef != null).Select(x => x.GeneDef.defName));
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 35f), LanguageManager.Get("GenePickerTitle"));
            Text.Font = GameFont.Small;
            y += 40f;

            // Search bar
            Rect searchRect = new Rect(0f, y, inRect.width, 28f);
            searchText = Widgets.TextField(searchRect, searchText);
            y += 34f;

            // Skip existing
            if (existingDefNames.Count > 0)
            {
                Rect skipRect = new Rect(0f, y, inRect.width, 24f);
                Widgets.CheckboxLabeled(skipRect, LanguageManager.Get("SkipExistingItems"), ref skipExisting);
                y += 28f;
            }

            float bottomHeight = 40f;
            Rect listRect = new Rect(0f, y, inRect.width, inRect.height - y - bottomHeight);
            DrawGeneList(listRect);

            DrawBottomMulti(inRect);
        }

        private void DrawGeneList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);

            string lowerSearch = (searchText ?? "").Trim().ToLowerInvariant();
            bool hasSearch = !string.IsNullOrEmpty(lowerSearch);

            // Calculate content height
            float contentHeight = 0f;
            if (hasSearch)
            {
                var filtered = allGenes.Where(g =>
                    (g.LabelCap.ToString()).ToLowerInvariant().Contains(lowerSearch) ||
                    (g.defName ?? "").ToLowerInvariant().Contains(lowerSearch) ||
                    (g.description ?? "").ToLowerInvariant().Contains(lowerSearch)).ToList();
                contentHeight = filtered.Count * RowHeight;
            }
            else
            {
                foreach (var cat in sortedCategories)
                {
                    if (genesByCategory.TryGetValue(cat, out var genes))
                        contentHeight += CategoryHeaderHeight + genes.Count * RowHeight;
                }
            }
            contentHeight += 8f;

            Rect viewRect = new Rect(0f, 0f, inner.width - 16f, contentHeight);
            Widgets.BeginScrollView(inner, ref scrollPos, viewRect);
            float y = 0f;

            if (hasSearch)
            {
                var filtered = allGenes.Where(g =>
                    (g.LabelCap.ToString()).ToLowerInvariant().Contains(lowerSearch) ||
                    (g.defName ?? "").ToLowerInvariant().Contains(lowerSearch) ||
                    (g.description ?? "").ToLowerInvariant().Contains(lowerSearch)).ToList();
                foreach (var gene in filtered)
                    DrawGeneRow(viewRect, gene, ref y);
            }
            else
            {
                geneRowIndex = 0;
                foreach (var cat in sortedCategories)
                {
                    if (!genesByCategory.TryGetValue(cat, out var genes) || genes.Count == 0) continue;

                    // Category header
                    Rect headerRect = new Rect(0f, y, viewRect.width, CategoryHeaderHeight);
                    Widgets.DrawBoxSolid(headerRect, new Color(0.18f, 0.18f, 0.22f));
                    Widgets.Label(new Rect(headerRect.x + 8f, headerRect.y, headerRect.width - 12f, CategoryHeaderHeight),
                        $"<b>{cat.LabelCap}</b>");
                    y += CategoryHeaderHeight;

                    foreach (var gene in genes)
                    {
                        DrawGeneRow(viewRect, gene, ref y);
                        geneRowIndex++;
                    }
                }
            }

            Widgets.EndScrollView();
        }

        private int geneRowIndex = 0;

        private void DrawGeneRow(Rect viewRect, GeneDef gene, ref float y)
        {
            Rect row = new Rect(0f, y, viewRect.width, RowHeight);

            // Alternating row background (only in categorized mode, not search)
            if (geneRowIndex % 2 == 1)
                Widgets.DrawAltRect(row);
            if (Mouse.IsOver(row))
                Widgets.DrawHighlight(row);

            bool alreadyExists = existingDefNames.Contains(gene.defName);

            // Vanilla InfoCard button
            Widgets.InfoCardButton(row.x + 2f, row.y + 3f, gene);

            // Gene icon (28x28, bigger)
            Rect iconRect = new Rect(row.x + 26f, row.y + 3f, 28f, 28f);
            if (gene.Icon != null)
            {
                GUI.color = gene.IconColor;
                GUI.DrawTexture(iconRect, gene.Icon, ScaleMode.ScaleToFit);
                GUI.color = Color.white;
            }
            else
            {
                Widgets.DrawBoxSolid(iconRect, new Color(0.22f, 0.22f, 0.28f));
            }

            float labelX = iconRect.xMax + 4f;
            string modSource = gene.modContentPack != null ? gene.modContentPack.Name : "Unknown";
            modSource = Truncate(modSource, 78f);
            if (alreadyExists && skipExisting)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                Widgets.Label(new Rect(labelX, row.y, row.width - labelX - 170f, RowHeight),
                    gene.LabelCap + " (" + LanguageManager.Get("AlreadyAdded") + ")");
                GUI.color = Color.white;
            }
            else
            {
                bool isSel = selected.Contains(gene);
                Widgets.CheckboxLabeled(new Rect(labelX, row.y, row.width - labelX - 170f, RowHeight), gene.LabelCap, ref isSel, false);
                if (isSel != selected.Contains(gene))
                {
                    if (isSel) selected.Add(gene);
                    else selected.Remove(gene);
                }
            }

            // Mod source column
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(row.x + row.width - 165f, row.y, 80f, RowHeight), modSource);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Metabolism + Complexity
            float rightX = row.x + row.width - 80f;
            float met = gene.biostatMet;
            string metStr = met > 0 ? $"+{met:F0}" : $"{met:F0}";
            Color metColor = met > 0 ? new Color(1f, 0.4f, 0.3f) : (met < 0 ? new Color(0.3f, 0.85f, 0.3f) : Color.gray);
            Rect metRect = new Rect(rightX, row.y, 40f, RowHeight);
            GUI.color = metColor;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(metRect, metStr);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            float cpx = gene.biostatCpx;
            if (cpx > 0)
            {
                Rect cpxRect = new Rect(rightX + 42f, row.y, 36f, RowHeight);
                GUI.color = new Color(0.5f, 0.7f, 1f);
                Widgets.Label(cpxRect, "c" + cpx.ToString("F0"));
                GUI.color = Color.white;
            }

            // Tooltip
            string tip = gene.LabelCap.ToString();
            if (!string.IsNullOrEmpty(gene.description))
                tip += "\n" + gene.description;
            tip += "\nMet: " + (gene.biostatMet > 0 ? "+" : "") + gene.biostatMet.ToString("F0");
            tip += "\nCpx: " + gene.biostatCpx.ToString("F0");
            TooltipHandler.TipRegion(row, tip);

            y += RowHeight;
        }

        private static string Truncate(string text, float maxW)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (Text.CalcSize(text).x <= maxW) return text;
            for (int i = text.Length - 1; i > 0; i--)
                if (Text.CalcSize(text.Substring(0, i) + "...").x <= maxW)
                    return text.Substring(0, i) + "...";
            return "...";
        }

        private void DrawBottomMulti(Rect inRect)
        {
            Rect bottomRect = new Rect(0f, inRect.height - 40f, inRect.width, 40f);
            GUI.BeginGroup(bottomRect);

            float btnW = (bottomRect.width - 20f) / 2f;
            if (Widgets.ButtonText(new Rect(0f, 4f, btnW, 32f), LanguageManager.Get("AddSelected")))
                AddSelectedGenes();

            if (Widgets.ButtonText(new Rect(btnW + 16f, 4f, btnW, 32f), LanguageManager.Get("Cancel")))
                Close();

            GUI.EndGroup();
        }

        private void AddSelectedGenes()
        {
            int added = 0;
            int skipped = 0;

            foreach (var def in selected)
            {
                if (existingDefNames.Contains(def.defName))
                {
                    skipped++;
                    continue;
                }

                targetList.Add(new ForcedGene { GeneDef = def, asEndogene = false, chance = 1f });
                added++;
            }

            if (added > 0) FactionGearEditor.MarkDirty();

            Messages.Message(LanguageManager.Get("GenesAddedMessage", added, skipped), MessageTypeDefOf.PositiveEvent, false);
            selected.Clear();
        }
    }
}
