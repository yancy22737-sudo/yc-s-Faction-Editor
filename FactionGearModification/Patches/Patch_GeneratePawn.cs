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
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    [HarmonyPriority(Priority.Low)]
    public static class Patch_GeneratePawn
    {
        [ThreadStatic]
        private static bool isApplyingGear;

        public static void Prefix(ref PawnGenerationRequest request)
        {
            // 应用异种设置（在 Pawn 生成前）
            if (ModsConfig.BiotechActive && request.KindDef != null && request.Faction != null)
            {
                ApplyXenotypeSettings(request.Faction, request.KindDef, ref request);
            }

            // 应用年龄限制（在 Pawn 生成前）
            if (request.KindDef != null && request.Faction != null)
            {
                ApplyAgeSettings(ref request);
            }
        }

        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            if (isApplyingGear)
            {
                return;
            }

            // 在世界生成阶段，玩家派系尚未创建，Faction.OfPlayer 会报错
            // 因此跳过此阶段的装备应用
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

            // Ensure XenotypeSet exists, otherwise create it (fixes preview issues for factions without default xenotypes)
            FactionDefManager.EnsureXenotypeSetExists(faction.def);

            // 从 GameComponent 获取数据（与 ApplyAllSettings 保持一致）
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

            ApplyFactionXenotypeSettings(faction, factionData);

            if (HasKindXenotypeSettings(kindData))
            {
                ApplyKindXenotypeSettings(faction, kindData);
            }

            // 检查当前应用后的异种设置是否包含非暴力异种
            // 如果包含，且请求要求暴力，则放宽限制，允许生成非暴力 Pawn（避免生成失败）
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
                            // 检查基因是否包含 IncapableOfViolence 或者 xenotype 本身不能作为战斗人员生成
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
            if (kindData == null) return false;
            
            if (!string.IsNullOrEmpty(kindData.ForcedXenotype)) return true;
            if (kindData.DisableXenotypeChances) return true;
            if (kindData.XenotypeChances != null && kindData.XenotypeChances.Count > 0) return true;
            
            return false;
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

            // 强制异种：最高优先级
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

            // 禁用异种概率控制：禁用派系级别设置，应用兵种自定义概率
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

            // 自定义异种概率
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
                // If no original data, we should probably clear it, effectively making it Baseliner-only
                // unless the faction is supposed to have some default generation which we don't know about.
                // But since we are here, we are assuming control.
                LogUtils.DebugLog($"Cleared xenotype chances for {faction.def.defName} (no original data)");
            }
        }

        /// <summary>
        /// 应用年龄限制设置到 PawnGenerationRequest
        /// </summary>
        private static void ApplyAgeSettings(ref PawnGenerationRequest request)
        {
            if (request.KindDef == null || request.Faction == null) return;

            // 获取运行时生效的数据：存档优先，其次全局设置
            var factionData = GetRuntimeFactionData(request.Faction);
            if (factionData == null) return;

            string kindDefName = request.KindDef.defName;
            
            float? minAge = null;
            float? maxAge = null;

            // 1. 优先检查兵种专属配置（KindGearData）
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

            // 2. 在所有群组中查找该兵种的年龄设置（兼容旧逻辑）
            foreach (var groupMaker in factionData.groupMakers)
            {
                if (groupMaker?.options == null) continue;

                foreach (var option in groupMaker.options)
                {
                    if (option?.kindDefName == kindDefName)
                    {
                        // 找到匹配的配置
                        if (option.minAge.HasValue)
                            minAge = option.minAge.Value;
                        if (option.maxAge.HasValue)
                            maxAge = option.maxAge.Value;

                        // 如果找到了年龄设置，应用它
                        if (minAge.HasValue || maxAge.HasValue)
                        {
                            ApplyAgeToRequest(ref request, minAge, maxAge, kindDefName);
                            return;
                        }
                    }
                }

                // 同时检查 traders, carriers, guards
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
            float effectiveMax = Mathf.Max(0f, maxAge ?? 999f);
            if (effectiveMax < effectiveMin)
            {
                float temp = effectiveMin;
                effectiveMin = effectiveMax;
                effectiveMax = temp;
            }

            if (!request.FixedBiologicalAge.HasValue)
            {
                float targetAge = Rand.Range(effectiveMin, effectiveMax);
                request.FixedBiologicalAge = targetAge;
                request.FixedChronologicalAge = targetAge;
                LogUtils.DebugLog($"Applied age settings for {kindDefName}: {targetAge:F1} (range: {effectiveMin:F1}-{effectiveMax:F1})");
            }
        }

        /// <summary>
        /// 在列表中检查年龄设置
        /// </summary>
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
    }
}
