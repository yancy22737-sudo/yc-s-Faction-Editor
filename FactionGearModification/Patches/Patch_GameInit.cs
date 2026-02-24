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
                    CheckAndShowFirstTimePrompt();
                }, "CheckingFactionGearSettings", false, null);
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
