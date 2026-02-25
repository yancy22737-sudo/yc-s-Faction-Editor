using FactionGearCustomizer.Core;
using FactionGearCustomizer.UI.Dialogs;
using FactionGearCustomizer.Managers;
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
                Log.Message($"[FactionGearCustomizer] Generated save identifier: {gameComponent.saveUniqueIdentifier}");
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
                Log.Message($"[FactionGearCustomizer] Generated save identifier during sync: {gameComponent.saveUniqueIdentifier}");
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
                    FactionGearCustomizerMod.Settings.factionGearData.Add(cloned);
                    if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
                    {
                        FactionGearCustomizerMod.Settings.factionGearDataDict[cloned.factionDefName] = cloned;
                    }
                }

                FactionGearCustomizerMod.Settings.currentPresetName = gameComponent.activePresetName;

                // 刷新缓存
                FactionGearEditor.RefreshAllCaches();

                Log.Message($"[FactionGearCustomizer] Synced save settings to global. Preset: {gameComponent.activePresetName}");
            }
            else
            {
                // 存档没有自定义设置，保持全局设置不变
                Log.Message("[FactionGearCustomizer] Save has no custom settings, keeping global settings.");
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
                Log.Message($"[FactionGearCustomizer] Generated new save identifier: {gameComponent.saveUniqueIdentifier}");
            }

            // 如果存档没有绑定预设（useCustomSettings == false），则重置全局设置
            // 这样可以避免上一个存档的设置残留到新存档
            if (!gameComponent.useCustomSettings)
            {
                // 清理原始数据缓存，确保使用真正的原版数据
                FactionDefManager.ClearOriginalDataCache();

                // 重置全局设置
                FactionGearCustomizerMod.Settings.factionGearData.Clear();
                if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
                {
                    FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
                }
                FactionGearCustomizerMod.Settings.currentPresetName = null;

                // 重置会话状态
                EditorSession.ResetSession();
                UndoManager.Clear();

                // 重新保存原始数据（确保是干净的）
                FactionDefManager.SaveAllOriginalData();

                // 刷新缓存
                FactionGearEditor.RefreshAllCaches();

                Log.Message("[FactionGearCustomizer] Global settings reset for new save.");
            }
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
                    Log.Message("[FactionGearCustomizer] Save switch detected. Previous: " + 
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
            // 清理原始数据缓存，确保使用真正的原版数据
            FactionDefManager.ClearOriginalDataCache();

            // 清除全局设置中的数据
            FactionGearCustomizerMod.Settings.factionGearData.Clear();
            if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
            {
                FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
            }
            FactionGearCustomizerMod.Settings.currentPresetName = null;

            // 重置编辑器会话
            EditorSession.ResetSession();
            UndoManager.Clear();

            // 恢复所有被修改的 FactionDef 到原始状态
            RestoreAllFactionDefsToOriginal();

            // 重新保存原始数据（确保是干净的）
            FactionDefManager.SaveAllOriginalData();

            // 刷新缓存
            FactionGearEditor.RefreshAllCaches();

            Log.Message("[FactionGearCustomizer] Current modifications reset due to save switch.");
        }

        /// <summary>
        /// 恢复所有 FactionDef 到原始状态
        /// </summary>
        private static void RestoreAllFactionDefsToOriginal()
        {
            // 从全局设置中获取所有已修改的 factionDefName
            // 由于全局设置已被清除，我们需要使用存档中的原始数据来恢复
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null) return;

            // 如果存档有保存的 FactionGearData，我们不需要恢复 - 那是新存档应该有的数据
            // 如果存档没有自定义设置，那我们需要恢复所有被修改的 FactionDef
            if (gameComponent.useCustomSettings && gameComponent.savedFactionGearData != null && gameComponent.savedFactionGearData.Count > 0)
            {
                // 存档有自定义设置，不需要恢复
                Log.Message("[FactionGearCustomizer] Save has custom settings, skipping FactionDef restoration.");
                return;
            }

            // 存档没有自定义设置，遍历所有 Def 并尝试恢复原始值
            // 由于 originalFactionData 缓存在内存中，我们可以直接使用 FactionDefManager 的方法来恢复
            try
            {
                foreach (var factionDef in DefDatabase<FactionDef>.AllDefs)
                {
                    if (factionDef != null)
                    {
                        FactionDefManager.ResetFaction(factionDef);
                    }
                }
                Log.Message("[FactionGearCustomizer] All FactionDefs restored to original state.");
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
    }
}
