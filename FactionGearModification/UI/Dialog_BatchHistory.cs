using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class Dialog_BatchHistory : Window
    {
        private Vector2 scrollPos = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        public Dialog_BatchHistory()
        {
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y;

            // Header
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, 300f, 30f), LanguageManager.Get("BatchHistoryTitle"));
            Text.Font = GameFont.Small;
            y += 35f;

            // Clear All Button & Count
            Rect clearRect = new Rect(inRect.x, y, 100f, 24f);
            if (Widgets.ButtonText(clearRect, LanguageManager.Get("ClearAll")))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("BatchHistoryClearConfirm"),
                    LanguageManager.Get("Yes"),
                    () => { BatchHistoryManager.Clear(); },
                    LanguageManager.Get("No"),
                    null, null, false, null, null));
            }

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(inRect.xMax - 200f, y, 200f, 24f),
                string.Format(LanguageManager.Get("BatchHistoryCount"), BatchHistoryManager.History.Count));
            Text.Anchor = TextAnchor.UpperLeft;
            y += 30f;

            Widgets.DrawLineHorizontal(inRect.x, y, inRect.width);
            y += 10f;

            // List
            Rect listRect = new Rect(inRect.x, y, inRect.width, inRect.height - y - 10f);

            if (BatchHistoryManager.History.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(listRect, LanguageManager.Get("BatchHistoryEmpty"));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            float contentHeight = BatchHistoryManager.History.Count * 90f + 10f;
            Rect viewRect = new Rect(0, 0, listRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);

            float curY = 0f;
            foreach (var record in BatchHistoryManager.History.ToList())
            {
                DrawRecord(new Rect(0, curY, viewRect.width, 85f), record);
                curY += 90f;
            }

            Widgets.EndScrollView();
        }

        private void DrawRecord(Rect rect, BatchApplyRecord record)
        {
            Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.05f));
            Widgets.DrawHighlightIfMouseover(rect);

            Rect inner = rect.ContractedBy(4f);
            float y = inner.y;

            // Time
            GUI.color = Color.gray;
            Widgets.Label(new Rect(inner.x, y, 200f, 20f), record.AppliedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            GUI.color = Color.white;
            y += 20f;

            // Info (Translate defNames to Labels)
            string facLabel = DefDatabase<FactionDef>.GetNamedSilentFail(record.SourceFaction)?.LabelCap.ToString() ?? record.SourceFaction;
            string kindLabel = DefDatabase<PawnKindDef>.GetNamedSilentFail(record.SourceKind)?.LabelCap.ToString() ?? record.SourceKind;

            Widgets.Label(new Rect(inner.x, y, inner.width - 120f, 20f),
                string.Format(LanguageManager.Get("BatchHistory_SrcTgt"), facLabel, kindLabel, record.TargetKinds.Count));
            y += 18f;

            // Flags
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);

            System.Collections.Generic.List<string> flagLabels = new System.Collections.Generic.List<string>();
            foreach (GearCopyFlags flagValue in Enum.GetValues(typeof(GearCopyFlags)))
            {
                if (flagValue != GearCopyFlags.None && flagValue != GearCopyFlags.All && (record.Flags & flagValue) != 0)
                {
                    flagLabels.Add(LanguageManager.Get(flagValue.ToString()));
                }
            }
            string flagsStr = string.Join(", ", flagLabels);

            Widgets.Label(new Rect(inner.x, y, inner.width - 120f, 20f),
                string.Format(LanguageManager.Get("BatchHistory_Flags"), flagsStr));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Revert button (right aligned)
            Rect btnRect = new Rect(inner.xMax - 110f, inner.y + 25f, 105f, 24f);
            if (!record.IsValid)
            {
                GUI.color = Color.gray;
                Widgets.ButtonText(btnRect, LanguageManager.Get("BatchHistoryInvalidBtn"), active: false);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(btnRect, LanguageManager.Get("BatchHistoryInvalidTooltip"));
            }
            else if (Widgets.ButtonText(btnRect, LanguageManager.Get("BatchHistoryRevertBtn")))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("BatchHistoryRevertConfirm"),
                    LanguageManager.Get("Yes"),
                    () => { BatchHistoryManager.Revert(record); },
                    LanguageManager.Get("No"),
                    null, null, false, null, null));
            }
        }
    }
}
