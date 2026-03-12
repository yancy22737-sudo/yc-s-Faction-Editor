using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Core;
using FactionGearCustomizer.UI.Dialogs;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.Utils;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Patches
{
    /// <summary>
    /// 游戏初始化相关Patch
    /// </summary>
    public static class Patch_GameInit
    {
        // 是否需要显示存档切换提示
        private static bool shouldShowSaveSwitchPrompt = false;
        /// <summary>
        /// 在地图生成完成后检查是否需要显示首次提示
        /// </summary>
        [HarmonyPatch(typeof(GameInitData), "PrepForMapGen")]
        public static class Patch_PrepForMapGen
        {
            public static void Postfix()
            {
                // 延迟一帧显示提示，确保游戏完全加载
                LongEventHandler.QueueLongEvent(() =>
                {
                    CheckAndShowFirstTimePrompt();
                }, "CheckingFactionGearSettings", false, null);
            }
        }

        /// <summary>
        /// 在加载存档后检查是否需要显示首次提示
        /// </summary>
        [HarmonyPatch(typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow")]
        public static class Patch_LoadGameFromSaveFileNow
        {
            public static void Postfix()
            {
                // 延迟显示提示
                LongEventHandler.QueueLongEvent(() =>
                {
                    // 确保存档有唯一标识符（在检测存档切换之前）
                    EnsureSaveIdentifierExists();
                    
                    // 检查是否切换了存档
                    HandleSaveSwitch();
                    
                    // 同步存档设置到全局设置
                    SyncSaveSettingsToGlobal();
                    CheckAndShowFirstTimePrompt();
                }, "CheckingFactionGearSettings", false, null);
            }
        }

        /// <summary>
        /// 确保当前存档有唯一标识符
        /// </summary>
        private static void EnsureSaveIdentifierExists()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null) return;

            // 如果存档没有唯一标识符，生成一个新的
            if (string.IsNullOrEmpty(gameComponent.saveUniqueIdentifier))
            {
                gameComponent.GenerateNewSaveIdentifier();
                LogUtils.DebugLog($"Generated save identifier: {gameComponent.saveUniqueIdentifier}");
            }
        }

        /// <summary>
        /// 同步存档设置到全局设置，确保在加载存档后UI显示正确数据
        /// </summary>
        private static void SyncSaveSettingsToGlobal()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null) return;

            // 确保存档有唯一标识符
            if (string.IsNullOrEmpty(gameComponent.saveUniqueIdentifier))
            {
                gameComponent.GenerateNewSaveIdentifier();
                LogUtils.DebugLog($"Generated save identifier during sync: {gameComponent.saveUniqueIdentifier}");
            }

            // 如果存档有绑定预设，同步预设数据
            if (gameComponent.useCustomSettings && gameComponent.savedFactionGearData != null)
            {
                // 清除全局设置
                FactionGearCustomizerMod.Settings.factionGearData.Clear();
                if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
                {
                    FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
                }

                // 深拷贝存档数据到全局设置
                foreach (var factionData in gameComponent.savedFactionGearData)
                {
                    var cloned = factionData.DeepCopy();
                    // 【修复】确保深拷贝后引用被正确解析
                    cloned.ResolveReferences();
                    FactionGearCustomizerMod.Settings.factionGearData.Add(cloned);
                    if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
                    {
                        FactionGearCustomizerMod.Settings.factionGearDataDict[cloned.factionDefName] = cloned;
                    }
                }

                FactionGearCustomizerMod.Settings.currentPresetName = gameComponent.activePresetName;

                // 刷新缓存
                FactionGearEditor.RefreshAllCaches();

                LogUtils.DebugLog($"Synced save settings to global. Preset: {gameComponent.activePresetName}");
            }
            else
            {
                // 存档没有自定义设置，保持全局设置不变
                LogUtils.DebugLog("Save has no custom settings, keeping global settings.");
            }
        }

        /// <summary>
        /// 在新游戏初始化时重置全局设置，避免存档间数据污染
        /// </summary>
        [HarmonyPatch(typeof(Game), "InitNewGame")]
        public static class Patch_InitNewGame
        {
            public static void Postfix()
            {
                LongEventHandler.QueueLongEvent(() =>
                {
                    ResetGlobalSettingsForNewSave();
                }, "ResettingFactionGearSettings", false, null);
            }
        }

        private static void ResetGlobalSettingsForNewSave()
        {
            // 检查是否是游戏组件已存在（即这是新存档而非首次启动）
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null) return;

            // 为新存档生成唯一标识符
            if (string.IsNullOrEmpty(gameComponent.saveUniqueIdentifier))
            {
                gameComponent.GenerateNewSaveIdentifier();
                LogUtils.DebugLog($"Generated new save identifier: {gameComponent.saveUniqueIdentifier}");
            }

            // 【修复】无论新存档是否使用自定义设置，都要清理全局设置！
            // 从任意存档退出来再新建存档时，全局设置里肯定有上一个存档的残留
            
            // 【关键修复】首先恢复所有FactionDef到原始状态！
            FactionDefManager.ResetAllFactions();
            
            // 【重要】重置游戏组件中的派系数据，防止上次界面修改的残留数据影响新游戏
            gameComponent.ResetFactionData();

            // 重置全局设置
            FactionGearCustomizerMod.Settings.factionGearData.Clear();
            if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
            {
                FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
            }
            FactionGearCustomizerMod.Settings.currentPresetName = null;

            // 【关键修复】持久化清理操作
            FactionGearCustomizerMod.Settings.Write();

            // 重置会话状态
            EditorSession.ResetSession();
            UndoManager.Clear();

            // 【关键修复】不要清理原始数据缓存！
            // FactionDefManager.ClearOriginalDataCache(); // 这行删除！

            // 【关键修复】也不要重新保存原始数据！
            // FactionDefManager.SaveAllOriginalData(); // 这行删除！

            // 刷新缓存
            FactionGearEditor.RefreshAllCaches();

            LogUtils.Info("Global settings reset for new save.");
            // [Phase 2] 应用剧本配置
            ApplyScenarioConfigFromSettings();
        }

        /// <summary>
        /// 处理存档切换逻辑
        /// 当检测到切换存档时，重置当前修改数据并显示选择界面
        /// </summary>
        private static void HandleSaveSwitch()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null) return;

            // 检查是否切换了存档
            if (!string.IsNullOrEmpty(FactionGearCustomizerMod.Settings.previousSaveIdentifier))
            {
                if (gameComponent.IsDifferentSave(FactionGearCustomizerMod.Settings.previousSaveIdentifier))
                {
                    LogUtils.Info("Save switch detected. Previous: " + 
                        FactionGearCustomizerMod.Settings.previousSaveIdentifier + ", Current: " + gameComponent.saveUniqueIdentifier);
                    
                    // 重置当前修改数据，使其不生效
                    ResetCurrentModifications();
                    
                    // 标记需要显示选择界面（与首次进入存档相同的行为）
                    // 这里不直接调用hasShownFirstTimePrompt = false，而是使用专门的存档切换提示逻辑
                    shouldShowSaveSwitchPrompt = true;
                }
            }
            
            // 更新上一个存档标识符
            FactionGearCustomizerMod.Settings.previousSaveIdentifier = gameComponent.saveUniqueIdentifier;
        }

        /// <summary>
        /// 重置当前修改数据，使其不生效
        /// </summary>
        private static void ResetCurrentModifications()
        {
            // 【关键修复】首先恢复所有被修改的 FactionDef 到原始状态
            // 必须在清理任何缓存之前执行，因为恢复需要原始数据
            FactionDefManager.ResetAllFactions();

            // 清除全局设置中的数据
            FactionGearCustomizerMod.Settings.factionGearData.Clear();
            if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
            {
                FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
            }
            FactionGearCustomizerMod.Settings.currentPresetName = null;

            // 【关键修复】持久化清理操作
            FactionGearCustomizerMod.Settings.Write();

            // 重置编辑器会话
            EditorSession.ResetSession();
            UndoManager.Clear();

            // 【关键修复】不要清理原始数据缓存！
            // 如果清理了原始数据缓存，后续恢复操作就没有数据可用了
            // FactionDefManager.ClearOriginalDataCache(); // 这行删除！

            // 【关键修复】也不要重新保存原始数据！
            // 原始数据只应该在游戏启动时保存一次
            // FactionDefManager.SaveAllOriginalData(); // 这行删除！

            // 刷新UI缓存（不包括原始数据缓存）
            FactionGearEditor.RefreshAllCaches();

            LogUtils.Info("Current modifications reset due to save switch.");
        }

        /// <summary>
        /// 恢复所有 FactionDef 到原始状态
        /// </summary>
        private static void RestoreAllFactionDefsToOriginal()
        {
            // 【修复】无论新存档是否有自定义设置，都先恢复所有 FactionDef 到原版状态！
            // 这确保了即使新存档有自定义设置，也是在干净的原版基础上应用，而不是在上一个存档修改过的基础上应用
            
            try
            {
                foreach (var factionDef in DefDatabase<FactionDef>.AllDefs)
                {
                    if (factionDef != null)
                    {
                        FactionDefManager.ResetFaction(factionDef);
                    }
                }
                foreach (var kindDef in DefDatabase<PawnKindDef>.AllDefs)
                {
                    if (kindDef != null)
                    {
                        FactionDefManager.ResetKind(kindDef);
                    }
                }
                LogUtils.Info("All FactionDefs and PawnKindDefs restored to original state.");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error restoring FactionDefs: {ex.Message}");
            }
        }

        private static void CheckAndShowFirstTimePrompt()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null) return;

            // 优先处理存档切换提示
            if (shouldShowSaveSwitchPrompt && gameComponent.ShouldShowSaveSwitchPrompt())
            {
                shouldShowSaveSwitchPrompt = false; // 重置标志
                Find.WindowStack.Add(new Dialog_FirstTimePresetPrompt());
                return;
            }

            // 处理首次进入提示
            if (gameComponent.ShouldShowFirstTimePrompt())
            {
                // 延迟一帧显示对话框，确保UI准备就绪
                Find.WindowStack.Add(new Dialog_FirstTimePresetPrompt());
            }
        }

        /// <summary>
        /// 在返回主菜单时清理所有存档相关数据
        /// 这是修复"从存档返回主菜单后数据残留"问题的关键Patch
        /// 通过检测主菜单按钮点击事件来触发清理
        /// </summary>
        [HarmonyPatch(typeof(MainMenuDrawer), "DoMainMenuControls")]
        public static class Patch_MainMenuDrawer_DoMainMenuControls
        {
            private static bool wasInGame = false;
            
            public static void Prefix()
            {
                // 检测是否从游戏状态返回到主菜单
                // 当Current.Game为null且之前在游戏状态时，说明已返回主菜单
                bool currentlyInGame = Current.Game != null && Current.ProgramState == ProgramState.Playing;
                
                if (wasInGame && !currentlyInGame)
                {
                    LogUtils.Info("Detected return to main menu, cleaning up save data...");
                    CleanupOnReturnToMainMenu();
                }
                
                wasInGame = currentlyInGame;
            }
        }
        
        /// <summary>
        /// 备选方案：在存档加载前清理数据
        /// </summary>
        [HarmonyPatch(typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow")]
        public static class Patch_SavedGameLoaderNow_LoadGameFromSaveFileNow_Cleanup
        {
            public static void Prefix()
            {
                // 如果当前有游戏在运行，说明是切换存档，先清理旧数据
                if (Current.Game != null && Current.ProgramState == ProgramState.Playing)
                {
                    LogUtils.Info("Switching saves, cleaning up previous save data...");
                    CleanupOnReturnToMainMenu();
                }
            }
        }
        
        /// <summary>
        /// 返回主菜单时清理所有存档相关数据
        /// </summary>
        private static void CleanupOnReturnToMainMenu()
        {
            try
            {
                LogUtils.Info("Starting comprehensive cleanup on return to main menu...");
                
                // 【关键修复】首先恢复所有FactionDef到原始状态！
                // 这是解决"派系名称、颜色等数据残留"问题的核心
                FactionDefManager.ResetAllFactions();
                
                // 【修复】标记需要在返回主菜单后清理数据
                // 实际的清理会在DoSettingsWindowContents中进行，确保在正确的时机执行
                FactionGearCustomizerMod.MarkNeedsCleanupAfterReturnToMenu();
                
                // 同时立即执行清理（以防设置窗口当前已打开）
                // 1. 清理全局设置中的存档数据
                if (FactionGearCustomizerMod.Settings.factionGearData != null)
                {
                    FactionGearCustomizerMod.Settings.factionGearData.Clear();
                }
                
                if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
                {
                    FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
                }
                
                // 重置当前预设名称
                FactionGearCustomizerMod.Settings.currentPresetName = null;
                
                // 【关键修复】调用Write()持久化清理操作，防止RimWorld从配置文件重新加载残留数据
                FactionGearCustomizerMod.Settings.Write();
                
                // 【关键修复】不要清理原始数据缓存！
                // 如果清理了原始数据缓存，后续恢复操作就没有数据可用了
                // FactionDefManager.ClearOriginalDataCache(); // 这行删除！
                
                // 3. 重置编辑器会话状态
                EditorSession.ResetSession();
                
                // 4. 清理撤销管理器
                UndoManager.Clear();
                
                // 5. 刷新UI缓存
                FactionGearEditor.RefreshAllCaches();
                
                // 6. 重置存档切换提示标志
                shouldShowSaveSwitchPrompt = false;
                
                LogUtils.Info("Save data cleanup completed and persisted.");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error during cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// 从全局设置中应用剧本配置（Phase 2）
        /// </summary>
        private static void ApplyScenarioConfigFromSettings()
        {
            var settings = FactionGearCustomizerMod.Settings;
            if (settings?.scenarioFactionConfig == null)
                return;

            var config = settings.scenarioFactionConfig;
            if (string.IsNullOrEmpty(config.presetName))
                return;

            var preset = settings.presets?.FirstOrDefault(p => p.name == config.presetName);
            if (preset == null)
                return;

            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null)
                return;

            if (config.selectedFactions != null && config.selectedFactions.Any())
            {
                var presetFactionData = preset.factionGearData ?? new List<FactionGearData>();
                var selectedFactionNames = config.selectedFactions
                    .Where(name => presetFactionData.Any(f => f.factionDefName == name))
                    .Distinct()
                    .ToList();

                if (selectedFactionNames.Any())
                {
                    gameComponent.ResetFactionData();
                    gameComponent.ApplyFactionsFromPreset(preset, selectedFactionNames);
                    LogUtils.DebugLog($"Applied scenario config: {preset.name}, factions: {selectedFactionNames.Count}");
                }
            }

            // 清除剧本配置
            settings.scenarioFactionConfig = new ScenarioFactionConfig();
            settings.Write();
        }
    }
}
