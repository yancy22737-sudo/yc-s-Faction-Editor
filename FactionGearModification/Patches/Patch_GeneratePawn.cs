using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
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

        public static void Prefix(PawnGenerationRequest request)
        {
            // 应用异种设置（在 Pawn 生成前）
            if (ModsConfig.BiotechActive && request.KindDef != null && request.Faction != null)
            {
                ApplyXenotypeSettings(request.Faction, request.KindDef);
            }

            // 应用年龄限制（在 Pawn 生成前）
            if (request.KindDef != null && request.Faction != null)
            {
                ApplyAgeSettings(request);
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

        private static void ApplyXenotypeSettings(Faction faction, PawnKindDef kindDef)
        {
            if (faction == null || kindDef == null || !faction.def.humanlikeFaction) return;

            // 获取派系统一设置
            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(faction.def.defName);
            
            // 优先应用兵种级别的异种设置
            var kindData = factionData?.GetKindData(kindDef.defName);
            if (kindData != null && kindData.isModified)
            {
                // 兵种级别设置
                ApplyKindXenotypeSettings(faction, kindData);
            }
            else if (factionData != null && factionData.isModified)
            {
                // 派系统一设置
                ApplyFactionXenotypeSettings(faction, factionData);
            }
        }

        private static void ApplyKindXenotypeSettings(Faction faction, KindGearData kindData)
        {
            if (faction.def.xenotypeSet == null) return;

            // 强制异种：最高优先级
            if (!string.IsNullOrEmpty(kindData.ForcedXenotype))
            {
                XenotypeDef forcedXeno = DefDatabase<XenotypeDef>.GetNamedSilentFail(kindData.ForcedXenotype);
                if (forcedXeno != null)
                {
                    var chances = FactionDefManager.GetXenotypeChances(faction.def.xenotypeSet);
                    if (chances != null)
                    {
                        chances.Clear();
                        chances.Add(new XenotypeChance(forcedXeno, 1.0f));
                        LogUtils.DebugLog($"Forced xenotype {forcedXeno.defName} for {faction.def.defName}");
                    }
                }
                return;
            }

            // 禁用异种概率控制：不应用兵种级别设置，让派系统一设置生效
            if (kindData.DisableXenotypeChances)
            {
                LogUtils.DebugLog($"Kind xenotype chances disabled for {faction.def.defName}, skipping kind-level settings");
                return;
            }

            // 自定义异种概率
            if (kindData.XenotypeChances != null && kindData.XenotypeChances.Count > 0)
            {
                var chances = FactionDefManager.GetXenotypeChances(faction.def.xenotypeSet);
                if (chances != null)
                {
                    chances.Clear();
                    foreach (var kvp in kindData.XenotypeChances)
                    {
                        XenotypeDef xDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(kvp.Key);
                        if (xDef != null)
                        {
                            chances.Add(new XenotypeChance(xDef, kvp.Value));
                        }
                    }
                    LogUtils.DebugLog($"Applied kind xenotype chances for {faction.def.defName}");
                }
            }
        }

        private static void ApplyFactionXenotypeSettings(Faction faction, FactionGearData factionData)
        {
            if (faction.def.xenotypeSet == null) return;

            // 禁用异种概率控制：恢复原始异种设置
            if (factionData.DisableXenotypeChances)
            {
                // 尝试从原始数据恢复
                var originalFactionData = FactionDefManager.GetOriginalXenotypeChances(faction.def);
                if (originalFactionData != null && originalFactionData.Count > 0)
                {
                    var chances = FactionDefManager.GetXenotypeChances(faction.def.xenotypeSet);
                    if (chances != null)
                    {
                        chances.Clear();
                        foreach (var originalChance in originalFactionData)
                        {
                            chances.Add(originalChance);
                        }
                        LogUtils.DebugLog($"Restored original xenotype chances for {faction.def.defName}");
                    }
                }
                else
                {
                    // 原始数据也没有异种设置，则清空
                    var chances = FactionDefManager.GetXenotypeChances(faction.def.xenotypeSet);
                    if (chances != null)
                    {
                        chances.Clear();
                        LogUtils.DebugLog($"Cleared xenotype chances for {faction.def.defName} (no original data)");
                    }
                }
                return;
            }

            // 自定义异种概率
            if (factionData.XenotypeChances != null && factionData.XenotypeChances.Count > 0)
            {
                var chances = FactionDefManager.GetXenotypeChances(faction.def.xenotypeSet);
                if (chances != null)
                {
                    chances.Clear();
                    foreach (var kvp in factionData.XenotypeChances)
                    {
                        XenotypeDef xDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(kvp.Key);
                        if (xDef != null)
                        {
                            chances.Add(new XenotypeChance(xDef, kvp.Value));
                        }
                    }
                    LogUtils.DebugLog($"Applied faction xenotype chances for {faction.def.defName}");
                }
            }
        }

        /// <summary>
        /// 应用年龄限制设置到 PawnGenerationRequest
        /// </summary>
        private static void ApplyAgeSettings(PawnGenerationRequest request)
        {
            if (request.KindDef == null || request.Faction == null) return;

            // 获取派系统一设置
            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(request.Faction.def.defName);
            if (factionData?.groupMakers == null || factionData.groupMakers.Count == 0) return;

            string kindDefName = request.KindDef.defName;
            float? minAge = null;
            float? maxAge = null;

            // 在所有群组中查找该兵种的年龄设置
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
                            float effectiveMin = minAge ?? 0f;
                            float effectiveMax = maxAge ?? 999f;

                            // 如果请求中还没有固定年龄，则在范围内随机选择
                            if (!request.FixedBiologicalAge.HasValue)
                            {
                                float targetAge = Rand.Range(effectiveMin, effectiveMax);
                                request.FixedBiologicalAge = targetAge;
                                request.FixedChronologicalAge = targetAge;
                                LogUtils.DebugLog($"Applied age settings for {kindDefName}: {targetAge:F1} (range: {effectiveMin:F1}-{effectiveMax:F1})");
                            }
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
                    float effectiveMin = minAge ?? 0f;
                    float effectiveMax = maxAge ?? 999f;

                    if (!request.FixedBiologicalAge.HasValue)
                    {
                        float targetAge = Rand.Range(effectiveMin, effectiveMax);
                        request.FixedBiologicalAge = targetAge;
                        request.FixedChronologicalAge = targetAge;
                        LogUtils.DebugLog($"Applied age settings for {kindDefName}: {targetAge:F1} (range: {effectiveMin:F1}-{effectiveMax:F1})");
                    }
                    return;
                }
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
    }
}