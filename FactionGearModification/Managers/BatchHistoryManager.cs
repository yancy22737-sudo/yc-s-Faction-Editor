using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;

namespace FactionGearCustomizer
{
    public class BatchApplyRecord
    {
        public DateTime AppliedAt;
        public string SourceKind;
        public string SourceFaction;
        public GearCopyFlags Flags;
        public List<string> TargetKinds;

        // Snapshots of the target kinds before the batch apply
        public List<KindGearData> PreApplySnapshots;

        // Live references to the targets, so we can revert them
        public List<KindGearData> TargetRefs;

        public bool IsValid
        {
            get
            {
                if (TargetRefs == null || PreApplySnapshots == null) return false;
                if (TargetRefs.Count != PreApplySnapshots.Count) return false;

                // Ensure no null refs and references are still attached to the global settings
                // This prevents issues where a faction/kind is deleted by another mod
                // or cleared by the user, but the history still holds a disconnected reference.
                foreach (var r in TargetRefs)
                {
                    if (r == null) return false;

                    bool found = false;
                    foreach (var fac in FactionGearCustomizerMod.Settings.factionGearData)
                    {
                        if (fac.kindGearData != null && fac.kindGearData.Contains(r))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }
                return true;
            }
        }
    }

    public static class BatchHistoryManager
    {
        public const int MaxHistory = 50;
        public static List<BatchApplyRecord> History = new List<BatchApplyRecord>();

        public static void Record(BatchApplyRecord record)
        {
            if (record == null || !record.IsValid) return;

            History.Insert(0, record);
            if (History.Count > MaxHistory)
            {
                History.RemoveRange(MaxHistory, History.Count - MaxHistory);
            }
        }

        public static void Revert(BatchApplyRecord record)
        {
            if (record == null) return;
            // Prevent execution if objects are somehow broken or disconnected due to mods
            if (!record.IsValid)
            {
                Messages.Message(LanguageManager.Get("BatchHistoryInvalid"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            try
            {
                for (int i = 0; i < record.TargetRefs.Count; i++)
                {
                    var liveRef = record.TargetRefs[i];
                    var snapshot = record.PreApplySnapshots[i];
                    if (liveRef != null && snapshot != null)
                    {
                        liveRef.CopyFrom(snapshot);
                    }
                }

                History.Remove(record);
                FactionGearEditor.MarkDirty();
                Messages.Message(LanguageManager.Get("BatchReverted"), MessageTypeDefOf.PositiveEvent, false);
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Error reverting batch apply: {ex.Message}\n{ex.StackTrace}");
                Messages.Message(LanguageManager.Get("BatchHistoryInvalid"), MessageTypeDefOf.RejectInput, false);
            }
        }

        public static void Clear()
        {
            History.Clear();
        }
    }
}
