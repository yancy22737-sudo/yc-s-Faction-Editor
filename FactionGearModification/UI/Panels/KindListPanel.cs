using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;
using FactionGearCustomizer.UI;

namespace FactionGearCustomizer.UI.Panels
{
    public static class KindListPanel
    {
        private static Dictionary<string, List<PawnKindDef>> cachedFactionKinds = new Dictionary<string, List<PawnKindDef>>();

        public static void ClearCache()
        {
            cachedFactionKinds.Clear();
        }

        public static void Draw(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            // Title "Kind Defs" and Add Button
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(innerRect.x, innerRect.y, 150f, 30f), LanguageManager.Get("KindDefs"));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Add PawnKind Button - 设置成和派系添加按钮一样大小位置
            bool inGame = Current.Game != null;
            bool hasSelectedFaction = !string.IsNullOrEmpty(EditorSession.SelectedFactionDefName);
            string addLabel = LanguageManager.Get("AddPawnKind");
            Vector2 addSize = Text.CalcSize(addLabel);
            float btnWidth = Mathf.Max(addSize.x + 10f, 40f);
            Rect addBtnRect = new Rect(innerRect.xMax - btnWidth, innerRect.y + 3f, btnWidth, 24f);

            if (!inGame || !hasSelectedFaction)
            {
                GUI.color = Color.gray;
            }

            if (Widgets.ButtonText(addBtnRect, LanguageManager.Get("AddPawnKind")))
            {
                if (inGame && hasSelectedFaction)
                {
                    OpenAddPawnKindDialog();
                }
            }

            if (!inGame)
            {
                TooltipHandler.TipRegion(addBtnRect, LanguageManager.Get("OnlyAvailableInGame"));
                GUI.color = Color.white;
            }
            else if (!hasSelectedFaction)
            {
                TooltipHandler.TipRegion(addBtnRect, LanguageManager.Get("SelectFactionFirst"));
                GUI.color = Color.white;
            }
            else
            {
                TooltipHandler.TipRegion(addBtnRect, LanguageManager.Get("AddPawnKindTooltip"));
            }

            // List area calculation
            float listStartY = innerRect.y + 32f;
            float kindListHeight = innerRect.height - (listStartY - innerRect.y);

            Rect kindListOutRect = new Rect(innerRect.x, listStartY, innerRect.width, kindListHeight);

            // Get Kinds to draw
            List<PawnKindDef> kindDefsToDraw = GetKindsToDraw();

            Rect kindListViewRect = new Rect(0, 0, kindListOutRect.width - 16f, kindDefsToDraw.Count * 32f);
            
            // Scroll handling is done by BeginScrollView automatically


            Widgets.BeginScrollView(kindListOutRect, ref EditorSession.KindListScrollPos, kindListViewRect);
            
            float kindY = 0;
            foreach (var kindDef in kindDefsToDraw)
            {
                DrawKindRow(kindDef, kindY, kindListViewRect.width);
                kindY += 32f;
            }
            
            Widgets.EndScrollView();
        }

        private static List<PawnKindDef> GetKindsToDraw()
        {
            if (string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
            {
                return new List<PawnKindDef>();
            }

            List<PawnKindDef> fullList;
            if (!cachedFactionKinds.TryGetValue(EditorSession.SelectedFactionDefName, out fullList))
            {
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                fullList = FactionGearEditor.GetFactionKinds(factionDef);
                
                // 合并用户新增的兵种（来自设置数据）
                var factionData = FactionGearCustomizerMod.Settings.factionGearData.FirstOrDefault(f => f.factionDefName == EditorSession.SelectedFactionDefName);
                if (factionData != null && factionData.kindGearData != null)
                {
                    var existingKindNames = new HashSet<string>(fullList.Select(k => k.defName));
                    foreach (var kindData in factionData.kindGearData)
                    {
                        if (!existingKindNames.Contains(kindData.kindDefName))
                        {
                            var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
                            if (kindDef != null)
                            {
                                fullList.Add(kindDef);
                                existingKindNames.Add(kindData.kindDefName);
                            }
                        }
                    }
                    // 重新排序
                    fullList.Sort((a, b) => (a.label ?? a.defName).CompareTo(b.label ?? b.defName));
                }
                
                cachedFactionKinds[EditorSession.SelectedFactionDefName] = fullList;
            }

            return fullList;
        }

        private static void DrawKindRow(PawnKindDef kindDef, float y, float width)
        {
            Rect rowRect = new Rect(0, y, width, 32f);
            
            // Highlight
            if (EditorSession.SelectedKindDefName == kindDef.defName)
            {
                Widgets.DrawHighlightSelected(rowRect);
            }
            else if (Mouse.IsOver(rowRect))
            {
                Widgets.DrawHighlight(rowRect);
            }

            // Selection Click
            // Exclude Copy/Paste buttons area (48px on the right) to avoid click conflict
            Rect selectionRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 48f, rowRect.height);
            if (Widgets.ButtonInvisible(selectionRect))
            {
                if (EditorSession.SelectedKindDefName != kindDef.defName)
                {
                    EditorSession.SelectedKindDefName = kindDef.defName;
                    EditorSession.GearListScrollPos = Vector2.zero; // Reset gear list scroll
                }
            }

            // Check Modification Status
            bool isModified = false;
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.factionGearData.FirstOrDefault(f => f.factionDefName == EditorSession.SelectedFactionDefName);
                if (factionData != null)
                {
                    var kindData = factionData.kindGearData.FirstOrDefault(k => k.kindDefName == kindDef.defName);
                    if (kindData != null)
                    {
                        isModified = kindData.isModified;
                    }
                }
            }

