using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using FactionGearCustomizer.Core;

namespace FactionGearCustomizer.Managers
{
    public static class PresetFactionExporter
    {
        public enum ExportConflictResolution
        {
            Skip,
            Overwrite,
            Merge
        }

        public class ExportResult
        {
            public bool Success;
            public List<string> ExportedFactions = new List<string>();
            public List<string> SkippedFactions = new List<string>();
            public string ErrorMessage;
        }

        public static ExportResult ExportFactionsToPreset(
            List<FactionGearData> sourceFactions,
            FactionGearPreset targetPreset,
            List<string> selectedFactionDefNames,
            ExportConflictResolution resolution)
        {
            var result = new ExportResult();

            if (sourceFactions == null)
            {
                result.Success = false;
                result.ErrorMessage = "Source factions is null";
                return result;
            }

            if (targetPreset == null)
            {
                result.Success = false;
                result.ErrorMessage = "Target preset is null";
                return result;
            }

            if (selectedFactionDefNames?.Any() != true)
            {
                result.Success = false;
                result.ErrorMessage = "No factions selected";
                return result;
            }

            foreach (var factionDefName in selectedFactionDefNames)
            {
                var sourceFaction = sourceFactions.FirstOrDefault(f => f.factionDefName == factionDefName);
                if (sourceFaction == null)
                {
                    result.SkippedFactions.Add(factionDefName);
                    continue;
                }

                var existingFaction = targetPreset.factionGearData
                    .FirstOrDefault(f => f.factionDefName == factionDefName);

                if (existingFaction != null)
                {
                    switch (resolution)
                    {
                        case ExportConflictResolution.Skip:
                            result.SkippedFactions.Add(factionDefName);
                            continue;
                        case ExportConflictResolution.Overwrite:
                            targetPreset.factionGearData.Remove(existingFaction);
                            break;
                        case ExportConflictResolution.Merge:
                            MergeFactionData(existingFaction, sourceFaction);
                            result.ExportedFactions.Add(factionDefName);
                            continue;
                    }
                }

                var clonedFaction = sourceFaction.DeepCopy();
                targetPreset.factionGearData.Add(clonedFaction);
                result.ExportedFactions.Add(factionDefName);
            }

            foreach (var factionData in targetPreset.factionGearData)
            {
                factionData?.ResolveReferences();
            }
            
            targetPreset.CalculateRequiredMods();
            result.Success = result.ExportedFactions.Any();
            return result;
        }

        private static void MergeFactionData(FactionGearData target, FactionGearData source)
        {
            foreach (var sourceKind in source.kindGearData)
            {
                var existingKind = target.kindGearData
                    .FirstOrDefault(k => k.kindDefName == sourceKind.kindDefName);
                if (existingKind == null)
                {
                    target.kindGearData.Add(sourceKind.DeepCopy());
                }
            }

            if (source.groupMakers != null)
            {
                if (target.groupMakers == null)
                {
                    target.groupMakers = new List<PawnGroupMakerData>();
                }
                foreach (var sourceGroup in source.groupMakers)
                {
                    var existingGroup = target.groupMakers
                        .FirstOrDefault(g => g.kindDefName == sourceGroup.kindDefName);
                    if (existingGroup == null)
                    {
                        target.groupMakers.Add(sourceGroup.DeepCopy());
                    }
                }
            }
        }
    }
}