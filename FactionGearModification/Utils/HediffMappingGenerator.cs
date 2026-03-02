using System;
using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Validation;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Utils
{
    /// <summary>
    /// Hediff 映射生成器 - 用于自动发现和生成缺失的植入物映射
    /// </summary>
    public static class HediffMappingGenerator
    {
        /// <summary>
        /// 生成所有缺失的植入物映射并输出到日志
        /// </summary>
        public static void GenerateMissingMappings()
        {
            Log.Message("[HediffMappingGenerator] 开始扫描缺失的植入物映射...");

            var existingMappings = HediffPartMappingManager.GetAllMappings();
            var missingMappings = new List<(string HediffName, string BodyPart)>();

            // 获取所有 countsAsAddedPartOrImplant 的 HediffDef
            var allImplants = DefDatabase<HediffDef>.AllDefsListForReading
                .Where(def => def.countsAsAddedPartOrImplant || def.hediffClass == typeof(Hediff_AddedPart))
                .ToList();

            Log.Message($"[HediffMappingGenerator] 发现 {allImplants.Count} 个植入物");

            foreach (var implant in allImplants)
            {
                // 跳过已明确映射的
                if (existingMappings.ContainsKey(implant.defName))
                {
                    continue;
                }

                // 尝试从 RecipeDef 中查找安装位置
                var bodyPart = FindBodyPartFromRecipe(implant);
                
                if (bodyPart != null)
                {
                    missingMappings.Add((implant.defName, bodyPart));
                    Log.Message($"[HediffMappingGenerator] 缺失映射：{implant.defName} -> {bodyPart}");
                }
                else
                {
                    // 尝试从名称推断
                    var inferredParts = HediffPartMappingManager.GetRecommendedPartsForHediff(implant);
                    if (inferredParts.Count > 0)
                    {
                        Log.Message($"[HediffMappingGenerator] 可推断：{implant.defName} -> {string.Join(", ", inferredParts)}");
                    }
                    else
                    {
                        Log.Warning($"[HediffMappingGenerator] 无法确定部位：{implant.defName} (标签：{implant.label})");
                    }
                }
            }

            Log.Message($"[HediffMappingGenerator] 扫描完成。发现 {missingMappings.Count} 个缺失映射");

            // 输出可添加到代码的映射
            if (missingMappings.Count > 0)
            {
                Log.Message("[HediffMappingGenerator] === 建议添加的映射 ===");
                foreach (var (hediffName, bodyPart) in missingMappings.OrderBy(m => m.HediffName))
                {
                    Log.Message($"AddHediffMapping(\"{hediffName}\", new List<string> {{ \"{bodyPart}\" }});");
                }
            }
        }

        /// <summary>
        /// 从 RecipeDef 中查找 Hediff 的安装位置
        /// </summary>
        private static string FindBodyPartFromRecipe(HediffDef hediffDef)
        {
            // 查找添加该 hediff 的所有 RecipeDef
            var recipes = DefDatabase<RecipeDef>.AllDefsListForReading
                .Where(r => r.addsHediff == hediffDef)
                .ToList();

            foreach (var recipe in recipes)
            {
                // 检查 appliedOnFixedBodyParts
                if (recipe.appliedOnFixedBodyParts != null && recipe.appliedOnFixedBodyParts.Count > 0)
                {
                    foreach (var part in recipe.appliedOnFixedBodyParts)
                    {
                        return part.defName;
                    }
                }

                // 检查 appliedOnFixedBodyPartGroups（简化处理，返回组名）
                if (recipe.appliedOnFixedBodyPartGroups != null && recipe.appliedOnFixedBodyPartGroups.Count > 0)
                {
                    foreach (var group in recipe.appliedOnFixedBodyPartGroups)
                    {
                        return group.defName;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 验证现有映射是否正确
        /// </summary>
        public static void ValidateExistingMappings()
        {
            Log.Message("[HediffMappingGenerator] 开始验证现有映射...");

            var allMappings = HediffPartMappingManager.GetAllMappings();
            var incorrectMappings = new List<(string HediffName, string CurrentMapping, string CorrectMapping)>();

            foreach (var kvp in allMappings)
            {
                var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(kvp.Key);
                if (hediffDef == null)
                {
                    Log.Warning($"[HediffMappingGenerator] 未找到 HediffDef: {kvp.Key}");
                    continue;
                }

                var correctPart = FindBodyPartFromRecipe(hediffDef);
                if (correctPart != null && !kvp.Value.Contains(correctPart))
                {
                    incorrectMappings.Add((kvp.Key, string.Join(", ", kvp.Value), correctPart));
                    Log.Warning($"[HediffMappingGenerator] 映射可能不正确：{kvp.Key}");
                    Log.Warning($"  当前：{string.Join(", ", kvp.Value)}");
                    Log.Warning($"  应该：{correctPart}");
                }
            }

            if (incorrectMappings.Count > 0)
            {
                Log.Message($"[HediffMappingGenerator] 发现 {incorrectMappings.Count} 个可能不正确的映射");
            }
            else
            {
                Log.Message("[HediffMappingGenerator] 所有映射验证通过");
            }
        }
    }
}
