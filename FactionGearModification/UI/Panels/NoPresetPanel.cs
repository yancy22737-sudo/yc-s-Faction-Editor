using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace FactionGearCustomizer.UI.Panels
{
    public static class NoPresetPanel
    {
        public static void Draw(Rect inRect)
        {
            // 绘制半透明背景遮罩
            Widgets.DrawBoxSolid(inRect, new Color(0.1f, 0.1f, 0.1f, 0.85f));

            float centerX = inRect.x + inRect.width / 2f;
            float centerY = inRect.y + inRect.height / 2f;

            // 标题
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            string title = LanguageManager.Get("NoPresetWarning");
            GUI.color = Color.yellow;
            Widgets.Label(new Rect(centerX - 200f, centerY - 100f, 400f, 40f), title);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // 说明文字
            string desc = LanguageManager.Get("NoPresetDescription");
            float descHeight = Text.CalcHeight(desc, 400f);
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            Widgets.Label(new Rect(centerX - 200f, centerY - 50f, 400f, descHeight), desc);
            GUI.color = Color.white;

            // 按钮区域 - 三个按钮横向排列
            float btnY = centerY + 30f;
            float btnWidth = 150f;
            float btnHeight = 40f;
            float gap = 15f;
            float totalWidth = btnWidth * 3 + gap * 2;
            float startX = centerX - totalWidth / 2f;

            // 创建预设按钮
            Rect createBtnRect = new Rect(startX, btnY, btnWidth, btnHeight);
            GUI.color = Color.green;
            if (Widgets.ButtonText(createBtnRect, LanguageManager.Get("CreatePreset")))
            {
                Find.WindowStack.Add(new PresetManagerWindow());
            }
            GUI.color = Color.white;
            TooltipHandler.TipRegion(createBtnRect, LanguageManager.Get("CreatePresetTooltip"));

            // 加载预设按钮
            Rect loadBtnRect = new Rect(startX + btnWidth + gap, btnY, btnWidth, btnHeight);
            GUI.color = Color.cyan;
            if (Widgets.ButtonText(loadBtnRect, LanguageManager.Get("LoadPreset")))
            {
                Find.WindowStack.Add(new PresetManagerWindow());
            }
            GUI.color = Color.white;
            TooltipHandler.TipRegion(loadBtnRect, LanguageManager.Get("LoadPresetTooltip"));

            // 清除所有配置按钮
            Rect clearBtnRect = new Rect(startX + (btnWidth + gap) * 2, btnY, btnWidth, btnHeight);
            GUI.color = new Color(1f, 0.5f, 0.5f); // 红色
            if (Widgets.ButtonText(clearBtnRect, LanguageManager.Get("ClearAllConfigs")))
            {
                ShowClearAllConfigsDialog();
            }
            GUI.color = Color.white;
            TooltipHandler.TipRegion(clearBtnRect, LanguageManager.Get("ClearAllConfigsTooltip"));

            // 底部提示
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.gray;
            string tip = LanguageManager.Get("PresetEqualsConfigTip");
            Widgets.Label(new Rect(centerX - 250f, btnY + btnHeight + 20f, 500f, 30f), tip);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void ShowClearAllConfigsDialog()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                LanguageManager.Get("ClearAllConfigsConfirmMessage"),
                LanguageManager.Get("ClearAll"), // 使用已有的 "ClearAll" 键
                delegate
                {
                    // 执行清除所有配置
                    FactionGearCustomizerMod.Settings.ResetToDefault();
                    FactionGearEditor.RefreshAllCaches();
                    FactionGearEditor.MarkDirty();
                    Messages.Message(LanguageManager.Get("SettingsSaved"), MessageTypeDefOf.PositiveEvent, false);
                },
                LanguageManager.Get("Cancel"),
                null,
                LanguageManager.Get("ClearAllConfigs"),
                true
            ));
        }

        public static bool HasActivePreset()
        {
            return !string.IsNullOrEmpty(FactionGearCustomizerMod.Settings.currentPresetName);
        }
    }
}
