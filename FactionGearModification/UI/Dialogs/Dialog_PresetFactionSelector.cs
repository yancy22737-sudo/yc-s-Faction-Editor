using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Managers;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    /// <summary>
    /// 预设派系选择对话框 - 从预设中选择特定派系导入
    /// </summary>
    public class Dialog_PresetFactionSelector : Window
    {
        public override Vector2 InitialSize => new Vector2(750f, 600f);

        private readonly List<FactionGearPreset> availablePresets;
        private FactionGearPreset selectedPreset;
        private readonly Dictionary<string, bool> factionSelectionStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> factionExpandedStates = new Dictionary<string, bool>();

        private Vector2 presetListScrollPos;
        private Vector2 factionListScrollPos;
        private string presetSearchText = "";

        private PresetFactionImporter.ImportMode importMode = PresetFactionImporter.ImportMode.Merge;
        private ImportPreview currentPreview;

        private bool showOnlyValid = false;

        public Dialog_PresetFactionSelector()
        {
            availablePresets = FactionGearCustomizerMod.Settings.presets?.ToList() ?? new List<FactionGearPreset>();
            doCloseButton = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 标题
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
            Widgets.Label(titleRect, LanguageManager.Get("yFE_ImportFactions_Title"));
            Text.Font = GameFont.Small;

            // 主布局：左侧预设列表，右侧派系选择
            float splitX = inRect.width * 0.4f;
            Rect leftRect = new Rect(inRect.x, titleRect.yMax + 10f, splitX - 10f, inRect.height - titleRect.yMax - 60f);
            Rect rightRect = new Rect(leftRect.xMax + 10f, leftRect.y, inRect.width - splitX, leftRect.height);

            DrawPresetList(leftRect);
            DrawFactionSelection(rightRect);

            // 底部按钮
            DrawBottomButtons(new Rect(inRect.x, inRect.yMax - 45f, inRect.width, 40f));
        }

        private void DrawPresetList(Rect rect)
        {
            Widgets.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(10f);

            // 搜索框
            Rect searchRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 28f);
            presetSearchText = Widgets.TextField(searchRect, presetSearchText);
            if (string.IsNullOrEmpty(presetSearchText))
            {
                GUI.color = Color.gray;
                Widgets.Label(searchRect.ContractedBy(5f), LanguageManager.Get("SearchPresets") + "...");
                GUI.color = Color.white;
            }

            // 过滤预设
            var filteredPresets = availablePresets;
            if (!string.IsNullOrEmpty(presetSearchText))
            {
                filteredPresets = filteredPresets
                    .Where(p => p.name.ToLower().Contains(presetSearchText.ToLower()))
                    .ToList();
            }

            // 预设列表
            Rect listRect = new Rect(innerRect.x, searchRect.yMax + 10f, innerRect.width, innerRect.height - searchRect.height - 10f);
            float itemHeight = 70f;
            float spacing = 5f;
            float totalHeight = filteredPresets.Count * (itemHeight + spacing);

            Rect viewRect = new Rect(0, 0, listRect.width - 20f, totalHeight);
            Widgets.BeginScrollView(listRect, ref presetListScrollPos, viewRect);

            float y = 0;
            foreach (var preset in filteredPresets)
            {
                Rect itemRect = new Rect(0, y, viewRect.width, itemHeight);
                DrawPresetItem(itemRect, preset);
                y += itemHeight + spacing;
            }

            Widgets.EndScrollView();
        }

        private void DrawPresetItem(Rect rect, FactionGearPreset preset)
        {
            bool isSelected = selectedPreset == preset;

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
                SelectPreset(preset);
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
            Rect descRect = new Rect(contentRect.x, nameRect.yMax, contentRect.width, 36f);
            string desc = string.IsNullOrEmpty(preset.description)
                ? LanguageManager.Get("yFE_Preset_NoDescription")
                : preset.description;
            Widgets.Label(descRect, desc);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // 派系数量标签
            int factionCount = preset.factionGearData?.Count ?? 0;
            if (factionCount > 0)
            {
                Text.Font = GameFont.Tiny;
                string countLabel = $"{factionCount} {LanguageManager.Get("yFE_Factions_Label")}";
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

        private void SelectPreset(FactionGearPreset preset)
        {
            selectedPreset = preset;
            factionSelectionStates.Clear();
            factionExpandedStates.Clear();

            if (preset?.factionGearData != null)
            {
                foreach (var factionData in preset.factionGearData)
                {
                    factionSelectionStates[factionData.factionDefName] = false;
                    factionExpandedStates[factionData.factionDefName] = false;
                }
            }

            UpdatePreview();
        }

        private void DrawFactionSelection(Rect rect)
        {
            Widgets.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(10f);

            if (selectedPreset == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(innerRect, LanguageManager.Get("yFE_SelectPresetFirst"));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // 头部信息
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 60f);
            DrawFactionListHeader(headerRect);

            // 派系列表
            Rect listRect = new Rect(innerRect.x, headerRect.yMax + 5f, innerRect.width, innerRect.height - headerRect.height - 5f);
            DrawFactionList(listRect);
        }

        private void DrawFactionListHeader(Rect rect)
        {
            // 全选/取消全选按钮
            Rect selectAllRect = new Rect(rect.x, rect.y, 100f, 28f);
            if (Widgets.ButtonText(selectAllRect, LanguageManager.Get("yFE_SelectAll")))
            {
                foreach (var key in factionSelectionStates.Keys.ToList())
                {
                    factionSelectionStates[key] = true;
                }
                UpdatePreview();
            }

            Rect deselectAllRect = new Rect(selectAllRect.xMax + 5f, rect.y, 100f, 28f);
            if (Widgets.ButtonText(deselectAllRect, LanguageManager.Get("yFE_DeselectAll")))
            {
                foreach (var key in factionSelectionStates.Keys.ToList())
                {
                    factionSelectionStates[key] = false;
                }
                UpdatePreview();
            }

            // 仅显示有效派系复选框
            Rect validOnlyRect = new Rect(rect.x, selectAllRect.yMax + 5f, 200f, 24f);
            Widgets.CheckboxLabeled(validOnlyRect, LanguageManager.Get("yFE_ShowOnlyValid"), ref showOnlyValid);

            // 导入模式选择
            Rect modeLabelRect = new Rect(rect.xMax - 150f, rect.y, 150f, 24f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(modeLabelRect, LanguageManager.Get("yFE_ImportMode") + ":");
            Text.Anchor = TextAnchor.UpperLeft;

            Rect modeRect = new Rect(rect.xMax - 150f, modeLabelRect.yMax + 5f, 150f, 28f);
            if (Widgets.ButtonText(modeRect, GetImportModeLabel(importMode)))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption(LanguageManager.Get("yFE_ImportMode_Merge"), () => importMode = PresetFactionImporter.ImportMode.Merge),
                    new FloatMenuOption(LanguageManager.Get("yFE_ImportMode_Overwrite"), () => importMode = PresetFactionImporter.ImportMode.Overwrite)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private string GetImportModeLabel(PresetFactionImporter.ImportMode mode)
        {
            return mode == PresetFactionImporter.ImportMode.Merge
                ? LanguageManager.Get("yFE_ImportMode_Merge")
                : LanguageManager.Get("yFE_ImportMode_Overwrite");
        }

        private void DrawFactionList(Rect rect)
        {
            if (selectedPreset?.factionGearData == null) return;

            var factionsToShow = selectedPreset.factionGearData;
            if (showOnlyValid)
            {
                var validationResults = PresetFactionImporter.ValidatePresetFactions(selectedPreset);
                factionsToShow = factionsToShow
                    .Where(f => validationResults.TryGetValue(f.factionDefName, out var result) && result.IsValid)
                    .ToList();
            }

            float itemHeight = 60f;
            float expandedHeight = 100f;
            float spacing = 5f;

            float totalHeight = 0;
            foreach (var factionData in factionsToShow)
            {
                totalHeight += itemHeight;
                if (factionExpandedStates.TryGetValue(factionData.factionDefName, out bool expanded) && expanded)
                {
                    totalHeight += expandedHeight;
                }
                totalHeight += spacing;
            }

            Rect viewRect = new Rect(0, 0, rect.width - 20f, totalHeight);
            Widgets.BeginScrollView(rect, ref factionListScrollPos, viewRect);

            float y = 0;
            foreach (var factionData in factionsToShow)
            {
                var validation = PresetFactionImporter.ValidatePresetFactions(selectedPreset)
                    .TryGetValue(factionData.factionDefName, out var v) ? v : null;

                bool isExpanded = factionExpandedStates.TryGetValue(factionData.factionDefName, out bool exp) && exp;
                float currentItemHeight = itemHeight + (isExpanded ? expandedHeight : 0);

                Rect itemRect = new Rect(0, y, viewRect.width, currentItemHeight);
                DrawFactionItem(itemRect, factionData, validation, isExpanded);

                y += currentItemHeight + spacing;
            }

            Widgets.EndScrollView();
        }

        private void DrawFactionItem(Rect rect, FactionGearData factionData, FactionValidationResult validation, bool isExpanded)
        {
            bool isSelected = factionSelectionStates.TryGetValue(factionData.factionDefName, out bool sel) && sel;
            bool isValid = validation?.IsValid ?? false;

            // 背景
            if (isSelected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Rect contentRect = rect.ContractedBy(5f);

            // 复选框
            Rect checkboxRect = new Rect(contentRect.x, contentRect.y + 5f, 24f, 24f);
            bool newSelected = isSelected;
            Widgets.Checkbox(checkboxRect.x, checkboxRect.y, ref newSelected);
            if (newSelected != isSelected)
            {
                factionSelectionStates[factionData.factionDefName] = newSelected;
                UpdatePreview();
            }

            // 派系名称
            string factionLabel = validation?.FactionLabel ?? factionData.factionDefName;
            Rect nameRect = new Rect(checkboxRect.xMax + 5f, contentRect.y, contentRect.width - 60f, 24f);

            if (!isValid)
            {
                GUI.color = Color.red;
            }
            else if (isSelected)
            {
                GUI.color = new Color(0.6f, 1f, 0.6f);
            }

            Text.Font = GameFont.Small;
            Widgets.Label(nameRect, factionLabel);
            GUI.color = Color.white;

            // 展开按钮
            Rect expandRect = new Rect(contentRect.xMax - 50f, contentRect.y, 50f, 24f);
            string expandLabel = isExpanded ? "<<" : ">>";
            if (Widgets.ButtonText(expandRect, expandLabel))
            {
                factionExpandedStates[factionData.factionDefName] = !isExpanded;
            }

            // 状态信息
            Rect infoRect = new Rect(nameRect.x, nameRect.yMax + 2f, nameRect.width, 20f);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;

            int kindCount = factionData.kindGearData?.Count ?? 0;
            string infoText = $"{kindCount} {LanguageManager.Get("yFE_Kinds_Label")}";

            if (!isValid)
            {
                infoText += $" - {LanguageManager.Get("yFE_Invalid")}: {validation?.InvalidReason}";
                GUI.color = Color.red;
            }
            else if (validation?.RequiredMods?.Any() == true)
            {
                var missingModCount = validation.RequiredMods.Count(mod => !LoadedModManager.RunningMods.Any(m => m.Name == mod));
                if (missingModCount > 0)
                {
                    infoText += $" - {missingModCount} {LanguageManager.Get("yFE_MissingMods_Label")}";
                    GUI.color = Color.yellow;
                }
            }

            Widgets.Label(infoRect, infoText);
            GUI.color = Color.white;

            // 展开详情
            if (isExpanded)
            {
                Rect detailsRect = new Rect(contentRect.x + 20f, infoRect.yMax + 5f, contentRect.width - 20f, 70f);
                DrawFactionDetails(detailsRect, factionData, validation);
            }
        }

        private void DrawFactionDetails(Rect rect, FactionGearData factionData, FactionValidationResult validation)
        {
            Widgets.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(5f);

            float y = innerRect.y;

            // 显示缺失的Mod
            if (validation?.RequiredMods?.Any() == true)
            {
                var missingMods = validation.RequiredMods.Where(mod => !LoadedModManager.RunningMods.Any(m => m.Name == mod)).ToList();
                if (missingMods.Any())
                {
                    GUI.color = Color.yellow;
                    Widgets.Label(new Rect(innerRect.x, y, innerRect.width, 20f),
                        LanguageManager.Get("yFE_MissingMods") + ": " + string.Join(", ", missingMods));
                    GUI.color = Color.white;
                    y += 22f;
                }
            }

            // 显示缺失的DLC
            if (validation?.RequiredDLC?.Any() == true)
            {
                GUI.color = Color.yellow;
                Widgets.Label(new Rect(innerRect.x, y, innerRect.width, 20f),
                    LanguageManager.Get("yFE_MissingDLC") + ": " + string.Join(", ", validation.RequiredDLC));
                GUI.color = Color.white;
                y += 22f;
            }

            // 显示无效的兵种
            if (validation?.InvalidKinds?.Any() == true)
            {
                GUI.color = Color.red;
                Widgets.Label(new Rect(innerRect.x, y, innerRect.width, 20f),
                    LanguageManager.Get("yFE_InvalidKinds") + $": {validation.InvalidKinds.Count}");
                GUI.color = Color.white;
            }
        }

        private void UpdatePreview()
        {
            var selectedFactions = factionSelectionStates
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            if (selectedPreset != null && selectedFactions.Any())
            {
                currentPreview = PresetFactionImporter.GetImportPreview(selectedPreset, selectedFactions);
            }
            else
            {
                currentPreview = null;
            }
        }

        private void DrawBottomButtons(Rect rect)
        {
            float buttonWidth = 120f;
            float gap = 10f;

            // 取消按钮
            Rect cancelRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(cancelRect, LanguageManager.Get("Cancel")))
            {
                Close();
            }

            // 导入按钮
            int selectedCount = factionSelectionStates.Count(kvp => kvp.Value);
            bool canImport = selectedCount > 0 && currentPreview?.Factions.Any(f => f.IsValid) == true;

            Rect importRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
            GUI.color = canImport ? new Color(0.3f, 0.7f, 0.4f) : Color.gray;

            string importLabel = LanguageManager.Get("yFE_Import") + $" ({selectedCount})";
            if (Widgets.ButtonText(importRect, importLabel) && canImport)
            {
                ExecuteImport();
            }
            GUI.color = Color.white;

            // 预览信息
            if (currentPreview != null)
            {
                int validCount = currentPreview.Factions.Count(f => f.IsValid);
                int invalidCount = currentPreview.Factions.Count(f => !f.IsValid);

                string previewText = "";
                if (validCount > 0)
                {
                    previewText += $"{validCount} {LanguageManager.Get("yFE_ReadyToImport")}";
                }
                if (invalidCount > 0)
                {
                    if (!string.IsNullOrEmpty(previewText)) previewText += ", ";
                    previewText += $"{invalidCount} {LanguageManager.Get("yFE_InvalidSkipped")}";
                }

                Rect previewRect = new Rect(cancelRect.xMax + gap, rect.y, importRect.x - cancelRect.xMax - gap * 2, rect.height);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = invalidCount > 0 ? Color.yellow : Color.green;
                Widgets.Label(previewRect, previewText);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void ExecuteImport()
        {
            var selectedFactions = factionSelectionStates
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            if (!selectedFactions.Any()) return;

            var result = PresetFactionImporter.ImportFactions(selectedPreset, selectedFactions, importMode);

            if (result.Success)
            {
                string message = string.Format(LanguageManager.Get("yFE_ImportSuccess"), result.ImportedFactions.Count);
                Messages.Message(message, MessageTypeDefOf.PositiveEvent);

                if (result.MissingMods.Any())
                {
                    string warning = string.Format(LanguageManager.Get("yFE_ImportWarning_MissingMods"),
                        string.Join(", ", result.MissingMods.Distinct()));
                    Messages.Message(warning, MessageTypeDefOf.CautionInput);
                }

                // 刷新编辑器缓存
                FactionGearEditor.RefreshAllCaches();

                Close();
            }
            else
            {
                string error = result.ErrorMessage ?? LanguageManager.Get("yFE_ImportFailed");
                Messages.Message(error, MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}
