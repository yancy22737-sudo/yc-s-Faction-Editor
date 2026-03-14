using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    // Responsibility: Let users choose manual hediff target body parts in advanced editor mode.
    // Dependencies: BodyPartDef database, language keys, Window/Widgets UI framework.
    public class Dialog_BodyPartSelector : Window
    {
        private readonly List<BodyPartDef> availableParts;
        private readonly HashSet<string> selectedPartDefs;
        private readonly Action<List<BodyPartDef>> onApply;
        private Vector2 scrollPos;
        private string searchText = string.Empty;

        public override Vector2 InitialSize => new Vector2(520f, 700f);

        public Dialog_BodyPartSelector(IEnumerable<BodyPartDef> selectedParts, Action<List<BodyPartDef>> onApply)
        {
            this.onApply = onApply;
            selectedPartDefs = new HashSet<string>(
                selectedParts?.Where(p => p != null).Select(p => p.defName) ?? Enumerable.Empty<string>());
            availableParts = LoadAvailableParts();
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            DrawTitle(inRect);
            DrawSearch(inRect);
            DrawSelectionButtons(inRect);
            DrawPartList(inRect);
            DrawBottomButtons(inRect);
        }

        private void DrawTitle(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), LanguageManager.Get("BodyPartSelectionTitle"));
            Text.Font = GameFont.Small;
        }

        private void DrawSearch(Rect inRect)
        {
            float y = inRect.y + 40f;
            Widgets.Label(new Rect(inRect.x, y, 80f, 24f), LanguageManager.Get("Search"));
            searchText = Widgets.TextField(new Rect(inRect.x + 85f, y, inRect.width - 85f, 24f), searchText);
        }

        private void DrawSelectionButtons(Rect inRect)
        {
            float y = inRect.y + 70f;
            if (Widgets.ButtonText(new Rect(inRect.x, y, 110f, 24f), LanguageManager.Get("SelectAll")))
            {
                selectedPartDefs.Clear();
                foreach (var part in GetFilteredParts())
                {
                    selectedPartDefs.Add(part.defName);
                }
            }

            if (Widgets.ButtonText(new Rect(inRect.x + 120f, y, 110f, 24f), LanguageManager.Get("SelectNone")))
            {
                selectedPartDefs.Clear();
            }

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(inRect.x + 240f, y, inRect.width - 240f, 24f),
                $"{LanguageManager.Get("Selected")}: {selectedPartDefs.Count}");
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawPartList(Rect inRect)
        {
            float top = inRect.y + 100f;
            float bottom = inRect.yMax - 40f;
            var listRect = new Rect(inRect.x, top, inRect.width, bottom - top);
            var filteredParts = GetFilteredParts().ToList();
            var viewRect = new Rect(0f, 0f, listRect.width - 16f, filteredParts.Count * 28f);

            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);
            float currentY = 0f;
            for (int i = 0; i < filteredParts.Count; i++)
            {
                var part = filteredParts[i];
                var rowRect = new Rect(0f, currentY, viewRect.width, 24f);
                if (i % 2 == 1)
                {
                    Widgets.DrawAltRect(rowRect);
                }

                bool selected = selectedPartDefs.Contains(part.defName);
                bool original = selected;
                string label = $"{part.LabelCap} ({part.defName})";
                Widgets.CheckboxLabeled(rowRect, label, ref selected);
                if (selected != original)
                {
                    if (selected)
                    {
                        selectedPartDefs.Add(part.defName);
                    }
                    else
                    {
                        selectedPartDefs.Remove(part.defName);
                    }
                }

                currentY += 28f;
            }
            Widgets.EndScrollView();
        }

        private void DrawBottomButtons(Rect inRect)
        {
            var autoRect = new Rect(inRect.x, inRect.yMax - 30f, 110f, 30f);
            if (Widgets.ButtonText(autoRect, LanguageManager.Get("HediffPartModeAuto")))
            {
                onApply?.Invoke(new List<BodyPartDef>());
                Close();
                return;
            }

            var applyRect = new Rect(inRect.x + inRect.width - 120f, inRect.yMax - 30f, 120f, 30f);
            if (Widgets.ButtonText(applyRect, LanguageManager.Get("Apply")))
            {
                var result = selectedPartDefs
                    .Select(defName => DefDatabase<BodyPartDef>.GetNamedSilentFail(defName))
                    .Where(def => def != null)
                    .OrderBy(def => def.label)
                    .ThenBy(def => def.defName)
                    .ToList();
                onApply?.Invoke(result);
                Close();
            }
        }

        private IEnumerable<BodyPartDef> GetFilteredParts()
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return availableParts;
            }

            string term = searchText.ToLowerInvariant();
            return availableParts.Where(part =>
            {
                string label = (part.label ?? string.Empty).ToLowerInvariant();
                string defName = (part.defName ?? string.Empty).ToLowerInvariant();
                return label.Contains(term) || defName.Contains(term);
            });
        }

        private static List<BodyPartDef> LoadAvailableParts()
        {
            var humanDef = DefDatabase<ThingDef>.GetNamedSilentFail("Human");
            if (humanDef?.race?.body != null)
            {
                return humanDef.race.body.AllParts
                    .Where(part => part?.def != null)
                    .Select(part => part.def)
                    .Distinct()
                    .OrderBy(def => def.label)
                    .ThenBy(def => def.defName)
                    .ToList();
            }

            return DefDatabase<BodyPartDef>.AllDefsListForReading
                .Where(def => def != null)
                .OrderBy(def => def.label)
                .ThenBy(def => def.defName)
                .ToList();
        }
    }
}
