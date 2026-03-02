using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.Validation
{
    public static class HediffApplicationValidator
    {
        public struct ValidationResult
        {
            public bool IsValid;
            public List<string> Errors;
            public List<string> Warnings;

            public bool HasErrors => Errors != null && Errors.Count > 0;
            public bool HasWarnings => Warnings != null && Warnings.Count > 0;

            public static ValidationResult Success()
            {
                return new ValidationResult
                {
                    IsValid = true,
                    Errors = new List<string>(),
                    Warnings = new List<string>()
                };
            }

            public static ValidationResult Fail(string error)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { error },
                    Warnings = new List<string>()
                };
            }
        }

        public static ValidationResult ValidateForcedHediff(ForcedHediff forcedHediff)
        {
            var result = ValidationResult.Success();

            if (forcedHediff == null)
            {
                result.Errors.Add(LanguageManager.Get("Validation_HediffNull"));
                result.IsValid = false;
                return result;
            }

            if (forcedHediff.IsPool)
            {
                ValidatePoolHediff(forcedHediff, result);
            }
            else
            {
                ValidateSingleHediff(forcedHediff, result);
            }

            return result;
        }

        private static void ValidatePoolHediff(ForcedHediff forcedHediff, ValidationResult result)
        {
            if (forcedHediff.PoolType == HediffPoolType.None)
            {
                result.Errors.Add(LanguageManager.Get("Validation_InvalidPoolType"));
                result.IsValid = false;
            }

            if (forcedHediff.chance < 0f || forcedHediff.chance > 1f)
            {
                result.Warnings.Add(LanguageManager.Get("Validation_InvalidChance"));
                forcedHediff.chance = Mathf.Clamp01(forcedHediff.chance);
            }

            ValidateMaxParts(forcedHediff, result);
        }

        private static void ValidateSingleHediff(ForcedHediff forcedHediff, ValidationResult result)
        {
            if (forcedHediff.HediffDef == null)
            {
                if (string.IsNullOrEmpty(forcedHediff.hediffDefName))
                {
                    result.Errors.Add(LanguageManager.Get("Validation_HediffDefNull"));
                    result.IsValid = false;
                }
                else
                {
                    var def = DefDatabase<HediffDef>.GetNamedSilentFail(forcedHediff.hediffDefName);
                    if (def == null)
                    {
                        result.Errors.Add(string.Format(LanguageManager.Get("Validation_HediffDefNotFound"), forcedHediff.hediffDefName));
                        result.IsValid = false;
                    }
                    else
                    {
                        forcedHediff.HediffDef = def;
                        result.Warnings.Add(string.Format(LanguageManager.Get("Validation_HediffDefResolved"), def.defName));
                    }
                }
            }

            if (forcedHediff.HediffDef != null)
            {
                ValidateHediffCompatibility(forcedHediff, result);
            }

            if (forcedHediff.chance < 0f || forcedHediff.chance > 1f)
            {
                result.Warnings.Add(LanguageManager.Get("Validation_InvalidChance"));
                forcedHediff.chance = Mathf.Clamp01(forcedHediff.chance);
            }

            ValidateMaxParts(forcedHediff, result);
            ValidateSeverityRange(forcedHediff, result);
            ValidateBodyParts(forcedHediff, result);
        }

        private static void ValidateHediffCompatibility(ForcedHediff forcedHediff, ValidationResult result)
        {
            var hediffDef = forcedHediff.HediffDef;

            if (hediffDef.hediffClass == null)
            {
                result.Warnings.Add(string.Format(LanguageManager.Get("Validation_NoHediffClass"), hediffDef.defName));
            }

            if (hediffDef.lethalSeverity > 0f)
            {
                result.Warnings.Add(string.Format(LanguageManager.Get("Validation_LethalHediff"), hediffDef.LabelCap, hediffDef.lethalSeverity));
            }
        }

        private static void ValidateMaxParts(ForcedHediff forcedHediff, ValidationResult result)
        {
            if (forcedHediff.maxParts < 0)
            {
                result.Warnings.Add(LanguageManager.Get("Validation_NegativeMaxParts"));
                forcedHediff.maxParts = 0;
            }

            if (forcedHediff.maxPartsRange != default(IntRange))
            {
                if (forcedHediff.maxPartsRange.min < 0)
                {
                    result.Warnings.Add(LanguageManager.Get("Validation_NegativeMaxPartsRange"));
                    forcedHediff.maxPartsRange.min = Math.Max(0, forcedHediff.maxPartsRange.min);
                }

                if (forcedHediff.maxPartsRange.max < forcedHediff.maxPartsRange.min)
                {
                    result.Warnings.Add(LanguageManager.Get("Validation_InvalidMaxPartsRange"));
                    int temp = forcedHediff.maxPartsRange.min;
                    forcedHediff.maxPartsRange.min = forcedHediff.maxPartsRange.max;
                    forcedHediff.maxPartsRange.max = temp;
                }

                if (forcedHediff.maxPartsRange.max > 10)
                {
                    result.Warnings.Add(LanguageManager.Get("Validation_ExcessiveMaxParts"));
                    forcedHediff.maxPartsRange.max = Math.Min(10, forcedHediff.maxPartsRange.max);
                }
            }
        }

        private static void ValidateSeverityRange(ForcedHediff forcedHediff, ValidationResult result)
        {
            if (forcedHediff.severityRange == default(FloatRange))
            {
                return;
            }

            if (forcedHediff.severityRange.min < 0f)
            {
                result.Warnings.Add(LanguageManager.Get("Validation_NegativeSeverityMin"));
                forcedHediff.severityRange.min = Mathf.Max(0f, forcedHediff.severityRange.min);
            }

            if (forcedHediff.severityRange.max > 1f)
            {
                result.Warnings.Add(LanguageManager.Get("Validation_ExcessiveSeverityMax"));
                forcedHediff.severityRange.max = Mathf.Min(1f, forcedHediff.severityRange.max);
            }

            if (forcedHediff.severityRange.max < forcedHediff.severityRange.min)
            {
                result.Warnings.Add(LanguageManager.Get("Validation_InvalidSeverityRange"));
                float temp = forcedHediff.severityRange.min;
                forcedHediff.severityRange.min = forcedHediff.severityRange.max;
                forcedHediff.severityRange.max = temp;
            }
        }

        private static void ValidateBodyParts(ForcedHediff forcedHediff, ValidationResult result)
        {
            if (forcedHediff.partsDefNames == null || forcedHediff.partsDefNames.Count == 0)
            {
                return;
            }

            var validParts = new List<string>();
            foreach (var partDefName in forcedHediff.partsDefNames)
            {
                if (string.IsNullOrEmpty(partDefName))
                {
                    continue;
                }

                var partDef = DefDatabase<BodyPartDef>.GetNamedSilentFail(partDefName);
                if (partDef == null)
                {
                    result.Warnings.Add(string.Format(LanguageManager.Get("Validation_BodyPartNotFound"), partDefName));
                }
                else
                {
                    validParts.Add(partDefName);
                }
            }

            if (validParts.Count != forcedHediff.partsDefNames.Count)
            {
                forcedHediff.partsDefNames = validParts;
                forcedHediff.ResolveReferences();
            }
        }

        public static ValidationResult ValidateApplicationToPawn(ForcedHediff forcedHediff, Pawn pawn)
        {
            var result = ValidationResult.Success();

            if (pawn == null)
            {
                result.Errors.Add(LanguageManager.Get("Validation_PawnNull"));
                result.IsValid = false;
                return result;
            }

            if (pawn.health == null || pawn.health.hediffSet == null)
            {
                result.Errors.Add(LanguageManager.Get("Validation_PawnHealthNull"));
                result.IsValid = false;
                return result;
            }

            if (pawn.RaceProps == null || pawn.RaceProps.body == null)
            {
                result.Errors.Add(LanguageManager.Get("Validation_PawnBodyNull"));
                result.IsValid = false;
                return result;
            }

            var forcedValidation = ValidateForcedHediff(forcedHediff);
            if (!forcedValidation.IsValid)
            {
                result.Errors.AddRange(forcedValidation.Errors);
                result.Warnings.AddRange(forcedValidation.Warnings);
                result.IsValid = false;
            }

            if (forcedHediff.HediffDef != null)
            {
                ValidatePartAvailability(forcedHediff, pawn, result);
                ValidatePartOvercount(forcedHediff, pawn, result);
            }

            return result;
        }

        private static void ValidatePartAvailability(ForcedHediff forcedHediff, Pawn pawn, ValidationResult result)
        {
            var hediffDef = forcedHediff.HediffDef;

            if (!HediffNeedsBodyPart(hediffDef))
            {
                return;
            }

            var availableParts = GetAvailableBodyPartsForHediff(pawn, hediffDef, forcedHediff);

            if (availableParts.Count == 0)
            {
                result.Errors.Add(string.Format(LanguageManager.Get("Validation_NoAvailableParts"), hediffDef.LabelCap));
                result.IsValid = false;
            }
        }

        private static void ValidatePartOvercount(ForcedHediff forcedHediff, Pawn pawn, ValidationResult result)
        {
            var hediffDef = forcedHediff.HediffDef;

            if (!HediffNeedsBodyPart(hediffDef))
            {
                return;
            }

            int requestedCount = forcedHediff.maxParts > 0 ? forcedHediff.maxParts : 
                                  (forcedHediff.maxPartsRange != default(IntRange) ? forcedHediff.maxPartsRange.max : 1);

            var availableParts = GetAvailableBodyPartsForHediff(pawn, hediffDef, forcedHediff);
            int availableCount = availableParts.Count;

            if (requestedCount > availableCount)
            {
                result.Warnings.Add(string.Format(LanguageManager.Get("Validation_InsufficientParts"), 
                    hediffDef.LabelCap, requestedCount, availableCount));
            }
        }

        private static bool HediffNeedsBodyPart(HediffDef def)
        {
            if (def == null) return false;

            if (def.hediffClass == typeof(Hediff_Level)) return false;
            if (def.hediffClass == typeof(Hediff_AddedPart)) return true;
            if (def.hediffClass == typeof(Hediff_MissingPart)) return true;
            if (def.defName.Contains("Missing")) return true;
            if (def.countsAsAddedPartOrImplant) return true;

            return false;
        }

        private static List<BodyPartRecord> GetAvailableBodyPartsForHediff(Pawn pawn, HediffDef hediffDef, ForcedHediff forcedHediff)
        {
            var result = new List<BodyPartRecord>();

            if (pawn?.RaceProps?.body == null || hediffDef == null || pawn.health?.hediffSet == null)
                return result;

            List<BodyPartRecord> candidateParts;

            if (!forcedHediff.parts.NullOrEmpty())
            {
                candidateParts = new List<BodyPartRecord>();
                foreach (var partDef in forcedHediff.parts)
                {
                    if (partDef == null) continue;
                    var parts = pawn.RaceProps.body.GetPartsWithDef(partDef)
                        .Where(p => p != null && !pawn.health.hediffSet.PartIsMissing(p))
                        .ToList();
                    candidateParts.AddRange(parts);
                }
            }
            else
            {
                var recommendedPartNames = HediffPartMappingManager.GetRecommendedPartsForHediff(hediffDef);
                if (recommendedPartNames.Count > 0)
                {
                    candidateParts = new List<BodyPartRecord>();
                    foreach (var partName in recommendedPartNames)
                    {
                        var partDef = DefDatabase<BodyPartDef>.GetNamedSilentFail(partName);
                        if (partDef != null)
                        {
                            var parts = pawn.RaceProps.body.GetPartsWithDef(partDef)
                                .Where(p => p != null && !pawn.health.hediffSet.PartIsMissing(p))
                                .ToList();
                            candidateParts.AddRange(parts);
                        }
                    }
                }
                else
                {
                    candidateParts = pawn.RaceProps.body.AllParts
                        .Where(p => p != null && !pawn.health.hediffSet.PartIsMissing(p))
                        .ToList();
                }
            }

            foreach (var part in candidateParts)
            {
                if (part == null) continue;
                try
                {
                    if (hediffDef.countsAsAddedPartOrImplant || hediffDef.hediffClass == typeof(Hediff_AddedPart))
                    {
                        if (part.def != null && part.def.IsSolid(part, pawn.health.hediffSet.hediffs))
                            continue;
                    }

                    if (!pawn.health.hediffSet.HasHediff(hediffDef, part))
                    {
                        int currentImplantCount = CountImplantsOnPart(pawn, part);
                        int maxCount = HediffPartMappingManager.GetMaxImplantCountForPart(part.def);

                        if (currentImplantCount < maxCount)
                        {
                            result.Add(part);
                        }
                    }
                }
                catch
                {
                }
            }

            return result;
        }

        private static int CountImplantsOnPart(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet?.hediffs == null) return 0;

            int count = 0;
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff?.Part == part && 
                    (hediff.def.countsAsAddedPartOrImplant || hediff.def.hediffClass == typeof(Hediff_AddedPart)))
                {
                    count++;
                }
            }

            return count;
        }

        public static ValidationResult ValidateHediffList(List<ForcedHediff> hediffs)
        {
            var result = ValidationResult.Success();

            if (hediffs == null || hediffs.Count == 0)
            {
                return result;
            }

            var partImplantCount = new Dictionary<string, int>();

            foreach (var hediff in hediffs)
            {
                var singleResult = ValidateForcedHediff(hediff);
                if (!singleResult.IsValid)
                {
                    result.Errors.AddRange(singleResult.Errors);
                    result.IsValid = false;
                }
                result.Warnings.AddRange(singleResult.Warnings);

                if (hediff.HediffDef != null && HediffNeedsBodyPart(hediff.HediffDef))
                {
                    var recommendedParts = HediffPartMappingManager.GetRecommendedPartsForHediff(hediff.HediffDef);
                    foreach (var partName in recommendedParts)
                    {
                        if (!partImplantCount.ContainsKey(partName))
                        {
                            partImplantCount[partName] = 0;
                        }
                        partImplantCount[partName]++;
                    }
                }
            }

            foreach (var kvp in partImplantCount)
            {
                int maxCount = HediffPartMappingManager.GetMaxImplantCountForPart(kvp.Key);
                if (kvp.Value > maxCount)
                {
                    result.Warnings.Add(string.Format(LanguageManager.Get("Validation_TooManyImplantsForPart"), 
                        kvp.Key, kvp.Value, maxCount));
                }
            }

            return result;
        }
    }
}
