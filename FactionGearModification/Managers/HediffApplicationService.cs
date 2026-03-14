using System;
using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Validation;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.Managers
{
    // Responsibility: Execute validated hediff application with unified safety checks.
    // Dependencies: HediffApplicationValidator, HediffPartResolver, RimWorld health APIs.
    internal static class HediffApplicationService
    {
        public static void ApplyForcedHediff(
            Pawn pawn,
            ForcedHediff forcedHediff,
            Func<HediffPoolType, HediffDef> poolSelector = null)
        {
            if (pawn?.health?.hediffSet == null || forcedHediff == null)
            {
                return;
            }

            var resolvedHediff = ResolveForcedHediff(forcedHediff, poolSelector);
            var def = resolvedHediff?.HediffDef;
            if (def == null)
            {
                return;
            }

            var validation = HediffApplicationValidator.ValidateApplicationToPawn(resolvedHediff, pawn);
            LogValidationMessages(def, validation);
            if (!validation.IsValid)
            {
                return;
            }

            if (!IsHediffSafe(def))
            {
                Log.Warning($"[FactionGearCustomizer] Skipped applying unsafe hediff: {def.defName}");
                return;
            }

            if (HediffNeedsBodyPart(def))
            {
                ApplyWithBodyPart(pawn, resolvedHediff);
            }
            else
            {
                ApplyWithoutBodyPart(pawn, resolvedHediff);
            }
        }

        public static bool IsHediffSafe(HediffDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (def.hediffClass == typeof(Hediff_MissingPart))
            {
                return false;
            }

            string defNameLower = (def.defName ?? string.Empty).ToLowerInvariant();
            if (defNameLower.Contains("missing") || defNameLower.Contains("tendable") || defNameLower.Contains("fatal"))
            {
                return false;
            }

            if (def.stages == null)
            {
                return true;
            }

            foreach (var stage in def.stages)
            {
                if (stage?.capMods == null)
                {
                    continue;
                }

                foreach (var cap in stage.capMods)
                {
                    if (cap?.capacity == null)
                    {
                        continue;
                    }

                    string capacity = cap.capacity.defName.ToLowerInvariant();
                    if ((capacity == "consciousness" || capacity == "moving") && cap.offset < -0.5f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static ForcedHediff ResolveForcedHediff(ForcedHediff source, Func<HediffPoolType, HediffDef> poolSelector)
        {
            if (!source.IsPool)
            {
                return source;
            }

            if (poolSelector == null)
            {
                Log.Warning("[FactionGearCustomizer] Hediff pool requires a selector delegate.");
                return null;
            }

            var selectedDef = poolSelector(source.PoolType);
            if (selectedDef == null)
            {
                return null;
            }

            return new ForcedHediff
            {
                HediffDef = selectedDef,
                chance = 1f,
                maxParts = source.maxParts,
                maxPartsRange = source.maxPartsRange,
                severityRange = source.severityRange,
                parts = source.parts?.ToList()
            };
        }

        private static void LogValidationMessages(HediffDef def, HediffApplicationValidator.ValidationResult validation)
        {
            if (validation.HasWarnings)
            {
                foreach (var warning in validation.Warnings)
                {
                    Log.Warning($"[FactionGearCustomizer] Hediff validation warning for {def.defName}: {warning}");
                }
            }

            if (validation.HasErrors)
            {
                foreach (var error in validation.Errors)
                {
                    Log.Warning($"[FactionGearCustomizer] Hediff validation failed for {def.defName}: {error}");
                }
            }
        }

        private static void ApplyWithoutBodyPart(Pawn pawn, ForcedHediff forcedHediff)
        {
            var def = forcedHediff.HediffDef;
            if (pawn.health.hediffSet.HasHediff(def))
            {
                return;
            }

            try
            {
                var hediff = HediffMaker.MakeHediff(def, pawn);
                ApplySeverity(hediff, forcedHediff);
                if (WouldIncapacitatePawn(pawn, hediff))
                {
                    Log.Warning($"[FactionGearCustomizer] Skipped {def.defName} on {pawn.Name?.ToStringShort ?? "UnknownPawn"} due to safety checks.");
                    return;
                }

                pawn.health.AddHediff(hediff);
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to apply hediff {def.defName} without part: {ex.Message}");
            }
        }

        private static void ApplyWithBodyPart(Pawn pawn, ForcedHediff forcedHediff)
        {
            var def = forcedHediff.HediffDef;
            int targetCount = ResolvePartCount(forcedHediff);
            var resolution = HediffPartResolver.ResolveCandidates(pawn, def, forcedHediff);
            foreach (var warning in resolution.Warnings)
            {
                Log.Warning($"[FactionGearCustomizer] Part resolution warning for {def.defName}: {warning}");
            }

            if (!resolution.HasCandidates)
            {
                Log.Warning($"[FactionGearCustomizer] No valid body part found for {def.defName} on {pawn.Name?.ToStringShort ?? "UnknownPawn"}.");
                return;
            }

            var remainingParts = resolution.Candidates.ToList();
            targetCount = Mathf.Min(targetCount, remainingParts.Count);
            for (int i = 0; i < targetCount && remainingParts.Count > 0; i++)
            {
                var part = remainingParts.RandomElement();
                remainingParts.Remove(part);
                if (part?.def == null || pawn.health.hediffSet.HasHediff(def, part))
                {
                    continue;
                }

                TryApplyToPart(pawn, forcedHediff, part);
            }
        }

        private static int ResolvePartCount(ForcedHediff forcedHediff)
        {
            int count = forcedHediff.maxParts > 0
                ? forcedHediff.maxParts
                : forcedHediff.maxPartsRange.RandomInRange;
            return count <= 0 ? 1 : count;
        }

        private static void TryApplyToPart(Pawn pawn, ForcedHediff forcedHediff, BodyPartRecord part)
        {
            var def = forcedHediff.HediffDef;
            try
            {
                var hediff = HediffMaker.MakeHediff(def, pawn, part);
                ApplySeverity(hediff, forcedHediff);
                if (WouldIncapacitatePawn(pawn, hediff))
                {
                    Log.Warning($"[FactionGearCustomizer] Skipped {def.defName} on {pawn.Name?.ToStringShort ?? "UnknownPawn"} due to safety checks.");
                    return;
                }

                pawn.health.AddHediff(hediff);
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to apply hediff {def.defName} to part {part.Label}: {ex.Message}");
            }
        }

        private static bool HediffNeedsBodyPart(HediffDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (def.hediffClass == typeof(Hediff_Level))
            {
                return false;
            }

            if (def.hediffClass == typeof(Hediff_AddedPart) || def.hediffClass == typeof(Hediff_MissingPart))
            {
                return true;
            }

            if ((def.defName ?? string.Empty).Contains("Missing"))
            {
                return true;
            }

            return def.countsAsAddedPartOrImplant;
        }

        private static bool WouldIncapacitatePawn(Pawn pawn, Hediff newHediff)
        {
            if (pawn == null || newHediff == null)
            {
                return true;
            }

            if (newHediff.def?.hediffClass == typeof(Hediff_MissingPart))
            {
                return true;
            }

            bool added = false;
            try
            {
                pawn.health.AddHediff(newHediff);
                added = true;

                if (pawn.Downed || !pawn.Awake())
                {
                    return true;
                }

                if (newHediff.def != null && newHediff.def.isBad && newHediff.Severity > 0.5f)
                {
                    return true;
                }

                if (!pawn.health.hediffSet.HasHediff(HediffDefOf.MissingBodyPart))
                {
                    return false;
                }

                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff is Hediff_MissingPart missing && missing.Part?.def == BodyPartDefOf.Head)
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                if (added)
                {
                    pawn.health.RemoveHediff(newHediff);
                }
            }
        }

        private static void ApplySeverity(Hediff hediff, ForcedHediff forcedHediff)
        {
            if (hediff == null || forcedHediff.severityRange == default(FloatRange))
            {
                return;
            }

            float severity = forcedHediff.severityRange.RandomInRange;
            if (hediff.def == null)
            {
                hediff.Severity = Mathf.Max(0f, severity);
                return;
            }

            if (hediff.def.lethalSeverity > 0f)
            {
                severity = Mathf.Min(severity, hediff.def.lethalSeverity * 0.95f);
            }

            if (!hediff.def.stages.NullOrEmpty())
            {
                float maxStageSeverity = hediff.def.stages.Max(stage => stage.minSeverity);
                if (maxStageSeverity > 0f && severity > maxStageSeverity)
                {
                    severity = Mathf.Min(severity, maxStageSeverity);
                }
            }

            hediff.Severity = Mathf.Max(0f, severity);
        }
    }
}
