using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Validation;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Managers
{
    // Responsibility: Resolve valid body part targets for a hediff using strict source priority.
    // Dependencies: RecipeDef metadata, HediffPartMappingManager, Pawn health/body state.
    internal enum HediffPartSource
    {
        None,
        Manual,
        Recipe,
        Mapping,
        Inference
    }

    internal sealed class PartResolutionResult
    {
        public PartResolutionResult(HediffPartSource source, List<BodyPartRecord> candidates, List<string> warnings)
        {
            Source = source;
            Candidates = candidates ?? new List<BodyPartRecord>();
            Warnings = warnings ?? new List<string>();
        }

        public HediffPartSource Source { get; }
        public List<BodyPartRecord> Candidates { get; }
        public List<string> Warnings { get; }
        public bool HasCandidates => Candidates.Count > 0;
    }

    internal static class HediffPartResolver
    {
        public static PartResolutionResult ResolveCandidates(Pawn pawn, HediffDef hediffDef, ForcedHediff forcedHediff)
        {
            var warnings = new List<string>();
            if (pawn?.health?.hediffSet == null || pawn.RaceProps?.body == null || hediffDef == null)
            {
                warnings.Add("Part resolver received invalid pawn/body/hediff context.");
                return new PartResolutionResult(HediffPartSource.None, new List<BodyPartRecord>(), warnings);
            }

            if (!forcedHediff.parts.NullOrEmpty())
            {
                var manualCandidates = ResolveManualCandidates(pawn, forcedHediff);
                var filteredManual = FilterCandidates(pawn, hediffDef, manualCandidates);
                if (filteredManual.Count == 0)
                {
                    warnings.Add($"Manual part selection has no valid target for {hediffDef.defName}.");
                }
                return new PartResolutionResult(HediffPartSource.Manual, filteredManual, warnings);
            }

            var recipeCandidates = ResolveByRecipe(pawn, hediffDef);
            var filteredRecipe = FilterCandidates(pawn, hediffDef, recipeCandidates);
            if (filteredRecipe.Count > 0)
            {
                return new PartResolutionResult(HediffPartSource.Recipe, filteredRecipe, warnings);
            }

            var explicitMappingCandidates = ResolveByExplicitMapping(pawn, hediffDef);
            var filteredMapping = FilterCandidates(pawn, hediffDef, explicitMappingCandidates);
            if (filteredMapping.Count > 0)
            {
                return new PartResolutionResult(HediffPartSource.Mapping, filteredMapping, warnings);
            }

            var inferredCandidates = ResolveByInferenceMapping(pawn, hediffDef);
            var filteredInference = FilterCandidates(pawn, hediffDef, inferredCandidates);
            if (filteredInference.Count > 0)
            {
                return new PartResolutionResult(HediffPartSource.Inference, filteredInference, warnings);
            }

            warnings.Add($"No valid body parts resolved for {hediffDef.defName}.");
            return new PartResolutionResult(HediffPartSource.None, new List<BodyPartRecord>(), warnings);
        }

        private static List<BodyPartRecord> ResolveManualCandidates(Pawn pawn, ForcedHediff forcedHediff)
        {
            var result = new List<BodyPartRecord>();
            foreach (var partDef in forcedHediff.parts.Where(def => def != null))
            {
                AddDistinctParts(result, GetAvailablePartsByDef(pawn, partDef));
            }
            return result;
        }

        private static List<BodyPartRecord> ResolveByRecipe(Pawn pawn, HediffDef hediffDef)
        {
            var result = new List<BodyPartRecord>();
            var recipes = DefDatabase<RecipeDef>.AllDefsListForReading
                .Where(r => r != null && r.addsHediff == hediffDef);

            foreach (var recipe in recipes)
            {
                var fixedParts = recipe.appliedOnFixedBodyParts ?? new List<BodyPartDef>();
                foreach (var partDef in fixedParts.Where(def => def != null))
                {
                    AddDistinctParts(result, GetAvailablePartsByDef(pawn, partDef));
                }

                var fixedGroups = recipe.appliedOnFixedBodyPartGroups ?? new List<BodyPartGroupDef>();
                foreach (var groupDef in fixedGroups.Where(def => def != null))
                {
                    var groupParts = pawn.RaceProps.body.AllParts
                        .Where(p => p != null && p.groups != null && p.groups.Contains(groupDef));
                    AddDistinctParts(result, groupParts);
                }
            }

            return result;
        }

        private static List<BodyPartRecord> ResolveByExplicitMapping(Pawn pawn, HediffDef hediffDef)
        {
            var allMappings = HediffPartMappingManager.GetAllMappings();
            if (!allMappings.TryGetValue(hediffDef.defName, out var partNames) || partNames.NullOrEmpty())
            {
                return new List<BodyPartRecord>();
            }

            return ConvertPartNamesToRecords(pawn, partNames);
        }

        private static List<BodyPartRecord> ResolveByInferenceMapping(Pawn pawn, HediffDef hediffDef)
        {
            var allMappings = HediffPartMappingManager.GetAllMappings();
            if (allMappings.ContainsKey(hediffDef.defName))
            {
                return new List<BodyPartRecord>();
            }

            var inferredNames = HediffPartMappingManager.GetRecommendedPartsForHediff(hediffDef);
            if (inferredNames.NullOrEmpty())
            {
                return new List<BodyPartRecord>();
            }

            return ConvertPartNamesToRecords(pawn, inferredNames);
        }

        private static List<BodyPartRecord> ConvertPartNamesToRecords(Pawn pawn, IEnumerable<string> partNames)
        {
            var result = new List<BodyPartRecord>();
            foreach (var partName in partNames.Where(name => !string.IsNullOrEmpty(name)))
            {
                var partDef = DefDatabase<BodyPartDef>.GetNamedSilentFail(partName);
                if (partDef == null)
                {
                    continue;
                }

                AddDistinctParts(result, GetAvailablePartsByDef(pawn, partDef));
            }

            return result;
        }

        private static IEnumerable<BodyPartRecord> GetAvailablePartsByDef(Pawn pawn, BodyPartDef partDef)
        {
            return pawn.RaceProps.body.GetPartsWithDef(partDef)
                .Where(p => p != null && !pawn.health.hediffSet.PartIsMissing(p));
        }

        private static List<BodyPartRecord> FilterCandidates(Pawn pawn, HediffDef hediffDef, IEnumerable<BodyPartRecord> rawCandidates)
        {
            var result = new List<BodyPartRecord>();
            foreach (var part in rawCandidates.Where(p => p != null))
            {
                if (part.def == null || pawn.health.hediffSet.PartIsMissing(part))
                {
                    continue;
                }

                if (pawn.health.hediffSet.HasHediff(hediffDef, part))
                {
                    continue;
                }

                if (hediffDef.countsAsAddedPartOrImplant || hediffDef.hediffClass == typeof(Hediff_AddedPart))
                {
                    if (part.def.IsSolid(part, pawn.health.hediffSet.hediffs))
                    {
                        continue;
                    }

                    int currentImplants = CountImplantsOnPart(pawn, part);
                    int maxImplants = HediffPartMappingManager.GetMaxImplantCountForPart(part.def);
                    if (currentImplants >= maxImplants)
                    {
                        continue;
                    }
                }

                if (!result.Contains(part))
                {
                    result.Add(part);
                }
            }

            return result;
        }

        private static int CountImplantsOnPart(Pawn pawn, BodyPartRecord part)
        {
            int count = 0;
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff?.Part != part || hediff.def == null)
                {
                    continue;
                }

                if (hediff.def.countsAsAddedPartOrImplant || hediff.def.hediffClass == typeof(Hediff_AddedPart))
                {
                    count++;
                }
            }

            return count;
        }

        private static void AddDistinctParts(List<BodyPartRecord> target, IEnumerable<BodyPartRecord> source)
        {
            foreach (var part in source.Where(p => p != null))
            {
                if (!target.Contains(part))
                {
                    target.Add(part);
                }
            }
        }
    }
}
