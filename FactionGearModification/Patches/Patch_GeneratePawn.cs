using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using FactionGearCustomizer.Core;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.Utils;

namespace FactionGearCustomizer
{
    /// <summary>
    /// Vanilla CanGenerateFrom rejects faction combat pawns at tiles without
    /// settlements (requires wild animals or a settlement at the target tile).
    /// Custom group makers replace vanilla groups entirely, so faction pawns
    /// can never pass this check. This patch re-evaluates CanGenerateFrom
    /// without the tile constraint for factions with custom groupMakers.
    /// </summary>
    [HarmonyPatch(typeof(PawnGroupMaker), "CanGenerateFrom")]
    public static class Patch_CanGenerateFrom_TileBypass
    {
        public static void Postfix(PawnGroupMaker __instance, PawnGroupMakerParms parms, ref bool __result)
        {
            if (__result) return;
            if (parms.tile == -1) return;
            if (parms.faction?.def == null) return;
            if (__instance.options.NullOrEmpty()) return;

            string defName = parms.faction.def.defName;

            // 优先使用存档级数据，回退到全局设置
            var gameComponent = FactionGearGameComponent.Instance;
            var saveData = gameComponent?.GetActiveFactionGearData();
            FactionGearData data = null;
            if (saveData != null)
                data = saveData.FirstOrDefault(f => f.factionDefName == defName);
            if (data == null)
                data = FactionGearCustomizerMod.Settings?.TryGetFactionData(defName);

            if (data?.groupMakers == null || data.groupMakers.Count == 0)
            {
                LogUtils.DebugLog($"[TileBypass] No custom groupMakers for {defName} — result={__result}");
                return;
            }

            // Re-check non-tile conditions
            if (parms.points > 0f && __instance.maxTotalPoints > 0f && parms.points > __instance.maxTotalPoints)
            {
                LogUtils.DebugLog($"[TileBypass] {defName}: points={parms.points} > maxTotalPoints={__instance.maxTotalPoints} — blocked");
                return;
            }
            if (parms.generateFightersOnly && !__instance.options.Any(o => o.kind.isFighter))
                return;

            LogUtils.DebugLog($"[TileBypass] Allowed {defName}/{__instance.kindDef?.defName} for tile={parms.tile}, points={parms.points}");
            __result = true;
        }
    }

    /// <summary>
    /// Force-generate mechanoid bosses from custom group makers during raids.
    /// Vanilla RimWorld filters out boss mechs (Apocriton, War Queen, Diabolus)
    /// from PawnGroupMaker-based generation. This patch appends them after normal generation.
    /// </summary>
    [HarmonyPatch(typeof(PawnGroupMakerUtility), "GeneratePawns")]
    public static class Patch_PawnGroupMakerUtility_GeneratePawns_Fix
    {
        public static void Postfix(PawnGroupMakerParms parms, ref IEnumerable<Pawn> __result)
        {
            if (parms.faction?.def == null || __result == null) return;

            string defName = parms.faction.def.defName;
            var gameComponent = FactionGearGameComponent.Instance;
            var saveData = gameComponent?.GetActiveFactionGearData();
            FactionGearData data = null;
            if (saveData != null)
                data = saveData.FirstOrDefault(f => f.factionDefName == defName);
            if (data == null)
                data = FactionGearCustomizerMod.Settings?.TryGetFactionData(defName);

            if (data?.groupMakers == null) return;

            var bosses = new List<(PawnKindDef kind, float cost)>();
            var seen = new HashSet<string>();
            foreach (var gm in data.groupMakers)
            {
                if (gm.options == null) continue;
                foreach (var opt in gm.options)
                {
                    if (!opt.kindDefName.StartsWith("Mech_")) continue;
                    if (seen.Contains(opt.kindDefName)) continue;
                    seen.Add(opt.kindDefName);
                    var kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(opt.kindDefName);
                    if (kind != null)
                        bosses.Add((kind, kind.combatPower));
                }
            }
            if (bosses.Count == 0) return;

            LogUtils.DebugLog($"[MechBoss] Force-generating {bosses.Count} mech bosses for {defName}");
            var original = __result;
            __result = WrapWithBosses(original, parms.faction, bosses);
        }

