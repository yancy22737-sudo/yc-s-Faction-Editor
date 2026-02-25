using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;
using FactionGearCustomizer.Core;

namespace FactionGearCustomizer
{
    public class PresetManagerWindow : Window
    {
        // [修复] 环世界原生窗口必须通过重写 InitialSize 来设定弹窗大�?
        public override Vector2 InitialSize => new Vector2(850f, 650f);

        private Vector2 presetListScrollPos = Vector2.zero;
        private Vector2 detailsScrollPos = Vector2.zero;
        private string newPresetName = "";
        private string newPresetDescription = "";
        private FactionGearPreset selectedPreset = null;
        private Vector2 factionPreviewScrollPos = Vector2.zero;
        private Vector2 modListScrollPos = Vector2.zero;
        private string presetSearchText = "";

        // Preview fields
        private Pawn previewPawn;
        private FactionGearPreset lastPreviewPreset;
        private Rot4 previewRotation = Rot4.South;
        private string previewError = null;
        private FactionGearData selectedPreviewFactionData;
        private KindGearData selectedPreviewKindData;
        private Vector2 previewGearScrollPos = Vector2.zero;

        public PresetManagerWindow() : base()
        {
            // 删除错误�?windowRect 设定
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true; // 建议加上：打开预设管理器时暂停游戏底层时间
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 布局
            Rect listRect = new Rect(inRect.x, inRect.y, 300, inRect.height);
            Rect detailsRect = new Rect(inRect.x + 310, inRect.y, inRect.width - 310, inRect.height);

            // 绘制预设列表
            DrawPresetList(listRect);
            
            // 绘制预设详情
            DrawPresetDetails(detailsRect);
        }

        private void DrawPresetList(Rect rect)
        {
            WidgetsUtils.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);
            
