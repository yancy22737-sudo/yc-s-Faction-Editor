
using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Managers;
using FactionGearCustomizer.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_FactionEditorLite : Window
    {
        public override Vector2 InitialSize => new Vector2(900f, 700f);

        private readonly List<FactionGearPreset> availablePresets;
        private FactionGearPreset selectedPreset;
        private readonly Dictionary<string, bool> factionSelectionStates = new Dictionary<string, bool>();
        private FactionGearData selectedFactionForPreview;

        // 应用模式
        private enum ApplyMode { Add, Overwrite }
        private ApplyMode currentApplyMode = ApplyMode.Add;

        private Vector2 presetListScrollPos;
        private Vector2 factionListScrollPos;
        private Vector2 previewScrollPos;

        // 标记是否是从创建世界界面打开的lite窗口
        public static bool IsOpenedFromWorldCreation { get; private set; } = false;

        /// <summary>
        /// 重置从创建世界界面打开的标记
        /// </summary>
        public static void ResetWorldCreationFlag()
        {
            IsOpenedFromWorldCreation = false;
        }

        public Dialog_FactionEditorLite(bool fromWorldCreation = true)
        {
            IsOpenedFromWorldCreation = fromWorldCreation;
            availablePresets = FactionGearCustomizerMod.Settings.presets?.ToList() ?? new List<FactionGearPreset>();
            doCloseButton = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void PostClose()
        {
            base.PostClose();
            // 只有当不是打开主编辑器时才重置标记
            // 打开主编辑器时会保持标记，让主编辑器知道是从创建世界界面打开的
            if (!openingMainEditor)
            {
                IsOpenedFromWorldCreation = false;
            }
        }

        private bool openingMainEditor = false;

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
            Widgets.Label(titleRect, LanguageManager.Get("P2_FactionEditorLite_Title"));
            Text.Font = GameFont.Small;

            // 三栏布局：预设列表 | 派系列表 | 详细信息
            float col1Width = inRect.width * 0.25f;
            float col2Width = inRect.width * 0.35f;
            float col3Width = inRect.width * 0.35f - 20f;
            
            float startY = titleRect.yMax + 10f;
            float height = inRect.height - titleRect.yMax - 100f;

            Rect col1Rect = new Rect(inRect.x, startY, col1Width, height);
            Rect col2Rect = new Rect(col1Rect.xMax + 10f, startY, col2Width, height);
            Rect col3Rect = new Rect(col2Rect.xMax + 10f, startY, col3Width, height);

            DrawPresetList(col1Rect);
            DrawFactionList(col2Rect);
            DrawFactionDetails(col3Rect);

            DrawBottomArea(new Rect(inRect.x, inRect.yMax - 80f, inRect.width, 75f));
        }

        private void DrawPresetList(Rect rect)
        {
            Widgets.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(10f);

            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), LanguageManager.Get("P2_SelectPreset"));

            Rect listRect = new Rect(innerRect.x, innerRect.y + 28f, innerRect.width, innerRect.height - 28f);
            Widgets.DrawBoxSolid(listRect, new Color(0.1f, 0.1f, 0.1f));
            Rect listInner = listRect.ContractedBy(5f);

            float itemHeight = 30f;
            float spacing = 3f;
            float totalHeight = availablePresets.Count * (itemHeight + spacing);

            Rect viewRect = new Rect(0, 0, listInner.width - 20f, Mathf.Max(totalHeight, listInner.height));
            Widgets.BeginScrollView(listInner, ref presetListScrollPos, viewRect);

            float y = 0;
            foreach (var preset in availablePresets)
            {
                Rect itemRect = new Rect(0, y, viewRect.width, itemHeight);
                bool isSelected = selectedPreset == preset;

                if (isSelected)
                    Widgets.DrawHighlightSelected(itemRect);
                else if (Mouse.IsOver(itemRect))
                    Widgets.DrawHighlight(itemRect);

                if (Widgets.ButtonInvisible(itemRect))
                {
                    selectedPreset = preset;
                    factionSelectionStates.Clear();
                    selectedFactionForPreview = null;
                    foreach (var faction in preset.factionGearData)
                    {
                        factionSelectionStates[faction.factionDefName] = false;
                    }
                }

                Rect contentRect = itemRect.ContractedBy(5f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(contentRect, preset.name);
                Text.Anchor = TextAnchor.UpperLeft;

                y += itemHeight + spacing;
            }

            Widgets.EndScrollView();
        }

        private void DrawFactionList(Rect rect)
        {
            Widgets.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(10f);

            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), LanguageManager.Get("P2_SelectFactions"));

            if (selectedPreset == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(innerRect, LanguageManager.Get("P2_SelectPresetHint"));
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // 全选/取消全选按钮
            Rect selectAllRect = new Rect(innerRect.x, innerRect.y + 26f, 60f, 22f);
            if (Widgets.ButtonText(selectAllRect, LanguageManager.Get("yFE_SelectAll")))
            {
                foreach (var key in factionSelectionStates.Keys.ToList())
                {
                    factionSelectionStates[key] = true;
                }
            }
            Rect deselectAllRect = new Rect(selectAllRect.xMax + 5f, innerRect.y + 26f, 70f, 22f);
            if (Widgets.ButtonText(deselectAllRect, LanguageManager.Get("yFE_DeselectAll")))
            {
                foreach (var key in factionSelectionStates.Keys.ToList())
                {
                    factionSelectionStates[key] = false;
                }
            }

            Rect listRect = new Rect(innerRect.x, innerRect.y + 52f, innerRect.width, innerRect.height - 52f);
            Widgets.DrawBoxSolid(listRect, new Color(0.1f, 0.1f, 0.1f));
            Rect listInner = listRect.ContractedBy(5f);

            float itemHeight = 40f;
            float spacing = 3f;
            float totalHeight = selectedPreset.factionGearData.Count * (itemHeight + spacing);

            Rect viewRect = new Rect(0, 0, listInner.width - 20f, Mathf.Max(totalHeight, listInner.height));
            Widgets.BeginScrollView(listInner, ref factionListScrollPos, viewRect);

            float y = 0;
            foreach (var faction in selectedPreset.factionGearData)
            {
                Rect itemRect = new Rect(0, y, viewRect.width, itemHeight);
                bool isSelected = factionSelectionStates.TryGetValue(faction.factionDefName, out bool sel) && sel;
                bool isPreview = selectedFactionForPreview == faction;

                if (isSelected)
                    Widgets.DrawHighlightSelected(itemRect);
                else if (isPreview || Mouse.IsOver(itemRect))
                    Widgets.DrawHighlight(itemRect);

                Rect contentRect = itemRect.ContractedBy(5f);

                // 复选框
                Rect checkboxRect = new Rect(contentRect.x, contentRect.y + 8f, 24f, 24f);
                bool newSelected = isSelected;
                Widgets.Checkbox(checkboxRect.x, checkboxRect.y, ref newSelected);
                if (newSelected != isSelected)
                {
                    factionSelectionStates[faction.factionDefName] = newSelected;
                }

                // 派系名称
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(faction.factionDefName);
                string factionLabel = factionDef?.LabelCap ?? faction.factionDefName;
                Rect nameRect = new Rect(checkboxRect.xMax + 5f, contentRect.y, contentRect.width - 35f, 20f);
                Text.Font = GameFont.Small;
                Widgets.Label(nameRect, factionLabel);

                // 兵种数量
                int kindCount = faction.kindGearData?.Count ?? 0;
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Rect countRect = new Rect(nameRect.x, nameRect.yMax, nameRect.width, 16f);
                Widgets.Label(countRect, $"{kindCount} {LanguageManager.Get("P2_Kinds_Label")}");
                GUI.color = Color.white;

                // 点击显示详情
                if (Widgets.ButtonInvisible(itemRect))
                {
                    selectedFactionForPreview = faction;
                }

                y += itemHeight + spacing;
            }

            Widgets.EndScrollView();
        }

        private void DrawFactionDetails(Rect rect)
        {
            Widgets.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(10f);

            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), LanguageManager.Get("P2_FactionDetails"));

            if (selectedFactionForPreview == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(innerRect, LanguageManager.Get("P2_SelectFactionHint"));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Rect contentRect = new Rect(innerRect.x, innerRect.y + 28f, innerRect.width, innerRect.height - 28f);
            Widgets.DrawBoxSolid(contentRect, new Color(0.1f, 0.1f, 0.1f));
            Rect scrollRect = contentRect.ContractedBy(5f);

            var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(selectedFactionForPreview.factionDefName);
            string factionLabel = factionDef?.LabelCap ?? selectedFactionForPreview.factionDefName;

            float lineHeight = 20f;
            float y = 0;
            float totalHeight = 100f + (selectedFactionForPreview.kindGearData?.Count ?? 0) * 25f;

            Rect viewRect = new Rect(0, 0, scrollRect.width - 20f, Mathf.Max(totalHeight, scrollRect.height));
            Widgets.BeginScrollView(scrollRect, ref previewScrollPos, viewRect);

            // 派系名称
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.8f, 0.8f, 1f);
            Widgets.Label(new Rect(5f, y, viewRect.width - 10f, lineHeight), factionLabel);
            GUI.color = Color.white;
            y += lineHeight + 5f;

            // 描述
            if (!string.IsNullOrEmpty(factionDef?.description))
            {
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(5f, y, viewRect.width - 10f, lineHeight * 2), factionDef.description);
                GUI.color = Color.white;
                y += lineHeight * 2 + 10f;
            }

            // 兵种列表
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(5f, y, viewRect.width - 10f, lineHeight), LanguageManager.Get("P2_KindList") + ":");
            y += lineHeight + 5f;

            if (selectedFactionForPreview.kindGearData != null)
            {
                foreach (var kind in selectedFactionForPreview.kindGearData)
                {
                    var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kind.kindDefName);
                    string kindLabel = kindDef?.LabelCap ?? kind.kindDefName;

                    Text.Font = GameFont.Tiny;
                    Rect kindRect = new Rect(15f, y, viewRect.width - 25f, 20f);
                    
                    // 显示图标
                    if (kindDef?.lifeStages != null && kindDef.lifeStages.Count > 0)
                    {
                        var bodyGraphicData = kindDef.lifeStages[kindDef.lifeStages.Count - 1].bodyGraphicData;
                        if (bodyGraphicData?.graphicClass != null)
                        {
                            // 尝试绘制图标
                        }
                    }

                    Widgets.Label(kindRect, $"• {kindLabel}");
                    y += 20f;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawBottomArea(Rect rect)
        {
            // 分隔线
            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);

            float buttonWidth = 100f;
            float buttonHeight = 35f;

            // 取消按钮
            Rect cancelRect = new Rect(rect.x, rect.y + 10f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(cancelRect, LanguageManager.Get("Cancel")))
            {
                Close();
            }

            // 恢复默认按钮
            Rect resetRect = new Rect(cancelRect.xMax + 10f, rect.y + 10f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(resetRect, LanguageManager.Get("P2_ResetDefault")))
            {
                ResetToDefault();
            }

            // 设置按钮（打开主编辑器）
            Rect settingsRect = new Rect(resetRect.xMax + 10f, rect.y + 10f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(settingsRect, LanguageManager.Get("P2_OpenSettings")))
            {
                OpenMainEditor();
            }

            // 应用模式选择
            float modeStartX = settingsRect.xMax + 20f;
            Rect modeLabelRect = new Rect(modeStartX, rect.y + 10f, 70f, 20f);
            Widgets.Label(modeLabelRect, LanguageManager.Get("P2_ApplyMode") + ":");

            Rect addModeRect = new Rect(modeLabelRect.xMax + 5f, rect.y + 8f, 60f, 20f);
            if (Widgets.RadioButtonLabeled(addModeRect, LanguageManager.Get("P2_ModeAdd"), currentApplyMode == ApplyMode.Add))
            {
                currentApplyMode = ApplyMode.Add;
            }

            Rect overwriteModeRect = new Rect(addModeRect.xMax + 5f, rect.y + 8f, 70f, 20f);
            if (Widgets.RadioButtonLabeled(overwriteModeRect, LanguageManager.Get("P2_ModeOverwrite"), currentApplyMode == ApplyMode.Overwrite))
            {
                currentApplyMode = ApplyMode.Overwrite;
            }

            // 应用按钮
            var selectedFactions = factionSelectionStates.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            bool canApply = selectedPreset != null && selectedFactions.Any();

            Rect applyRect = new Rect(rect.xMax - buttonWidth - 10f, rect.y + 10f, buttonWidth + 10f, buttonHeight);
            GUI.color = canApply ? new Color(0.3f, 0.7f, 0.4f) : Color.gray;

            string applyLabel = LanguageManager.Get("P2_ApplyToWorld") + $" ({selectedFactions.Count})";
            if (Widgets.ButtonText(applyRect, applyLabel) && canApply)
            {
                ApplyToWorld(selectedFactions);
            }
            GUI.color = Color.white;
        }

        /// <summary>
        /// 恢复默认派系数据
        /// </summary>
        private void ResetToDefault()
        {
            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null)
            {
                Messages.Message(LanguageManager.Get("P2_NoWorldComponent"), MessageTypeDefOf.NegativeEvent);
                return;
            }

            gameComponent.ResetFactionData();
            Messages.Message(LanguageManager.Get("P2_ResetSuccess"), MessageTypeDefOf.PositiveEvent);
            
            // 清空当前选择
            selectedPreset = null;
            factionSelectionStates.Clear();
            selectedFactionForPreview = null;
        }

        /// <summary>
        /// 打开主编辑器
        /// </summary>
        private void OpenMainEditor()
        {
            openingMainEditor = true;
            Close();
            Find.WindowStack.Add(new FactionGearSettingsWindow());
        }

        private void ApplyToWorld(List<string> selectedFactionDefNames)
        {
            if (selectedPreset == null || !selectedFactionDefNames.Any())
                return;

            var gameComponent = FactionGearGameComponent.Instance;
            if (gameComponent == null)
            {
                Messages.Message(LanguageManager.Get("P2_NoWorldComponent"), MessageTypeDefOf.NegativeEvent);
                return;
            }

            // 覆盖模式：先清空现有数据
            if (currentApplyMode == ApplyMode.Overwrite)
            {
                gameComponent.savedFactionGearData.Clear();
            }

            int appliedCount = 0;
            foreach (var factionDefName in selectedFactionDefNames)
            {
                var sourceFaction = selectedPreset.factionGearData.FirstOrDefault(f => f.factionDefName == factionDefName);
                if (sourceFaction == null) continue;

                // 加入模式：检查是否已存在
                if (currentApplyMode == ApplyMode.Add)
                {
                    var existing = gameComponent.savedFactionGearData.FirstOrDefault(f => f.factionDefName == factionDefName);
                    if (existing != null)
                    {
                        // 已存在则跳过
                        continue;
                    }
                }

                var clonedFaction = sourceFaction.DeepCopy();
                clonedFaction.ResolveReferences();

                // 添加到游戏组件
                gameComponent.savedFactionGearData.Add(clonedFaction);
                appliedCount++;
            }

            if (appliedCount > 0)
            {
                gameComponent.useCustomSettings = true;
                // 记录应用的预设名称（用于显示）
                gameComponent.activePresetName = selectedPreset.name;
                
                string modeText = currentApplyMode == ApplyMode.Add ? LanguageManager.Get("P2_ModeAdd") : LanguageManager.Get("P2_ModeOverwrite");
                Messages.Message(string.Format(LanguageManager.Get("P2_AppliedToWorld"), appliedCount, modeText), MessageTypeDefOf.PositiveEvent);
                Close();
            }
            else if (currentApplyMode == ApplyMode.Add)
            {
                Messages.Message(LanguageManager.Get("P2_NoNewFactionsToAdd"), MessageTypeDefOf.RejectInput);
            }
        }
    }
}
