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

        // 存档唯一标识符，用于检测存档切换
        public string saveUniqueIdentifier = null;

        public FactionGearGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref activePresetName, "activePresetName", null);
            Scribe_Values.Look(ref hasShownFirstTimePrompt, "hasShownFirstTimePrompt", false);
            Scribe_Values.Look(ref useCustomSettings, "useCustomSettings", false);
            Scribe_Values.Look(ref saveUniqueIdentifier, "saveUniqueIdentifier", null);
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
                
                // 【修复】先重置所有派系Def到原版状态
                foreach (var factionDef in DefDatabase<FactionDef>.AllDefs)
                {
                    FactionDefManager.ResetFaction(factionDef);
                }
                foreach (var kindDef in DefDatabase<PawnKindDef>.AllDefs)
                {
                    FactionDefManager.ResetKind(kindDef);
                }
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

            // 同步存档数据到全局设置，确保UI显示正确
            SyncSaveDataToGlobal();

            // 立即应用设置到游戏
            FactionDefManager.ApplyAllSettings();
        }

        /// <summary>
        /// 同步存档中的派系数据到全局设置，确保UI显示正确
        /// </summary>
        private void SyncSaveDataToGlobal()
        {
            // 清理全局设置中的旧数据
            FactionGearCustomizerMod.Settings.factionGearData.Clear();
            if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
            {
                FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
            }

            // 将存档数据同步到全局设置
            if (savedFactionGearData != null)
            {
                foreach (var factionData in savedFactionGearData)
                {
                    var cloned = factionData.DeepCopy();
                    FactionGearCustomizerMod.Settings.factionGearData.Add(cloned);
                    if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
                    {
                        FactionGearCustomizerMod.Settings.factionGearDataDict[cloned.factionDefName] = cloned;
                    }
                }
            }

            FactionGearCustomizerMod.Settings.currentPresetName = activePresetName;

            Log.Message($"[FactionGearCustomizer] Synced save data to global settings. Faction count: {savedFactionGearData?.Count ?? 0}");
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
        /// 检查是否需要显示存档切换提示（与首次进入相同的行为）
        /// </summary>
        public bool ShouldShowSaveSwitchPrompt()
        {
            // 存档切换时总是显示选择界面（如果当前没有使用自定义设置且有可用预设）
            return !useCustomSettings && FactionGearCustomizerMod.Settings?.presets?.Any() == true;
        }

        /// <summary>
        /// 标记已显示首次提示
        /// </summary>
        public void MarkFirstTimePromptShown()
        {
            hasShownFirstTimePrompt = true;
        }

        /// <summary>
        /// 生成新的存档唯一标识符
        /// </summary>
        public void GenerateNewSaveIdentifier()
        {
            saveUniqueIdentifier = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 检查是否是不同的存档（切换了存档）
        /// </summary>
        public bool IsDifferentSave(string previousSaveIdentifier)
        {
            return !string.IsNullOrEmpty(previousSaveIdentifier) && 
                   previousSaveIdentifier != saveUniqueIdentifier;
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

        /// <summary>
        /// 从预设中选择性应用单个或多个派系到当前存档
        /// </summary>
        public void ApplyFactionsFromPreset(FactionGearPreset preset, List<string> factionDefNames)
        {
            if (preset == null || factionDefNames == null || factionDefNames.Count == 0)
            {
                Log.Warning("[FactionGearCustomizer] ApplyFactionsFromPreset called with null or empty parameters.");
                return;
            }

            // 标记使用自定义设置
            activePresetName = preset.name;
            useCustomSettings = true;

            // 收集需要验证的Mod和DLC
            List<string> missingMods = new List<string>();
            List<string> missingDLC = new List<string>();

            // 只添加选中的派系数据
            foreach (var factionDefName in factionDefNames)
            {
                var factionData = preset.factionGearData?.FirstOrDefault(f => f.factionDefName == factionDefName);
                if (factionData != null)
                {
                    // 深拷贝派系数据
                    var clonedData = factionData.DeepCopy();
                    
                    // 移除无效的兵种数据
                    clonedData.kindGearData.RemoveAll(k => DefDatabase<PawnKindDef>.GetNamedSilentFail(k.kindDefName) == null);

                    // 检查是否已存在该派系的数据，存在则更新
                    var existingIndex = savedFactionGearData.FindIndex(f => f.factionDefName == factionDefName);
                    if (existingIndex >= 0)
                    {
                        savedFactionGearData[existingIndex] = clonedData;
                    }
                    else
                    {
                        savedFactionGearData.Add(clonedData);
                    }
                }
                else
                {
                    Log.Warning($"[FactionGearCustomizer] Faction '{factionDefName}' not found in preset '{preset.name}'");
                }
            }

            Log.Message($"[FactionGearCustomizer] Applied {factionDefNames.Count} factions from preset '{preset.name}' to current save.");

            // 同步存档数据到全局设置
            SyncSaveDataToGlobal();

            // 立即应用设置到游戏
            FactionDefManager.ApplyAllSettings();
        }
    }
}