            // 1. 顶部标题和搜索框
            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), LanguageManager.Get("SavedPresets"));
            presetSearchText = DrawTextFieldWithPlaceholder(new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 24f), presetSearchText, LanguageManager.Get("SearchPresets") + "...");
            
            // 2. 列表区域动态高�?
            float bottomAreaHeight = 95f;
            Rect listOutRect = new Rect(innerRect.x, innerRect.y + 55f, innerRect.width, innerRect.height - 55f - bottomAreaHeight);
            
            // 3. 执行搜索过滤
            List<FactionGearPreset> presets = FactionGearCustomizerMod.Settings.presets;
            if (!string.IsNullOrEmpty(presetSearchText))
            {
                presets = presets.Where(p => p.name.ToLower().Contains(presetSearchText.ToLower())).ToList();
            }
            
            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, presets.Count * 30f);
            
            Widgets.BeginScrollView(listOutRect, ref presetListScrollPos, listViewRect);
            float y = 0;
            foreach (var preset in presets)
            {
                Rect rowRect = new Rect(0, y, listViewRect.width, 24f);
                if (selectedPreset == preset)
                    Widgets.DrawHighlightSelected(rowRect);
                else if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);
                
                string displayName = preset.name;
                bool isActive = preset.name == FactionGearCustomizerMod.Settings.currentPresetName;
                if (isActive)
                {
                    displayName += " " + LanguageManager.Get("Active");
                    GUI.color = Color.green;
                }
                
                Widgets.Label(rowRect, displayName);
                GUI.color = Color.white;
                
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedPreset = preset;
                }
                y += 30f;
            }
            Widgets.EndScrollView();
            
            // 【新增】将新建预设移到左侧底部
            float bottomY = listOutRect.yMax + 5f;
            Widgets.DrawLineHorizontal(innerRect.x, bottomY, innerRect.width);
            
            // Import Button
            Rect importRect = new Rect(innerRect.x, bottomY + 5f, innerRect.width, 24f);
            if (Widgets.ButtonText(importRect, LanguageManager.Get("ImportFromClipboard")))
            {
                ImportPreset();
            }
            TooltipHandler.TipRegion(importRect, LanguageManager.Get("ImportFromClipboardTooltip"));
            
            Widgets.DrawLineHorizontal(innerRect.x, bottomY + 34f, innerRect.width);

            // Create New Section
            Widgets.Label(new Rect(innerRect.x, bottomY + 40f, innerRect.width, 20f), LanguageManager.Get("CreateNew") + ":");
            newPresetName = DrawTextFieldWithPlaceholder(new Rect(innerRect.x, bottomY + 62f, innerRect.width - 65f, 24f), newPresetName, LanguageManager.Get("Name") + "...");
            
            Rect addPresetRect = new Rect(innerRect.xMax - 60f, bottomY + 62f, 60f, 24f);
            if (Widgets.ButtonText(addPresetRect, LanguageManager.Get("Add")))
            {
                CreateNewPreset();
            }
            TooltipHandler.TipRegion(addPresetRect, LanguageManager.Get("CreatePresetTooltip"));
        }

        private void DrawPresetDetails(Rect rect)
        {
            WidgetsUtils.DrawMenuSection(rect);
            if (selectedPreset == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, LanguageManager.Get("SelectPresetFirst"));
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Rect innerRect = rect.ContractedBy(10f);
            
            // 底部按钮区域高度
            float bottomHeight = 35f;
            // 内容区域高度
            float contentHeight = innerRect.height - bottomHeight - 5f;
            
            Rect contentRect = new Rect(innerRect.x, innerRect.y, innerRect.width, contentHeight);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(contentRect);

            Rect labelRect1 = listing.GetRect(Text.CalcHeight(LanguageManager.Get("PresetDetails") + ":", listing.ColumnWidth));
            Widgets.Label(labelRect1, LanguageManager.Get("PresetDetails") + ":");
            
            // Name
            Rect nameRect = listing.GetRect(24f);
            string newName = DrawTextFieldWithPlaceholder(nameRect, selectedPreset.name, LanguageManager.Get("PresetName") + "...");
            if (newName != selectedPreset.name)
            {
                selectedPreset.name = newName;
            }
            listing.Gap(4f);
            
            // Description
            Rect descRect = listing.GetRect(50f);
            string newDesc = DrawTextFieldWithPlaceholder(descRect, selectedPreset.description, LanguageManager.Get("Description") + "...", true);
            if (newDesc != selectedPreset.description)
            {
                selectedPreset.description = newDesc;
            }
            
            listing.Gap(10f);
            
            // Save/Update Section
            Rect labelRect2 = listing.GetRect(Text.CalcHeight(LanguageManager.Get("Management"), listing.ColumnWidth));
            Widgets.Label(labelRect2, LanguageManager.Get("Management"));
            
            // Two columns for update buttons
            Rect updateRow = listing.GetRect(30f);
            Rect saveMetaRect = updateRow.LeftHalf().ContractedBy(2f);
            if (Widgets.ButtonText(saveMetaRect, LanguageManager.Get("SaveNameDesc")))
            {
                 SavePreset();
                 Notify(LanguageManager.Get("PresetMetadataSaved"));
            }
            TooltipHandler.TipRegion(saveMetaRect, LanguageManager.Get("SavePresetMetadataTooltip"));
            
            Rect updateFromGameRect = updateRow.RightHalf().ContractedBy(2f);
            if (Widgets.ButtonText(updateFromGameRect, LanguageManager.Get("UpdateFromGame"))) 
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("OverwritePresetConfirm").Replace("{0}", selectedPreset.name),
                    LanguageManager.Get("Yes"), delegate { SaveFromCurrentSettings(); Notify(LanguageManager.Get("PresetUpdatedFromGameSettings")); },
                    LanguageManager.Get("No"), null
                ));
            }
            TooltipHandler.TipRegion(updateFromGameRect, LanguageManager.Get("UpdateFromGameTooltip"));

            listing.GapLine();

            // Mod List
            listing.Label(LanguageManager.Get("RequiredMods") + ":");
            DrawModList(listing.GetRect(100f)); 
            listing.Gap(10f);

            Rect labelRect4 = listing.GetRect(Text.CalcHeight(LanguageManager.Get("FactionPreview") + ":", listing.ColumnWidth));
            Widgets.Label(labelRect4, LanguageManager.Get("FactionPreview") + ":");
            // 使用剩余空间，但要保留底部按钮空�?
            float remainingHeight = contentRect.height - listing.CurHeight;
            if (remainingHeight > 50f)
            {
                DrawFactionPreview(listing.GetRect(remainingHeight));
            }
            
            listing.End();

            // Bottom Actions
            Rect bottomRect = new Rect(innerRect.x, innerRect.yMax - 30f, innerRect.width, 30f);
            float btnWidth = (bottomRect.width - 10f) / 3f;
            
            if (Widgets.ButtonText(new Rect(bottomRect.x, bottomRect.y, btnWidth, 30f), LanguageManager.Get("LoadPreset"))) ApplyPreset();
            TooltipHandler.TipRegion(new Rect(bottomRect.x, bottomRect.y, btnWidth, 30f), LanguageManager.Get("LoadPresetTooltip"));
            
            if (Widgets.ButtonText(new Rect(bottomRect.x + btnWidth + 5f, bottomRect.y, btnWidth, 30f), LanguageManager.Get("Export"))) ExportPreset();
            TooltipHandler.TipRegion(new Rect(bottomRect.x + btnWidth + 5f, bottomRect.y, btnWidth, 30f), LanguageManager.Get("ExportTooltip"));
            
            GUI.color = new Color(1f, 0.5f, 0.5f);
            Rect deletePresetRect = new Rect(bottomRect.x + (btnWidth + 5f) * 2, bottomRect.y, btnWidth, 30f);
            if (Widgets.ButtonText(deletePresetRect, LanguageManager.Get("Delete"))) DeletePreset();
            GUI.color = Color.white;
            TooltipHandler.TipRegion(deletePresetRect, LanguageManager.Get("DeletePresetTooltip"));
        }

        private void CreateNewPreset()
        {
            // 如果玩家没填名字，自动生成一个带时间戳的默认�?
            string finalName = string.IsNullOrEmpty(newPresetName) ? LanguageManager.Get("NewPreset") + " " + DateTime.Now.ToString("MM-dd HH:mm") : newPresetName;
            
            var existingPreset = FactionGearCustomizerMod.Settings.presets.FirstOrDefault(p => p.name == finalName);
            if (existingPreset != null)
            {
                // 如果名称已存在，弹出覆盖确认对话�?
                Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("PresetAlreadyExists").Replace("{0}", finalName),
                    LanguageManager.Get("Overwrite"), delegate
                    {
                        // 覆盖现有预设
                        existingPreset.SaveFromCurrentSettings(FactionGearCustomizerMod.Settings.factionGearData);
                        existingPreset.description = newPresetDescription;
                        FactionGearCustomizerMod.Settings.UpdatePreset(existingPreset);
                        selectedPreset = existingPreset;
                        newPresetName = "";
                        newPresetDescription = "";
                        Notify(LanguageManager.Get("PresetOverwritten").Replace("{0}", finalName));
                    },
                    LanguageManager.Get("Cancel"), null, null, true));
            }
            else
            {
                // 创建新预设 - 自动从当前游戏设置保存所有派系数据
                var newPreset = new FactionGearPreset();
                newPreset.name = finalName;
                newPreset.description = newPresetDescription;
                
                // 创建预设时自动保存当前所有派系数据（包含已修改的派系和兵种）
                newPreset.SaveFromCurrentSettings(FactionGearCustomizerMod.Settings.factionGearData);
                
                FactionGearCustomizerMod.Settings.AddPreset(newPreset);
                // 设置为当前预设
                FactionGearCustomizerMod.Settings.currentPresetName = newPreset.name;
                
                selectedPreset = newPreset;
                newPresetName = "";
                newPresetDescription = "";
                Notify(LanguageManager.Get("NewPresetCreated"));
            }
        }

        private void SavePreset()
        {
            if (selectedPreset != null)
            {
                selectedPreset.CalculateRequiredMods();
                FactionGearCustomizerMod.Settings.UpdatePreset(selectedPreset);
            }
        }

        private void ApplyPreset()
        {
            if (selectedPreset == null) return;

            List<string> missingMods = new List<string>();
            foreach (var modName in selectedPreset.requiredMods)
            {
                if (!LoadedModManager.RunningMods.Any(m => m.Name == modName))
                {
                    missingMods.Add(modName);
                }
            }

            if (missingMods.Any())
            {
                string missingList = string.Join("\n- ", missingMods);
                Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("MissingModsWarningMessage").Replace("{0}", missingList),
                    LanguageManager.Get("MissingModsWarningRiskIt"), 
                    delegate { ShowApplyConfirmation(); },
                    LanguageManager.Get("MissingModsWarningCancel"), 
                    null, 
                    null, 
                    true
                ));
            }
            else
            {
                ShowApplyConfirmation();
            }
        }

        private void ShowApplyConfirmation()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                LanguageManager.Get("LoadPresetConfirmMessage"),
                LanguageManager.Get("LoadPresetOverwrite"), delegate
                {
                    FactionGearCustomizerMod.Settings.ResetToDefault();
                    ExecuteApplyPreset();
                },
                LanguageManager.Get("LoadPresetMerge"), delegate
                {
                    ExecuteApplyPreset();
                },
                null, true));
        }

        private void ExecuteApplyPreset()
        {
            if (selectedPreset == null) return;

            try
            {
                // 【修复】优先使用 GameComponent 管理存档级别的数据
                var gameComponent = FactionGearGameComponent.Instance;
                if (gameComponent != null)
                {
                    // 使用 GameComponent 的 ApplyPresetToSave 方法，它会正确同步所有数据
                    gameComponent.ApplyPresetToSave(selectedPreset);
                }
                else
                {
                    // 降级处理：如果没有 GameComponent（主菜单场景），只更新 Settings
                    FactionGearCustomizerMod.Settings.factionGearData.Clear();
                    if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
                    {
                        FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
                    }
                    
                    if (selectedPreset.factionGearData != null)
                    {
                        foreach (var factionData in selectedPreset.factionGearData)
                        {
                            if (factionData == null || string.IsNullOrEmpty(factionData.factionDefName)) continue;

                            var clonedFactionData = factionData.DeepCopy();
                            FactionGearCustomizerMod.Settings.factionGearData.Add(clonedFactionData);
                            
                            if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
                            {
                                if (!FactionGearCustomizerMod.Settings.factionGearDataDict.ContainsKey(clonedFactionData.factionDefName))
                                {
                                    FactionGearCustomizerMod.Settings.factionGearDataDict.Add(clonedFactionData.factionDefName, clonedFactionData);
                                }
                                else
                                {
                                    FactionGearCustomizerMod.Settings.factionGearDataDict[clonedFactionData.factionDefName] = clonedFactionData;
                                }
                            }
                        }
                    }

                    FactionGearCustomizerMod.Settings.currentPresetName = selectedPreset.name;
                    FactionGearCustomizerMod.Settings.Write();
                }
                
                // 刷新UI缓存
                FactionGearEditor.RefreshAllCaches();
                Notify(LanguageManager.Get("PresetApplied"));
                this.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Error applying preset: {ex}");
                Notify(LanguageManager.Get("ApplyPresetFailed"));
            }
        }

        private void DeletePreset()
        {
            if (selectedPreset != null)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("DeletePresetConfirm").Replace("{0}", selectedPreset.name),
                    LanguageManager.Get("Delete"), delegate
                    {
                        if (FactionGearCustomizerMod.Settings.currentPresetName == selectedPreset.name)
                        {
                            FactionGearCustomizerMod.Settings.currentPresetName = null;
                        }

                        FactionGearCustomizerMod.Settings.RemovePreset(selectedPreset);
                        selectedPreset = null;
                        Notify(LanguageManager.Get("PresetDeleted"));
                    },
                    LanguageManager.Get("Cancel"), null, null, true));
            }
        }

        private void SaveFromCurrentSettings()
        {
            if (selectedPreset != null)
            {
                selectedPreset.SaveFromCurrentSettings(FactionGearCustomizerMod.Settings.factionGearData);
                FactionGearCustomizerMod.Settings.UpdatePreset(selectedPreset);
            }
        }

        private void ExportPreset()
        {
            if (selectedPreset != null)
            {
                try
                {
                    // 使用新的 PresetIOManager 导出预设
                    string base64Content = PresetIOManager.ExportToBase64(selectedPreset);
                    if (!string.IsNullOrEmpty(base64Content))
                    {
                        // 复制到剪贴板
                        GUIUtility.systemCopyBuffer = base64Content;
                        Log.Message("[FactionGearCustomizer] Preset exported to clipboard!");
                        Notify(LanguageManager.Get("PresetExportedToClipboard").Replace("{0}", selectedPreset.name));
                    }
                    else
                    {
                        Log.Message("[FactionGearCustomizer] Export failed!");
                    }
                }
                catch (System.Exception e)
                {
                    Log.Message("[FactionGearCustomizer] Export failed: " + e.Message);
                }
            }
        }

        private void ImportPreset()
        {
            try
            {
                // 从剪贴板读取
                string base64Content = GUIUtility.systemCopyBuffer;
                if (string.IsNullOrEmpty(base64Content))
                {
                    Log.Message("[FactionGearCustomizer] " + LanguageManager.Get("ClipboardIsEmpty"));
                    Notify(LanguageManager.Get("ClipboardIsEmpty"));
                    return;
                }
                
                // 使用新的 PresetIOManager 导入预设
                FactionGearPreset newPreset = PresetIOManager.ImportFromBase64(base64Content);
                if (newPreset != null)
                {
                    // 检查重名并处理
                    string originalName = newPreset.name;
                    string finalName = originalName;
                    int count = 1;
                    while (FactionGearCustomizerMod.Settings.presets.Any(p => p.name == finalName))
                    {
                        finalName = $"{originalName} {LanguageManager.Get("ImportedCopySuffix").Replace("{0}", count.ToString())}";
                        count++;
                    }
                    newPreset.name = finalName;

                    // 添加到预设列�?
                    FactionGearCustomizerMod.Settings.AddPreset(newPreset);
                    selectedPreset = newPreset;
                    Log.Message("[FactionGearCustomizer] Preset imported successfully!");
                    Notify(LanguageManager.Get("PresetImportedFromClipboard").Replace("{0}", newPreset.name));
                }
                else
                {
                    Log.Message("[FactionGearCustomizer] Import failed: Invalid preset data!");
                    Notify(LanguageManager.Get("ImportFailedInvalidData"));
                }
            }
            catch (System.Exception e)
            {
                Log.Message("[FactionGearCustomizer] Import failed: " + e.Message);
                Notify(LanguageManager.Get("ImportFailed").Replace("{0}", e.Message));
            }
        }

        private void DrawFactionPreview(Rect rect)
        {
            WidgetsUtils.DrawBox(rect);
            Rect innerRect = rect.ContractedBy(5f);

            if (selectedPreset == null || !selectedPreset.factionGearData.Any())
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(innerRect, LanguageManager.Get("NoFactionDataInPreset"));
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Split into List (Left) and Preview (Right)
            float previewWidth = 200f;
            if (innerRect.width < 350f) previewWidth = 0f; // Too small for preview

            Rect listRect = previewWidth > 0 ? new Rect(innerRect.x, innerRect.y, innerRect.width - previewWidth - 10f, innerRect.height) : innerRect;
            
            // Draw List
            Widgets.BeginScrollView(listRect, ref factionPreviewScrollPos, new Rect(0, 0, listRect.width - 16f, selectedPreset.factionGearData.Sum(f => 25f + f.kindGearData.Count * 20f + 10f)));
            float y = 0;
            
            foreach (var factionData in selectedPreset.factionGearData)
            {
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionData.factionDefName);
                string factionName = factionDef != null ? factionDef.LabelCap.ToString() : factionData.factionDefName;
                
                Widgets.Label(new Rect(0, y, listRect.width - 16f, 24f), factionName);
                y += 25f;
                
                foreach (var kindData in factionData.kindGearData)
                {
                    var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
                    string kindName = kindDef != null ? kindDef.LabelCap.ToString() : kindData.kindDefName;
                    
                    Rect kindRect = new Rect(20, y, listRect.width - 36f, 24f);
                    
                    if (selectedPreviewKindData == kindData)
                        Widgets.DrawHighlightSelected(kindRect);
                    else if (Mouse.IsOver(kindRect))
                        Widgets.DrawHighlight(kindRect);
                    
                    Widgets.Label(kindRect, "- " + kindName);
                    
                    if (Widgets.ButtonInvisible(kindRect))
                    {
                        selectedPreviewFactionData = factionData;
                        selectedPreviewKindData = kindData;
                        GeneratePreviewPawn();
                    }

                    y += 20f;
                }
                y += 10f;
            }
            
            Widgets.EndScrollView();

            // Draw Preview if space allows
            if (previewWidth > 0)
            {
                Rect previewRect = new Rect(innerRect.xMax - previewWidth, innerRect.y, previewWidth, innerRect.height);
                DrawPawnPreview(previewRect);
            }
        }

        private void DrawPawnPreview(Rect rect)
        {
            // Title
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), LanguageManager.Get("VisualPreview"));
            Text.Anchor = TextAnchor.UpperLeft;
            
            if (selectedPreset != lastPreviewPreset)
            {
                selectedPreviewFactionData = null;
                selectedPreviewKindData = null;
                GeneratePreviewPawn();
                lastPreviewPreset = selectedPreset;
            }
            
            float pawnHeight = Mathf.Min(rect.height * 0.6f, 300f);
            Rect pawnRect = new Rect(rect.x, rect.y + 30f, rect.width, pawnHeight);
            WidgetsUtils.DrawWindowBackground(pawnRect);

            if (previewPawn != null)
            {
                 RenderTexture image = WidgetsUtils.GetPortrait(previewPawn, new Vector2(pawnRect.width, pawnRect.height), previewRotation, Vector3.zero, 1f);
                 if (image != null) GUI.DrawTexture(pawnRect, image);
                 
                 // Rotation
                 if (Widgets.ButtonText(new Rect(rect.x, pawnRect.yMax - 24f, 40f, 24f), "<"))
                 {
                     previewRotation.Rotate(RotationDirection.Counterclockwise);
                     WidgetsUtils.SetPortraitDirty(previewPawn);
                 }
                 if (Widgets.ButtonText(new Rect(rect.xMax - 40f, pawnRect.yMax - 24f, 40f, 24f), ">"))
                 {
                     previewRotation.Rotate(RotationDirection.Clockwise);
                     WidgetsUtils.SetPortraitDirty(previewPawn);
                 }
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(pawnRect, previewError ?? LanguageManager.Get("NoPawn"));
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // Reroll Button (Center Bottom of Pawn Area)
            if (Widgets.ButtonText(new Rect(rect.x + 45f, pawnRect.yMax - 24f, rect.width - 90f, 24f), LanguageManager.Get("Reroll")))
            {
                GeneratePreviewPawn();
            }
            
            // Gear List (Below Pawn)
            float listY = pawnRect.yMax + 5f;
            float listHeight = rect.height - (listY - rect.y);
            
            if (listHeight > 50f && previewPawn != null)
            {
                Rect listRect = new Rect(rect.x, listY, rect.width, listHeight);
                Widgets.DrawMenuSection(listRect);
                Rect innerList = listRect.ContractedBy(4f);
                
                // Calculate content height roughly
                float contentHeight = 0f;
                if (previewPawn.equipment != null) contentHeight += 30f + previewPawn.equipment.AllEquipmentListForReading.Count * 25f;
                if (previewPawn.apparel != null) contentHeight += 30f + previewPawn.apparel.WornApparel.Count * 25f;
                contentHeight += 20f;

                Widgets.BeginScrollView(innerList, ref previewGearScrollPos, new Rect(0, 0, innerList.width - 16f, contentHeight));
                Listing_Standard list = new Listing_Standard();
                list.Begin(new Rect(0, 0, innerList.width - 16f, contentHeight));
                
                // Equipment
                if (previewPawn.equipment != null && previewPawn.equipment.AllEquipmentListForReading.Any())
                {
                    list.Label(LanguageManager.Get("EquippedGear"));
                    foreach (var eq in previewPawn.equipment.AllEquipmentListForReading)
                    {
                        var qualityComp = eq.GetComp<CompQuality>();
                        string qualityStr = qualityComp != null ? (LanguageManager.Get("Quality" + qualityComp.Quality.ToString()) ?? qualityComp.Quality.ToString()) : (LanguageManager.Get("QualityNormal") ?? "Normal");
                        WidgetsUtils.Label(list, "- " + eq.LabelCap + " (" + qualityStr + ")");
                    }
                    list.Gap(5f);
                }

                // Apparel
                if (previewPawn.apparel != null && previewPawn.apparel.WornApparel.Any())
                {
                    list.Label(LanguageManager.Get("ApparelWorn"));
                    foreach (var app in previewPawn.apparel.WornApparel)
                    {
                         var qualityComp = app.GetComp<CompQuality>();
                         string qualityStr = qualityComp != null ? (LanguageManager.Get("Quality" + qualityComp.Quality.ToString()) ?? qualityComp.Quality.ToString()) : (LanguageManager.Get("QualityNormal") ?? "Normal");
                         WidgetsUtils.Label(list, "- " + app.LabelCap + " (" + qualityStr + ")");
                    }
                }
                
                list.End();
                Widgets.EndScrollView();
            }
        }

        private void GeneratePreviewPawn()
        {
            if (previewPawn != null && !previewPawn.Destroyed)
            {
                previewPawn.Destroy();
                previewPawn = null;
            }
            previewError = null;

            if (selectedPreset == null || !selectedPreset.factionGearData.Any()) return;

            try
            {
                // If nothing selected, pick random
                if (selectedPreviewFactionData == null || selectedPreviewKindData == null)
                {
                    var randomFaction = selectedPreset.factionGearData.RandomElement();
                    if (randomFaction != null && randomFaction.kindGearData.Any())
                    {
                        selectedPreviewFactionData = randomFaction;
                        selectedPreviewKindData = randomFaction.kindGearData.RandomElement();
                    }
                }

                if (selectedPreviewFactionData == null || selectedPreviewKindData == null)
                {
                     previewError = LanguageManager.Get("PreviewError_NoData");
                     return;
                }

                var factionData = selectedPreviewFactionData;
                var kindData = selectedPreviewKindData;
                
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionData.factionDefName);
                var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);

                if (factionDef == null || kindDef == null)
                {
                    previewError = LanguageManager.Get("PreviewError_DefMissing");
                    return;
                }

                Faction faction = Find.FactionManager.FirstFactionOfDef(factionDef);
                if (faction == null)
                {
                    previewError = LanguageManager.Get("PreviewError_FactionMissing");
                    return;
                }

                // Inject Preset into GearApplier
                GearApplier.PreviewPreset = selectedPreset;
                
                // 跳过 creepjoiner 类型的 PawnKindDef，因为它们需要特殊的生成逻辑
                if (kindDef?.race?.defName == "CreepJoiner")
                {
                    Log.Warning($"[FactionGearCustomizer] Skipping preview for creepjoiner kindDef: {kindDef.defName}");
                    previewError = "Creepjoiner preview not supported";
                    return;
                }

                PawnGenerationRequest request = new PawnGenerationRequest(
                    kindDef, faction, PawnGenerationContext.NonPlayer, -1, 
                    true, false, false, false, true, 1f, false, true, true, false, false);
                
                previewPawn = PawnGenerator.GeneratePawn(request);
            }
            catch (Exception ex)
            {
                Log.Warning("[FactionGearCustomizer] Preview generation failed: " + ex.Message);
                previewError = LanguageManager.Get("PreviewError_Error");
            }
            finally
            {
                GearApplier.PreviewPreset = null;
            }
        }

        private void DrawModList(Rect rect)
        {
            if (selectedPreset != null && selectedPreset.requiredMods.Any())
            {
                WidgetsUtils.DrawBox(rect);
                Rect innerRect = rect.ContractedBy(5f);
                
                Widgets.BeginScrollView(innerRect, ref modListScrollPos, new Rect(0, 0, innerRect.width - 16f, selectedPreset.requiredMods.Count * 24f));
                float y = 0;
                
                foreach (var mod in selectedPreset.requiredMods)
                {
                    bool isActive = LoadedModManager.RunningMods.Any(m => m.Name == mod);
                    GUI.color = isActive ? Color.green : Color.red;
                    
                    Rect labelRect = new Rect(0, y, innerRect.width, 24f);
                    Widgets.Label(labelRect, mod + (isActive ? "" : " " + LanguageManager.Get("Missing")));
                    
                    if (!isActive)
                    {
                        TooltipHandler.TipRegion(labelRect, LanguageManager.Get("ModNotActiveTooltip"));
                    }
                    
                    y += 24f;
                }
                GUI.color = Color.white;
                
                Widgets.EndScrollView();
            }
            else
            {
                Widgets.Label(rect, LanguageManager.Get("NoRequiredMods"));
            }
        }

        private string DrawTextFieldWithPlaceholder(Rect rect, string text, string placeholder, bool isMultiLine = false)
        {
            string result;
            if (isMultiLine)
            {
                result = Widgets.TextArea(rect, text);
            }
            else
            {
                result = Widgets.TextField(rect, text);
            }

            if (string.IsNullOrEmpty(result))
            {
                var anchor = Text.Anchor;
                var color = GUI.color;
                Text.Anchor = isMultiLine ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                Widgets.Label(new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height), placeholder);
                GUI.color = color;
                Text.Anchor = anchor;
            }
            return result;
        }

        private static void Notify(string text)
        {
            if (text == null) text = "";

            try
            {
                if (Current.ProgramState == ProgramState.Playing)
                {
                    Messages.Message(text, MessageTypeDefOf.NeutralEvent, false);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[FactionGearCustomizer] Notify failed: " + ex);
            }

            try
            {
                Find.WindowStack?.Add(new Dialog_MessageBox(text));
            }
            catch (Exception ex)
            {
                Log.Warning("[FactionGearCustomizer] Notify fallback failed: " + ex);
            }
        }
        public override void PreClose()
        {
            base.PreClose();
            if (previewPawn != null && !previewPawn.Destroyed)
            {
                previewPawn.Destroy();
                previewPawn = null;
            }
        }
    }
}
