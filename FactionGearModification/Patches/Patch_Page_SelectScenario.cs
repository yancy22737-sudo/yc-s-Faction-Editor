
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.UI.Dialogs;

namespace FactionGearCustomizer.Patches
{
    /// <summary>
    /// 在创建世界界面添加派系编辑器按钮
    /// </summary>
    [HarmonyPatch(typeof(Page_CreateWorldParams), "DoWindowContents")]
    public static class Patch_Page_CreateWorldParams
    {
        public static void Postfix(Page_CreateWorldParams __instance, Rect rect)
        {
            // 在"派系"标题右侧添加按钮
            // Page_CreateWorldParams 的派系列表在右侧，宽度约为 45%
            float buttonWidth = 100f;
            float buttonHeight = 24f;
            float buttonX = rect.width - 320f; // 右侧区域
            float buttonY = 12f; // 顶部对齐

            Rect factionEditorButtonRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);
            
            if (Widgets.ButtonText(factionEditorButtonRect, LanguageManager.Get("P2_FactionEditorButton")))
            {
                Find.WindowStack.Add(new Dialog_FactionEditorLite());
            }
        }
    }
}