            // Draw Label
            Rect labelRect = new Rect(rowRect.x + 6f, rowRect.y, rowRect.width - 60f, rowRect.height); // Reserve space for buttons
            string labelText = kindDef.label != null ? kindDef.LabelCap.ToString() : kindDef.defName;

            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.factionGearData.FirstOrDefault(f => f.factionDefName == EditorSession.SelectedFactionDefName);
                if (factionData != null)
                {
                    var kindData = factionData.kindGearData.FirstOrDefault(k => k.kindDefName == kindDef.defName);
                    if (kindData != null && !string.IsNullOrEmpty(kindData.Label))
                    {
                        labelText = kindData.Label;
                    }
                }
            }
            
            if (isModified)
            {
                GUI.color = Color.yellow;
                Widgets.Label(new Rect(labelRect.x + 20f, labelRect.y + 6f, labelRect.width - 20f, 20f), labelText);
                
                // Modification Icon
                Texture2D modTex = TexCache.ApplyTex ?? Widgets.CheckboxOnTex; // Use fallback
                WidgetsUtils.DrawTextureFitted(new Rect(labelRect.x, labelRect.y + 8f, 16f, 16f), modTex, 1f);
                
                GUI.color = Color.white;
            }
            else
            {
                Widgets.Label(new Rect(labelRect.x, labelRect.y + 6f, labelRect.width, 20f), labelText);
            }

            // Copy/Paste Buttons
            float btnSize = 20f;
            float btnSpacing = 4f;
            float btnX = rowRect.width - (btnSize * 2 + btnSpacing + 4f); // Align to right
            float btnY = rowRect.y + (rowRect.height - btnSize) / 2f;

            // Copy
            Texture2D kindCopyTex = TexCache.CopyTex ?? Widgets.CheckboxOnTex;
            Rect copyBtnRect = new Rect(btnX, btnY, btnSize, btnSize);
            if (Widgets.ButtonImage(copyBtnRect, kindCopyTex))
            {
                if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(kindDef.defName))
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                    var kindData = factionData.GetOrCreateKindData(kindDef.defName);
                    EditorSession.CopiedKindGearData = kindData.DeepCopy();
                    Messages.Message("Copied gear from " + labelText, MessageTypeDefOf.NeutralEvent, false);
                }
            }
            TooltipHandler.TipRegion(copyBtnRect, "Copy this KindDef's gear");

            // Paste
            btnX += btnSize + btnSpacing;
            Texture2D kindPasteTex = TexCache.PasteTex ?? Widgets.CheckboxOnTex;
            Rect pasteBtnRect = new Rect(btnX, btnY, btnSize, btnSize);

            if (EditorSession.CopiedKindGearData != null)
            {
                if (Widgets.ButtonImage(pasteBtnRect, kindPasteTex))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(kindDef.defName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var targetKindData = factionData.GetOrCreateKindData(kindDef.defName);
                        if (targetKindData != null)
                        {
                            UndoManager.RecordState(targetKindData);
                            targetKindData.CopyFrom(EditorSession.CopiedKindGearData);
                            targetKindData.isModified = true;
                            FactionGearEditor.MarkDirty(); // Notify editor
                            Messages.Message("Pasted gear to " + labelText, MessageTypeDefOf.NeutralEvent, false);
                        }
                    }
                }
                TooltipHandler.TipRegion(pasteBtnRect, "Paste gear to this KindDef");
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);
                Widgets.ButtonImage(pasteBtnRect, kindPasteTex);
                GUI.color = Color.white;
            }
        }

        private static void OpenAddPawnKindDialog()
        {
            var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
            if (factionDef == null) return;

            // 获取当前派系已有的兵种
            var existingKinds = GetKindsToDraw();
            var existingKindNames = new HashSet<string>(existingKinds.Select(k => k.defName));

            // 打开兵种选择器，排除已有的兵种和非人类单位
            Find.WindowStack.Add(new Dialog_PawnKindPicker((selectedKinds) => {
                if (selectedKinds != null && selectedKinds.Count > 0)
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                    int addedCount = 0;
                    int skippedCount = 0;

                    foreach (var kind in selectedKinds)
                    {
                        // 禁止添加非人类单位
                        if (kind.RaceProps == null || !kind.RaceProps.Humanlike)
                        {
                            skippedCount++;
                            continue;
                        }
                        
                        if (!existingKindNames.Contains(kind.defName))
                        {
                            // 为该兵种创建设置数据
                            var kindData = factionData.GetOrCreateKindData(kind.defName);
                            kindData.isModified = true;
                            addedCount++;
                        }
                    }

                    if (addedCount > 0)
                    {
                        // 清除缓存以刷新列表
                        ClearCache();
                        FactionGearEditor.ClearFactionKindsCache();
                        FactionGearEditor.MarkDirty();
                        Messages.Message(LanguageManager.Get("AddedPawnKinds", addedCount), MessageTypeDefOf.PositiveEvent, false);
                    }
                    
                    if (skippedCount > 0)
                    {
                        Messages.Message(LanguageManager.Get("SkippedNonHumanKinds", skippedCount), MessageTypeDefOf.RejectInput, false);
                    }
                }
            }, factionDef, existingKindNames));
        }
    }
}