        private static IEnumerable<Pawn> WrapWithBosses(IEnumerable<Pawn> original, Faction faction, List<(PawnKindDef kind, float cost)> bosses)
        {
            foreach (var pawn in original)
                yield return pawn;

            foreach (var b in bosses)
            {
                Pawn p = null;
                try
                {
                    var request = new PawnGenerationRequest(b.kind, faction,
                        PawnGenerationContext.NonPlayer, -1, true, false, false, false,
                        true, 1f, false, true, true, false, false);
                    p = PawnGenerator.GeneratePawn(request);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[FactionGearCustomizer] Failed to force-generate {b.kind.defName}: {ex.Message}");
                }
                if (p != null)
                {
                    LogUtils.DebugLog($"Force-generated mech boss: {p.kindDef.defName} for {faction.Name}");
                    yield return p;
                }
            }
        }
    }

    /// <summary>
    /// 临时覆盖 PawnKindDef.combatPower 以支持 pointsOverride。
    /// 原版 PawnGenOption 没有点数覆盖字段，PawnGroupMakerUtility.GeneratePawns
    /// 直接读取 kind.combatPower。GeneratePawns 是迭代器方法，实际的 pawn 生成
    /// 发生在 IEnumerable 被枚举时，而非方法调用时。因此必须用 Postfix 包装
    /// __result，在枚举期间维持 combatPower 覆盖。
    /// </summary>
    [HarmonyPatch(typeof(PawnGroupMakerUtility), "GeneratePawns")]
    [HarmonyPriority(Priority.First)]
    public static class Patch_GeneratePawns_CombatPowerOverride
    {
        public static void Postfix(PawnGroupMakerParms parms, ref IEnumerable<Pawn> __result)
        {
            if (parms.faction?.def == null || __result == null) return;
            string defName = parms.faction.def.defName;

            // 优先使用存档级数据，回退到全局设置
            var gameComponent = FactionGearGameComponent.Instance;
            var saveData = gameComponent?.GetActiveFactionGearData();
            FactionGearData data = null;
            if (saveData != null)
            {
                data = saveData.FirstOrDefault(f => f.factionDefName == defName);
                LogUtils.DebugLog($"[CombatPowerOverride] save-level data: {saveData.Count} entries, found={data != null}");
            }
            if (data == null)
            {
                data = FactionGearCustomizerMod.Settings?.TryGetFactionData(defName);
                LogUtils.DebugLog($"[CombatPowerOverride] global settings: found={data != null}");
            }

            if (data == null) return;

            // 收集所有需要覆盖的 combatPower
            var overrides = new Dictionary<PawnKindDef, float>();   // kind -> new value
            var originals = new Dictionary<PawnKindDef, float>();    // kind -> original value

            // 来源1：PawnGenOptionData 的手动 pointsOverride
            if (data.groupMakers != null)
            {
                foreach (var gm in data.groupMakers)
                {
                    CollectOverridesFromOptions(gm.options, overrides, originals);
                    CollectOverridesFromOptions(gm.traders, overrides, originals);
                    CollectOverridesFromOptions(gm.carriers, overrides, originals);
                    CollectOverridesFromOptions(gm.guards, overrides, originals);
                }
            }

            // 来源2：KindGearData 的自动计算 RaidPointsOverride
            if (data.kindGearData != null)
            {
                foreach (var kindData in data.kindGearData)
                {
                    if (!kindData.RaidPointsOverride.HasValue) continue;
                    var kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
                    if (kind == null) continue;
                    if (!originals.ContainsKey(kind))
                        originals[kind] = kind.combatPower;
                    overrides[kind] = kindData.RaidPointsOverride.Value;
                }
            }

            if (overrides.Count == 0)
            {
                LogUtils.DebugLog($"[CombatPowerOverride] {defName}: no pointsOverride entries found");
                return;
            }

            LogUtils.DebugLog($"[CombatPowerOverride] {defName}: wrapping iterator with {overrides.Count} combatPower overrides, points={parms.points:F0}");
            foreach (var kv in overrides)
                LogUtils.DebugLog($"[CombatPowerOverride]   {kv.Key.defName}: {originals[kv.Key]:F0} -> {kv.Value:F0}");

            var original = __result;
            __result = WrapWithCombatPower(original, overrides, originals);
        }

