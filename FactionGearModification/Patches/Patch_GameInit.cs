using FactionGearCustomizer.Core;
using FactionGearCustomizer.UI.Dialogs;
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
                    // 同步存档设置到全局设置
                    SyncSaveSettingsToGlobal();
                    CheckAndShowFirstTimePrompt();
                }, "CheckingFactionGearSettings", false, null);
            }
        }

        /// <summary>
        /// 同步存档设置到全局设置，确保在加载存档后UI显示正确数据
        /// </summary>
        private static void SyncSaveSettingsToGlobal()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null) return;

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

            // 如果存档没有绑定预设（useCustomSettings == false），则重置全局设置
            // 这样可以避免上一个存档的设置残留到新存档
            if (!gameComponent.useCustomSettings)
            {
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

                // 刷新缓存
                FactionGearEditor.RefreshAllCaches();

                Log.Message("[FactionGearCustomizer] Global settings reset for new save.");
            }
        }

        private static void CheckAndShowFirstTimePrompt()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null) return;

            if (gameComponent.ShouldShowFirstTimePrompt())
            {
                // 延迟一帧显示对话框，确保UI准备就绪
                Find.WindowStack.Add(new Dialog_FirstTimePresetPrompt());
            }
        }
    }
}
