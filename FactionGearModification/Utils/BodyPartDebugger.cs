using System;
using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Validation;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Utils
{
    /// <summary>
    /// 身体部位调试器 - 仅输出关键配置信息
    /// </summary>
    public static class BodyPartDebugger
    {
        /// <summary>
        /// 输出 pawn 的关键身体部位配置信息
        /// </summary>
        public static void DebugPawnBodyParts(Pawn pawn)
        {
            if (pawn?.RaceProps?.body == null)
            {
                Log.Warning("[BodyPartDebugger] Pawn or body is null");
                return;
            }

            Log.Message($"[BodyPartDebugger] ========== {pawn.Name} 身体部位配置 ==========");
            Log.Message($"[BodyPartDebugger] 身体类型：{pawn.RaceProps.body.defName}，部位总数：{pawn.RaceProps.body.AllParts.Count}");

            // 只输出关键部位的可用性统计
            var keyParts = new[] { "Eye", "Ear", "Arm", "Leg", "Hand", "Foot", "Lung", "Kidney", "Heart", "Brain", "Spine", "Stomach", "Liver" };
            var availableParts = new List<string>();
            var missingParts = new List<string>();

            foreach (var partName in keyParts)
            {
                var partDef = DefDatabase<BodyPartDef>.GetNamedSilentFail(partName);
                if (partDef == null) continue;

                var parts = pawn.RaceProps.body.GetPartsWithDef(partDef).ToList();
                var missingCount = parts.Count(p => pawn.health?.hediffSet?.PartIsMissing(p) == true);
                var availableCount = parts.Count - missingCount;

                if (availableCount > 0)
                {
                    availableParts.Add($"{partName}({availableCount})");
                }
                if (missingCount > 0)
                {
                    missingParts.Add($"{partName}({missingCount})");
                }
            }

            if (availableParts.Any())
            {
                Log.Message($"[BodyPartDebugger] 可用关键部位：{string.Join(", ", availableParts)}");
            }
            if (missingParts.Any())
            {
                Log.Message($"[BodyPartDebugger] 缺失关键部位：{string.Join(", ", missingParts)}");
            }

            Log.Message("[BodyPartDebugger] ===========================================");
        }

        /// <summary>
        /// 测试特定植入物可以安装到哪些部位
        /// </summary>
        public static void DebugImplantPlacement(Pawn pawn, HediffDef hediffDef)
        {
            if (pawn == null || hediffDef == null) return;

            var recommendedParts = HediffPartMappingManager.GetRecommendedPartsForHediff(hediffDef);

            Log.Message($"[BodyPartDebugger] {hediffDef.defName} -> 推荐部位：{string.Join(", ", recommendedParts)}");

            foreach (var partName in recommendedParts)
            {
                var partDef = DefDatabase<BodyPartDef>.GetNamedSilentFail(partName);
                if (partDef == null)
                {
                    Log.Warning($"[BodyPartDebugger] ❌ 找不到 BodyPartDef: {partName}");
                    continue;
                }

                var parts = pawn.RaceProps.body.GetPartsWithDef(partDef).ToList();
                var availableCount = parts.Count(p => !pawn.health?.hediffSet?.PartIsMissing(p) == true);

                if (availableCount == 0)
                {
                    Log.Warning($"[BodyPartDebugger] ❌ {partName} 无可用部位");
                }
                else
                {
                    Log.Message($"[BodyPartDebugger] ✅ {partName} 可用数量：{availableCount}");
                }
            }
        }
    }
}