        private static void CollectOverridesFromOptions(List<PawnGenOptionData> list,
            Dictionary<PawnKindDef, float> overrides, Dictionary<PawnKindDef, float> originals)
        {
            if (list == null) return;
            foreach (var opt in list)
            {
                if (!opt.pointsOverride.HasValue) continue;
                var kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(opt.kindDefName);
                if (kind == null) continue;
                if (!originals.ContainsKey(kind))
                    originals[kind] = kind.combatPower;
                overrides[kind] = opt.pointsOverride.Value;
            }
        }

        private static IEnumerable<Pawn> WrapWithCombatPower(
            IEnumerable<Pawn> inner,
            Dictionary<PawnKindDef, float> overrides,
            Dictionary<PawnKindDef, float> originals)
        {
            // 在枚举开始前应用覆盖
            foreach (var kv in overrides)
                kv.Key.combatPower = kv.Value;
            LogUtils.DebugLog("[CombatPowerOverride] Applied overrides, starting enumeration...");

            try
            {
                foreach (var pawn in inner)
                    yield return pawn;
            }
            finally
            {
                // 枚举结束后（正常或异常）恢复原始值
                foreach (var kv in originals)
                    kv.Key.combatPower = kv.Value;
                LogUtils.DebugLog($"[CombatPowerOverride] Restored {originals.Count} combatPower values after enumeration");
            }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    [HarmonyPriority(Priority.Low)]
    public static class Patch_GeneratePawn
    {
        [ThreadStatic]
        private static bool isApplyingGear;

        private const float DefaultMaxAge = 999f;

        public static void Prefix(ref PawnGenerationRequest request)
        {
            try
            {
                if (ModsConfig.BiotechActive && request.KindDef != null && request.Faction != null)
                {
                    ApplyXenotypeSettings(request.Faction, request.KindDef, ref request);
                }

                if (request.KindDef != null && request.Faction != null)
                {
                    ApplyAgeSettings(ref request);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error in Prefix for {request.KindDef?.defName}: {ex.Message}");
            }
        }

        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            if (isApplyingGear)
            {
                return;
            }

            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            if (__result != null && __result.RaceProps != null && __result.RaceProps.Humanlike)
            {
                Faction faction = request.Faction ?? __result.Faction;
                if (faction != null)
                {
                    try
                    {
                        isApplyingGear = true;

                        var kindData = GetKindData(faction, request.KindDef);
                        if (kindData != null)
                        {
                            TraitApplicationService.ApplyTraits(__result, kindData);
                            SkillApplicationService.ApplySkills(__result, kindData);
                            GeneApplicationService.ApplyGenes(__result, kindData);
                            AppearanceApplicationService.ApplyAppearance(__result, kindData);
                        }

                        GearApplier.ApplyCustomGear(__result, faction);
                    }
                    finally
                    {
                        isApplyingGear = false;
                    }
                }
            }
        }

        private static void ApplyXenotypeSettings(Faction faction, PawnKindDef kindDef, ref PawnGenerationRequest request)
        {
            if (faction == null || kindDef == null || !faction.def.humanlikeFaction) return;

            var gameComponent = FactionGearGameComponent.Instance;
            var saveData = gameComponent?.GetActiveFactionGearData();

            FactionGearData factionData;
            if (saveData != null)
            {
                factionData = saveData.FirstOrDefault(f => f.factionDefName == faction.def.defName);
            }
            else
            {
                factionData = FactionGearCustomizerMod.Settings?.TryGetFactionData(faction.def.defName);
            }

            var kindData = factionData?.GetKindData(kindDef.defName);
            if (!ShouldApplyXenotypeSettings(factionData, kindData)) return;

            FactionDefManager.EnsureXenotypeSetExists(faction.def);

            ApplyFactionXenotypeSettings(faction, factionData);

            if (HasKindXenotypeSettings(kindData))
            {
                ApplyKindXenotypeSettings(faction, kindData);
            }

            if (request.MustBeCapableOfViolence)
            {
                var chances = FactionDefManager.GetXenotypeChances(faction.def.xenotypeSet);
                if (chances != null)
                {
                    bool hasNonViolenceXenotype = false;
                    foreach (var chance in chances)
                    {
                        if (chance.xenotype != null && chance.chance > 0)
                        {
                            if (!chance.xenotype.canGenerateAsCombatant ||
                                (chance.xenotype.genes != null && chance.xenotype.genes.Any(g => g.defName == "ViolenceDisabled")))
                            {
                                hasNonViolenceXenotype = true;
                                break;
                            }
                        }
                    }

                    if (hasNonViolenceXenotype)
                    {
                        request.MustBeCapableOfViolence = false;
                        LogUtils.DebugLog($"Relaxed violence requirement for {kindDef.defName} due to non-violent xenotype settings.");
                    }
                }
            }
        }

        private static bool HasKindXenotypeSettings(KindGearData kindData)
        {
            if (kindData == null || !kindData.isModified) return false;

            if (!string.IsNullOrEmpty(kindData.ForcedXenotype)) return true;
            if (kindData.DisableXenotypeChances) return true;
            if (kindData.XenotypeChances != null && kindData.XenotypeChances.Count > 0) return true;

            return false;
        }

        private static bool ShouldApplyXenotypeSettings(FactionGearData factionData, KindGearData kindData)
        {
            bool hasFactionOverrides = factionData != null &&
                                       factionData.isModified &&
                                       (factionData.DisableXenotypeChances ||
                                        (factionData.XenotypeChances != null && factionData.XenotypeChances.Count > 0));

            return hasFactionOverrides || HasKindXenotypeSettings(kindData);
        }

        private static void ApplyKindXenotypeSettings(Faction faction, KindGearData kindData)
        {
            if (faction?.def?.xenotypeSet == null) return;

            var chances = FactionDefManager.GetXenotypeChances(faction.def.xenotypeSet);
            if (chances == null)
            {
                chances = new List<XenotypeChance>();
                FactionDefManager.SetXenotypeChances(faction.def.xenotypeSet, chances);
            }

            if (!string.IsNullOrEmpty(kindData.ForcedXenotype))
            {
                XenotypeDef forcedXeno = DefDatabase<XenotypeDef>.GetNamedSilentFail(kindData.ForcedXenotype);
                if (forcedXeno != null)
                {
                    chances.Clear();
                    chances.Add(new XenotypeChance(forcedXeno, 1.0f));
                    LogUtils.DebugLog($"Forced xenotype {forcedXeno.defName} for {faction.def.defName}");
                }
                return;
            }

            if (kindData.DisableXenotypeChances)
            {
                if (kindData.XenotypeChances != null && kindData.XenotypeChances.Count > 0)
                {
                    ReplaceWithCustomChances(chances, kindData.XenotypeChances);
                    LogUtils.DebugLog($"Applied kind xenotype chances (disabled faction-level) for {faction.def.defName}");
                }
                else
                {
                    RestoreOriginalXenotypeChances(faction, chances);
                    LogUtils.DebugLog($"Kind xenotype disabled, restoring native faction settings for {faction.def.defName}");
                }
                return;
            }

            if (kindData.XenotypeChances != null && kindData.XenotypeChances.Count > 0)
            {
                ReplaceWithCustomChances(chances, kindData.XenotypeChances);
                LogUtils.DebugLog($"Applied kind xenotype chances for {faction.def.defName}");
            }
        }

        private static void ApplyFactionXenotypeSettings(Faction faction, FactionGearData factionData)
        {
            if (faction?.def?.xenotypeSet == null) return;

            var chances = FactionDefManager.GetXenotypeChances(faction.def.xenotypeSet);
            if (chances == null)
            {
                chances = new List<XenotypeChance>();
                FactionDefManager.SetXenotypeChances(faction.def.xenotypeSet, chances);
            }

            if (factionData == null || !factionData.isModified)
            {
                RestoreOriginalXenotypeChances(faction, chances);
                return;
            }

            if (factionData.DisableXenotypeChances)
            {
                RestoreOriginalXenotypeChances(faction, chances);
                LogUtils.DebugLog($"Restored original xenotype chances for {faction.def.defName} (faction-level disabled)");
                return;
            }

            if (factionData.XenotypeChances != null && factionData.XenotypeChances.Count > 0)
            {
                ReplaceWithCustomChances(chances, factionData.XenotypeChances);
                LogUtils.DebugLog($"Applied faction xenotype chances for {faction.def.defName}");
                return;
            }

            RestoreOriginalXenotypeChances(faction, chances);
        }

        private static void ReplaceWithCustomChances(List<XenotypeChance> target, Dictionary<string, float> source)
        {
            target.Clear();
            if (source == null) return;

            foreach (var kvp in source)
            {
                XenotypeDef xDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(kvp.Key);
                if (xDef != null)
                {
                    target.Add(new XenotypeChance(xDef, kvp.Value));
                }
            }
        }

        private static void RestoreOriginalXenotypeChances(Faction faction, List<XenotypeChance> target)
        {
            if (faction?.def == null) return;
            var originalFactionData = FactionDefManager.GetOriginalXenotypeChances(faction.def);
            target.Clear();

            if (originalFactionData != null && originalFactionData.Count > 0)
            {
                foreach (var originalChance in originalFactionData)
                {
                    if (originalChance?.xenotype != null)
                    {
                        target.Add(new XenotypeChance(originalChance.xenotype, originalChance.chance));
                    }
                }
                LogUtils.DebugLog($"Restored original xenotype chances for {faction.def.defName}");
            }
            else
            {
                LogUtils.DebugLog($"Cleared xenotype chances for {faction.def.defName} (no original data)");
            }
        }

        private static void ApplyAgeSettings(ref PawnGenerationRequest request)
        {
            if (request.KindDef == null || request.Faction == null) return;

            var factionData = GetRuntimeFactionData(request.Faction);
            if (factionData == null) return;

            string kindDefName = request.KindDef.defName;

            float? minAge = null;
            float? maxAge = null;

            var kindData = factionData.GetKindData(kindDefName);
            if (kindData != null)
            {
                if (kindData.MinAge.HasValue) minAge = kindData.MinAge.Value;
                if (kindData.MaxAge.HasValue) maxAge = kindData.MaxAge.Value;

                if (minAge.HasValue || maxAge.HasValue)
                {
                    ApplyAgeToRequest(ref request, minAge, maxAge, kindDefName);
                    return;
                }
            }

            if (factionData?.groupMakers == null || factionData.groupMakers.Count == 0) return;

            foreach (var groupMaker in factionData.groupMakers)
            {
                if (groupMaker?.options == null) continue;

                foreach (var option in groupMaker.options)
                {
                    if (option?.kindDefName == kindDefName)
                    {
                        if (option.minAge.HasValue)
                            minAge = option.minAge.Value;
                        if (option.maxAge.HasValue)
                            maxAge = option.maxAge.Value;

                        if (minAge.HasValue || maxAge.HasValue)
                        {
                            ApplyAgeToRequest(ref request, minAge, maxAge, kindDefName);
                            return;
                        }
                    }
                }

                CheckAgeInList(groupMaker.traders, kindDefName, ref minAge, ref maxAge);
                CheckAgeInList(groupMaker.carriers, kindDefName, ref minAge, ref maxAge);
                CheckAgeInList(groupMaker.guards, kindDefName, ref minAge, ref maxAge);

                if (minAge.HasValue || maxAge.HasValue)
                {
                    ApplyAgeToRequest(ref request, minAge, maxAge, kindDefName);
                    return;
                }
            }
        }

        private static void ApplyAgeToRequest(ref PawnGenerationRequest request, float? minAge, float? maxAge, string kindDefName)
        {
            float effectiveMin = Mathf.Max(0f, minAge ?? 0f);
            float effectiveMax = Mathf.Max(0f, maxAge ?? DefaultMaxAge);
            if (effectiveMax < effectiveMin)
            {
                float temp = effectiveMin;
                effectiveMin = effectiveMax;
                effectiveMax = temp;
            }

            float minAdultAge = GetMinAdultAge(request.KindDef);
            effectiveMin = Mathf.Max(effectiveMin, minAdultAge);
            if (effectiveMax < effectiveMin)
            {
                effectiveMax = effectiveMin;
            }

            if (!request.FixedBiologicalAge.HasValue)
            {
                float targetAge = Rand.Range(effectiveMin, effectiveMax);
                request.FixedBiologicalAge = targetAge;
                request.FixedChronologicalAge = targetAge;
                LogUtils.DebugLog($"Applied age settings for {kindDefName}: {targetAge:F1} (range: {effectiveMin:F1}-{effectiveMax:F1}, minAdult: {minAdultAge:F1})");
            }
        }

        private static float GetMinAdultAge(PawnKindDef kindDef)
        {
            if (kindDef?.race?.race?.lifeStageAges == null) return 18f;

            foreach (var lsa in kindDef.race.race.lifeStageAges)
            {
                if (lsa.def?.defName == "HumanlikeAdult" || lsa.def?.defName == "Adult")
                {
                    return lsa.minAge;
                }
            }

            float maxMinAge = 0f;
            foreach (var lsa in kindDef.race.race.lifeStageAges)
            {
                if (lsa.minAge > maxMinAge)
                {
                    maxMinAge = lsa.minAge;
                }
            }

            return maxMinAge > 0f ? maxMinAge : 18f;
        }

        private static void CheckAgeInList(System.Collections.Generic.List<PawnGenOptionData> list, string kindDefName, ref float? minAge, ref float? maxAge)
        {
            if (list == null) return;

            foreach (var option in list)
            {
                if (option?.kindDefName == kindDefName)
                {
                    if (option.minAge.HasValue)
                        minAge = option.minAge.Value;
                    if (option.maxAge.HasValue)
                        maxAge = option.maxAge.Value;
                    return;
                }
            }
        }

        private static FactionGearData GetRuntimeFactionData(Faction faction)
        {
            if (faction?.def == null) return null;

            var gameComponent = FactionGearGameComponent.Instance;
            var saveData = gameComponent?.GetActiveFactionGearData();
            if (saveData != null)
            {
                return saveData.FirstOrDefault(f => f.factionDefName == faction.def.defName);
            }

            return FactionGearCustomizerMod.Settings?.TryGetFactionData(faction.def.defName);
        }

        private static KindGearData GetKindData(Faction faction, PawnKindDef kindDef)
        {
            if (faction?.def == null || kindDef == null) return null;
            var factionData = GetRuntimeFactionData(faction);
            return factionData?.GetKindData(kindDef.defName);
        }
    }
}
