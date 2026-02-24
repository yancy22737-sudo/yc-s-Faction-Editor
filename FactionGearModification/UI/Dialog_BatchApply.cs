using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class Dialog_BatchApply : Window
    {
        private FactionDef factionDef;
        private KindGearData sourceData;
        private List<PawnKindDef> allKinds;
        private HashSet<PawnKindDef> selectedKinds = new HashSet<PawnKindDef>();
        private string searchText = "";
        private Vector2 scrollPos;

        public override Vector2 InitialSize => new Vector2(500f, 700f);

        public Dialog_BatchApply(FactionDef faction, KindGearData source)
        {
            this.factionDef = faction;
            this.sourceData = source;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            
            // Load kinds
            this.allKinds = FactionGearEditor.GetFactionKinds(faction);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), LanguageManager.Get("BatchApplyTitle"));
            Text.Font = GameFont.Small;

            float y = inRect.y + 40f;
            
            // Search
            Rect searchRect = new Rect(inRect.x, y, inRect.width, 24f);
            string oldSearch = searchText;
            searchText = Widgets.TextField(searchRect, searchText);
            if (searchText != oldSearch) scrollPos = Vector2.zero;
            y += 30f;

            // Buttons: Select All / None
            Rect btnRow = new Rect(inRect.x, y, inRect.width, 24f);
            if (Widgets.ButtonText(new Rect(btnRow.x, btnRow.y, 100f, 24f), LanguageManager.Get("SelectAll")))
            {
                selectedKinds.Clear();
                foreach (var k in GetFilteredKinds()) selectedKinds.Add(k);
            }
            if (Widgets.ButtonText(new Rect(btnRow.x + 110f, btnRow.y, 100f, 24f), LanguageManager.Get("SelectNone")))
            {
                selectedKinds.Clear();
            }
            
            // Selected Count
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(btnRow.xMax - 200f, btnRow.y, 200f, 24f), $"{LanguageManager.Get("Selected")}: {selectedKinds.Count}");
            Text.Anchor = TextAnchor.UpperLeft;

            y += 30f;

            // List
            Rect listRect = new Rect(inRect.x, y, inRect.width, inRect.height - y - 40f);
            var filtered = GetFilteredKinds().ToList();
            Rect viewRect = new Rect(0, 0, listRect.width - 16f, filtered.Count * 28f);
            
            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);
            float curY = 0f;
            foreach (var kind in filtered)
            {
                Rect rowRect = new Rect(0, curY, viewRect.width, 24f);
                
                // Alternating row background
                if ((int)(curY / 28f) % 2 == 1) Widgets.DrawAltRect(rowRect);

                bool selected = selectedKinds.Contains(kind);
                bool oldSelected = selected;
                
                Widgets.CheckboxLabeled(rowRect, kind.LabelCap, ref selected);
                
                if (selected != oldSelected)
                {
                    if (selected) selectedKinds.Add(kind);
                    else selectedKinds.Remove(kind);
                }
                curY += 28f;
            }
            Widgets.EndScrollView();

            // Apply Button
            Rect applyRect = new Rect(inRect.x, inRect.height - 30f, inRect.width, 30f);
            if (Widgets.ButtonText(applyRect, LanguageManager.Get("Apply")))
            {
                if (selectedKinds.Count > 0)
                {
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        string.Format(LanguageManager.Get("BatchApplyConfirm"), selectedKinds.Count),
                        LanguageManager.Get("Yes"),
                        () => {
                            Apply();
                            Close();
                        },
                        LanguageManager.Get("No"),
                        null,
                        null,
                        false,
                        null,
                        null
                    ));
                }
                else
                {
                    Close();
                }
            }
        }

        private IEnumerable<PawnKindDef> GetFilteredKinds()
        {
            if (string.IsNullOrEmpty(searchText)) return allKinds;
            string term = searchText.ToLower();
            return allKinds.Where(k => (k.label ?? "").ToLower().Contains(term) || k.defName.ToLower().Contains(term));
        }

        private void Apply()
        {
            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(factionDef.defName);
            int count = 0;

            // Collect targets for undo
            List<KindGearData> targets = new List<KindGearData>();
            foreach (var kind in selectedKinds)
            {
                var targetKindData = factionData.GetOrCreateKindData(kind.defName);
                if (targetKindData != null)
                {
                    targets.Add(targetKindData);
                }
            }

            // Record state
            if (targets.Count > 0)
            {
                UndoManager.RecordState(new BatchUndoable(targets));
            }

            foreach (var targetKindData in targets)
            {
                targetKindData.CopyFrom(sourceData);
                targetKindData.isModified = true;
                count++;
            }
            FactionGearEditor.MarkDirty();
            Messages.Message(string.Format(LanguageManager.Get("BatchApplied"), count), MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
