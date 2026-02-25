using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    /// <summary>
    /// 首次进入存档时的预设选择提示对话框 - 支持选择单个或多个派系
    /// </summary>
    public class Dialog_FirstTimePresetPrompt : Window
    {
        private List<FactionGearPreset> availablePresets;
        private FactionGearPreset selectedPreset;
        private Vector2 scrollPosition;
        private bool dontAskAgain;
        private HashSet<string> selectedFactions = new HashSet<string>();
        private bool isExpanded = false;

        public override Vector2 InitialSize => new Vector2(650f, 550f);

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
            Rect descRect = new Rect(inRect.x, titleRect.yMax + 10f, inRect.width, 50f);
            GUI.color = Color.gray;
            Widgets.Label(descRect, LanguageManager.Get("FGC_FirstTimePrompt_Description"));
            GUI.color = Color.white;

            // 预设列表 - 上半部分
            float listY = descRect.yMax + 10f;
            float listHeight = isExpanded ? 180f : 280f;
            Rect listRect = new Rect(inRect.x, listY, inRect.width, listHeight);

            DrawPresetList(listRect);

            // 派系选择区域（展开时显示）
            if (isExpanded && selectedPreset != null)
            {
                float factionY = listY + listHeight + 10f;
                float factionHeight = inRect.yMax - factionY - 90f;
                Rect factionRect = new Rect(inRect.x, factionY, inRect.width, factionHeight);
                DrawFactionSelection(factionRect);
            }

            // 底部按钮区域
            float buttonY = inRect.yMax - 35f;

            // 不再询问复选框
            Rect checkboxRect = new Rect(inRect.x, buttonY - 25f, 250f, 24f);
            Widgets.CheckboxLabeled(checkboxRect, LanguageManager.Get("FGC_FirstTimePrompt_DontAskAgain"), ref dontAskAgain);

            // 按钮行
            Rect buttonRowRect = new Rect(inRect.x, buttonY, inRect.width, 35f);
            DrawButtons(buttonRowRect);
        }

        private void DrawPresetList(Rect rect)
        {
            Widgets.DrawBox(rect);

            Rect innerRect = rect.ContractedBy(10f);
            float itemHeight = 60f;
            float spacing = 5f;
            float totalHeight = (availablePresets.Count + 1) * (itemHeight + spacing);

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
                selectedFactions.Clear();
                if (preset.factionGearData != null)
                {
                    foreach (var f in preset.factionGearData)
                    {
                        selectedFactions.Add(f.factionDefName);
                    }
                }
            }

            Rect contentRect = rect.ContractedBy(8f);

            // 预设名称
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect nameRect = new Rect(contentRect.x, contentRect.y, contentRect.width - 60f, 24f);
            Widgets.Label(nameRect, preset.name);

            // 展开/折叠按钮
            if (isSelected && (preset.factionGearData?.Count ?? 0) > 0)
            {
                Rect expandRect = new Rect(contentRect.xMax - 50f, contentRect.y, 50f, 24f);
                string expandText = isExpanded ? "▲" : "▼";
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(expandRect, expandText);
                if (Widgets.ButtonInvisible(expandRect))
                {
                    isExpanded = !isExpanded;
                }
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // 派系数量标签
            int factionCount = preset.factionGearData?.Count ?? 0;
            if (factionCount > 0)
            {
                Text.Font = GameFont.Tiny;
                string countLabel = $"{factionCount} {LanguageManager.Get("FGC_Factions_Label")}";
                Vector2 labelSize = Text.CalcSize(countLabel);
                Rect countRect = new Rect(
                    contentRect.xMax - labelSize.x - 5f,
                    contentRect.y + 22f,
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
                selectedFactions.Clear();
                isExpanded = false;
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

        private void DrawFactionSelection(Rect rect)
        {
            Widgets.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(10f);

            // 标题和全选按钮
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 25f);
            Widgets.Label(headerRect, LanguageManager.Get("FGC_SelectFactions") + ":");
            
            Rect selectAllRect = new Rect(innerRect.xMax - 100f, innerRect.y, 80f, 20f);
            if (Widgets.ButtonText(selectAllRect, LanguageManager.Get("SelectAll")))
            {
                if (selectedPreset?.factionGearData != null)
                {
                    selectedFactions.Clear();
                    foreach (var f in selectedPreset.factionGearData)
                    {
                        selectedFactions.Add(f.factionDefName);
                    }
                }
            }

            // 派系列表
            if (selectedPreset?.factionGearData == null || selectedPreset.factionGearData.Count == 0)
            {
                Widgets.Label(innerRect, LanguageManager.Get("NoFactionDataInPreset"));
                return;
            }

            Rect listRect = new Rect(innerRect.x, headerRect.yMax + 5f, innerRect.width, innerRect.height - 30f);
            float itemHeight = 28f;
            float totalHeight = selectedPreset.factionGearData.Count * itemHeight;

            Rect viewRect = new Rect(listRect.x, listRect.y, listRect.width - 16f, totalHeight);
            Vector2 factionScrollPos = new Vector2();
            Widgets.BeginScrollView(listRect, ref factionScrollPos, viewRect);

            float y = viewRect.y;
            foreach (var factionData in selectedPreset.factionGearData)
            {
                Rect itemRect = new Rect(viewRect.x, y, viewRect.width, itemHeight);
                
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionData.factionDefName);
                string factionName = factionDef != null ? factionDef.LabelCap.ToString() : factionData.factionDefName;

                // 复选框
                bool isChecked = selectedFactions.Contains(factionData.factionDefName);
                Rect checkboxRect = new Rect(itemRect.x, itemRect.y + 4f, 20f, 20f);
                Widgets.Checkbox(checkboxRect.position, ref isChecked);

                if (isChecked != selectedFactions.Contains(factionData.factionDefName))
                {
                    if (isChecked)
                        selectedFactions.Add(factionData.factionDefName);
                    else
                        selectedFactions.Remove(factionData.factionDefName);
                }

                // 派系名称
                Rect labelRect = new Rect(checkboxRect.xMax + 5f, itemRect.y, viewRect.width - 30f, itemHeight);
                Widgets.Label(labelRect, factionName);

                y += itemHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawButtons(Rect rect)
        {
            float buttonWidth = 130f;

            // 使用原版设置按钮
            Rect vanillaButtonRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(vanillaButtonRect, LanguageManager.Get("FGC_FirstTimePrompt_UseVanilla")))
            {
                ApplyAndClose(null, new List<string>());
            }

            // 应用选中的派系按钮
            int selectedCount = selectedFactions.Count;
            bool canApply = selectedPreset != null && selectedCount > 0;
            
            Rect applyButtonRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
            string applyText = canApply 
                ? $"{LanguageManager.Get("FGC_FirstTimePrompt_ApplyPreset")} ({selectedCount})"
                : LanguageManager.Get("FGC_FirstTimePrompt_ApplyPreset");
            
            GUI.color = canApply ? new Color(0.3f, 0.7f, 0.4f) : Color.gray;
            if (Widgets.ButtonText(applyButtonRect, applyText))
            {
                if (selectedPreset != null && selectedCount > 0)
                {
                    ApplyAndClose(selectedPreset, selectedFactions.ToList());
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

        private void ApplyAndClose(FactionGearPreset preset, List<string> factionDefNames)
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent != null)
            {
                if (preset != null && factionDefNames.Any())
                {
                    gameComponent.ApplyFactionsFromPreset(preset, factionDefNames);
                }
                else if (preset == null)
                {
                    gameComponent.ApplyPresetToSave(null);
                }
                gameComponent.MarkFirstTimePromptShown();

                if (dontAskAgain)
                {
                }
            }

            Close();

            string message;
            if (preset != null && factionDefNames.Any())
            {
                message = string.Format(LanguageManager.Get("FGC_Message_SelectedFactionsApplied"), preset.name, factionDefNames.Count);
            }
            else if (preset != null)
            {
                message = string.Format(LanguageManager.Get("FGC_Message_PresetApplied"), preset.name);
            }
            else
            {
                message = LanguageManager.Get("FGC_Message_UsingVanilla");
            }
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
