using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Utils
{
    /// <summary>
    /// 记录所有找不到部位的植入物，便于手动添加映射
    /// </summary>
    public static class HediffMappingLogger
    {
        private static HashSet<string> _loggedHediffs = new HashSet<string>();
        private static bool _enabled = true;

        /// <summary>
        /// 记录找不到部位的植入物（每个植入物只记录一次）
        /// </summary>
        public static void LogMissingPart(string hediffName, List<string> recommendedParts, string pawnKindName)
        {
            if (!_enabled) return;
            if (_loggedHediffs.Contains(hediffName)) return;

            _loggedHediffs.Add(hediffName);
            
            string parts = recommendedParts != null && recommendedParts.Count > 0 
                ? string.Join(", ", recommendedParts) 
                : "无推荐部位";
            
            Log.Warning($"[HediffMappingLogger] 需要手动指定部位的植入物: {hediffName}");
            Log.Warning($"  推荐部位: {parts}");
            Log.Warning($"  出现在: {pawnKindName}");
            Log.Warning($"  建议添加到 HediffPartMappingManager.cs:");
            Log.Warning($"  AddHediffMapping(\"{hediffName}\", new List<string> {{ \"{parts.Split(',')[0].Trim()}\" }});");
        }

        /// <summary>
        /// 输出所有记录到的植入物汇总
        /// </summary>
        public static void PrintSummary()
        {
            if (_loggedHediffs.Count == 0) return;

            Log.Message("[HediffMappingLogger] ========== 需要手动指定部位的植入物汇总 ==========");
            foreach (var hediff in _loggedHediffs.OrderBy(x => x))
            {
                Log.Message($"  - {hediff}");
            }
            Log.Message($"[HediffMappingLogger] 共 {_loggedHediffs.Count} 个植入物需要手动指定");
            Log.Message("[HediffMappingLogger] 请将这些植入物添加到 HediffPartMappingManager.cs 的 InitializeDefaultHediffMappings 方法中");
        }

        /// <summary>
        /// 清空记录（用于重新开始记录）
        /// </summary>
        public static void Clear()
        {
            _loggedHediffs.Clear();
        }

        /// <summary>
        /// 启用/禁用记录
        /// </summary>
        public static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// 获取所有记录到的植入物
        /// </summary>
        public static IEnumerable<string> GetLoggedHediffs()
        {
            return _loggedHediffs.OrderBy(x => x);
        }
    }
}
