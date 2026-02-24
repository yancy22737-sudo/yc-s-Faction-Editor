using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using FactionGearCustomizer.Managers;

namespace FactionGearCustomizer.Core
{
    /// <summary>
    /// 存档级别的派系装备预设管理组件
    /// 负责存储每个存档独立的预设选择状态
    /// </summary>
    public class FactionGearGameComponent : GameComponent
    {
        // 当前存档激活的预设名称（null 表示使用原版设置）
        public string activePresetName = null;

        // 是否已显示过首次进入提示
        public bool hasShownFirstTimePrompt = false;

        // 当前存档应用的派系装备数据（深拷贝自预设）
        public List<FactionGearData> savedFactionGearData = new List<FactionGearData>();

        // 是否使用自定义设置（而非原版）
        public bool useCustomSettings = false;

        public FactionGearGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref activePresetName, "activePresetName", null);
            Scribe_Values.Look(ref hasShownFirstTimePrompt, "hasShownFirstTimePrompt", false);
            Scribe_Values.Look(ref useCustomSettings, "useCustomSettings", false);
            Scribe_Collections.Look(ref savedFactionGearData, "savedFactionGearData", LookMode.Deep);

            if (savedFactionGearData == null)
                savedFactionGearData = new List<FactionGearData>();
        }

        /// <summary>
        /// 获取当前存档应该使用的派系装备数据
        /// </summary>
        public List<FactionGearData> GetActiveFactionGearData()
        {
            if (!useCustomSettings || savedFactionGearData == null || savedFactionGearData.Count == 0)
                return null;
            return savedFactionGearData;
        }

        /// <summary>
        /// 应用预设到当前存档
        /// </summary>
        public void ApplyPresetToSave(FactionGearPreset preset)
        {
            if (preset == null)
            {
                // 恢复原版设置
                activePresetName = null;
                useCustomSettings = false;
                savedFactionGearData.Clear();
                Log.Message("[FactionGearCustomizer] Restored vanilla settings for this save.");
            }
            else
            {
                // 深拷贝预设数据到存档
                activePresetName = preset.name;
                useCustomSettings = true;
                savedFactionGearData.Clear();

                if (preset.factionGearData != null)
                {
                    foreach (var factionData in preset.factionGearData)
                    {
                        savedFactionGearData.Add(factionData.DeepCopy());
                    }
                }

                Log.Message($"[FactionGearCustomizer] Applied preset '{preset.name}' to current save.");
            }

            // 立即应用设置到游戏
            FactionDefManager.ApplyAllSettings();
        }

        /// <summary>
        /// 获取当前激活的预设（从全局预设列表中查找）
        /// </summary>
        public FactionGearPreset GetActivePreset()
        {
            if (string.IsNullOrEmpty(activePresetName))
                return null;

            return FactionGearCustomizerMod.Settings?.presets
                ?.FirstOrDefault(p => p.name == activePresetName);
        }

        /// <summary>
        /// 检查是否需要显示首次进入提示
        /// </summary>
        public bool ShouldShowFirstTimePrompt()
        {
            // 只在以下情况显示：
            // 1. 从未显示过提示
            // 2. 当前没有使用自定义设置
            // 3. 全局有可用预设
            return !hasShownFirstTimePrompt
                && !useCustomSettings
                && FactionGearCustomizerMod.Settings?.presets?.Any() == true;
        }

        /// <summary>
        /// 标记已显示首次提示
        /// </summary>
        public void MarkFirstTimePromptShown()
        {
            hasShownFirstTimePrompt = true;
        }

        // 静态辅助方法：获取当前游戏组件
        public static FactionGearGameComponent Instance
        {
            get
            {
                if (Current.Game == null)
                    return null;
                return Current.Game.GetComponent<FactionGearGameComponent>();
            }
        }
    }
}
