using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    /// <summary>
    /// 首次进入存档时的预设选择提示对话框
    /// </summary>
    public class Dialog_FirstTimePresetPrompt : Window
    {
        private List<FactionGearPreset> availablePresets;
        private FactionGearPreset selectedPreset;
        private Vector2 scrollPosition;
        private bool dontAskAgain;

        public override Vector2 InitialSize => new Vector2(600f, 500f);

        public Dialog_FirstTimePresetPrompt()
        {
            availablePresets = FactionGearCustomizerMod.Settings.presets?.ToList() ?? new List<FactionGearPreset>();
            doCloseButton = false;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 标题
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
            Widgets.Label(titleRect, LanguageManager.Get("FGC_FirstTimePrompt_Title"));
            Text.Font = GameFont.Small;

            // 说明文字
            Rect descRect = new Rect(inRect.x, titleRect.yMax + 10f, inRect.width, 60f);
            GUI.color = Color.gray;
            Widgets.Label(descRect, LanguageManager.Get("FGC_FirstTimePrompt_Description"));
            GUI.color = Color.white;

            // 预设列表
            float listY = descRect.yMax + 15f;
            Rect listRect = new Rect(inRect.x, listY, inRect.width, inRect.height - listY - 80f);

            DrawPresetList(listRect);

            // 底部按钮区域
            float buttonY = inRect.yMax - 35f;

            // 不再询问复选框
            Rect checkboxRect = new Rect(inRect.x, buttonY - 25f, 200f, 24f);
            Widgets.CheckboxLabeled(checkboxRect, LanguageManager.Get("FGC_FirstTimePrompt_DontAskAgain"), ref dontAskAgain);

            // 按钮行
            Rect buttonRowRect = new Rect(inRect.x, buttonY, inRect.width, 35f);
            DrawButtons(buttonRowRect);
        }

        private void DrawPresetList(Rect rect)
        {
            Widgets.DrawBox(rect);

            Rect innerRect = rect.ContractedBy(10f);
            float itemHeight = 70f;
            float spacing = 5f;
            float totalHeight = availablePresets.Count * (itemHeight + spacing);

            Rect viewRect = new Rect(innerRect.x, innerRect.y, innerRect.width - 20f, totalHeight);

            Widgets.BeginScrollView(innerRect, ref scrollPosition, viewRect);

            float y = viewRect.y;
            foreach (var preset in availablePresets)
            {
                Rect itemRect = new Rect(viewRect.x, y, viewRect.width, itemHeight);
                DrawPresetItem(itemRect, preset);
                y += itemHeight + spacing;
            }

            // 原版设置选项
            Rect vanillaRect = new Rect(viewRect.x, y, viewRect.width, itemHeight);
            DrawVanillaOption(vanillaRect);

            Widgets.EndScrollView();
        }

        private void DrawPresetItem(Rect rect, FactionGearPreset preset)
        {
            bool isSelected = selectedPreset == preset;

            // 背景
            if (isSelected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            // 点击选择
            if (Widgets.ButtonInvisible(rect))
            {
                selectedPreset = preset;
            }

            Rect contentRect = rect.ContractedBy(8f);

            // 预设名称
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect nameRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 24f);
            Widgets.Label(nameRect, preset.name);

            // 描述
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Rect descRect = new Rect(contentRect.x, nameRect.yMax, contentRect.width, contentRect.height - 24f);
            string desc = string.IsNullOrEmpty(preset.description)
                ? LanguageManager.Get("FGC_Preset_NoDescription")
                : preset.description;
            Widgets.Label(descRect, desc);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // 派系数量标签
            int factionCount = preset.factionGearData?.Count ?? 0;
            if (factionCount > 0)
            {
                Text.Font = GameFont.Tiny;
                string countLabel = $"{factionCount} {LanguageManager.Get("FGC_Factions_Label")}";
                Vector2 labelSize = Text.CalcSize(countLabel);
                Rect countRect = new Rect(
                    contentRect.xMax - labelSize.x - 5f,
                    contentRect.y + 5f,
                    labelSize.x + 10f,
                    18f);
                GUI.color = new Color(0.3f, 0.6f, 0.9f);
                Widgets.DrawBoxSolid(countRect, new Color(0.15f, 0.3f, 0.45f));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(countRect, countLabel);
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawVanillaOption(Rect rect)
        {
            bool isSelected = selectedPreset == null;

            if (isSelected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                selectedPreset = null;
            }

            Rect contentRect = rect.ContractedBy(8f);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Rect nameRect = new Rect(contentRect.x, contentRect.y, contentRect.width, contentRect.height);
            Widgets.Label(nameRect, LanguageManager.Get("FGC_FirstTimePrompt_VanillaOption"));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawButtons(Rect rect)
        {
            float buttonWidth = 120f;

            // 使用原版设置按钮
            Rect vanillaButtonRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(vanillaButtonRect, LanguageManager.Get("FGC_FirstTimePrompt_UseVanilla")))
            {
                ApplyAndClose(null);
            }

            // 应用预设按钮
            Rect applyButtonRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
            GUI.color = selectedPreset != null ? new Color(0.3f, 0.7f, 0.4f) : Color.gray;
            if (Widgets.ButtonText(applyButtonRect, LanguageManager.Get("FGC_FirstTimePrompt_ApplyPreset")))
            {
                if (selectedPreset != null)
                {
                    ApplyAndClose(selectedPreset);
                }
            }
            GUI.color = Color.white;

            // 稍后决定按钮
            float laterButtonX = rect.x + (rect.width - buttonWidth) / 2f;
            Rect laterButtonRect = new Rect(laterButtonX, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(laterButtonRect, LanguageManager.Get("FGC_FirstTimePrompt_DecideLater")))
            {
                MarkPromptShownAndClose();
            }
        }

        private void ApplyAndClose(FactionGearPreset preset)
        {
            // 应用预设到当前存档
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent != null)
            {
                gameComponent.ApplyPresetToSave(preset);
                gameComponent.MarkFirstTimePromptShown();

                if (dontAskAgain)
                {
                    // 保存"不再询问"偏好到全局设置
                    // 这里可以添加一个全局设置项
                }
            }

            Close();

            // 显示确认消息
            string message = preset != null
                ? string.Format(LanguageManager.Get("FGC_Message_PresetApplied"), preset.name)
                : LanguageManager.Get("FGC_Message_UsingVanilla");
            Messages.Message(message, MessageTypeDefOf.PositiveEvent);
        }

        private void MarkPromptShownAndClose()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent != null)
            {
                gameComponent.MarkFirstTimePromptShown();
            }
            Close();
        }
    }
}
