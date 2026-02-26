using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.UI.Panels;
using FactionGearCustomizer;

namespace FactionGearCustomizer.UI
{
    public class Dialog_EditPawnGroup : Window
    {
        private PawnGroupMakerData groupData;
        private FactionDef factionDef;
        private Vector2 mainScrollPos;
        private Vector2 optionsScrollPos;
        private Vector2 tradersScrollPos;
        private Vector2 carriersScrollPos;
        private Vector2 guardsScrollPos;
        private static System.Reflection.FieldInfo defaultFactionTypeField;

        public override Vector2 InitialSize => new Vector2(800f, 700f);

        public Dialog_EditPawnGroup(PawnGroupMakerData data, FactionDef faction)
        {
            this.groupData = data;
            this.factionDef = faction;
            this.doCloseX = true;
            this.forcePause = true;
            this.closeOnClickedOutside = false;

            if (defaultFactionTypeField == null)
            {
                defaultFactionTypeField = typeof(PawnKindDef).GetField("defaultFactionType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Handle Keyboard Shortcuts
            if (Event.current.type == EventType.KeyDown && Event.current.control)
            {
                if (Event.current.keyCode == KeyCode.Z)
                {
                    UndoManager.Undo();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Y)
                {
                    UndoManager.Redo();
                    Event.current.Use();
                }
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width - 100f, 35f), LanguageManager.Get("EditGroup") + ": " + GetGroupDisplayLabel());
            
            // Undo/Redo Buttons
            Rect undoRect = new Rect(inRect.width - 60f, 0f, 24f, 24f);
            Rect redoRect = new Rect(inRect.width - 30f, 0f, 24f, 24f);
            
            if (Widgets.ButtonText(undoRect, "<")) UndoManager.Undo();
            TooltipHandler.TipRegion(undoRect, LanguageManager.Get("UndoTooltip"));

            if (Widgets.ButtonText(redoRect, ">")) UndoManager.Redo();
            TooltipHandler.TipRegion(redoRect, LanguageManager.Get("RedoTooltip"));

            Text.Font = GameFont.Small;

            Rect contentRect = new Rect(0, 40f, inRect.width, inRect.height - 80f);
            Widgets.DrawMenuSection(contentRect);
            Rect innerRect = contentRect.ContractedBy(10f);

            float viewHeight = CalculateTotalViewHeight(innerRect);
            Rect viewRect = new Rect(0, 0, innerRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(innerRect, ref mainScrollPos, viewRect);

            float curY = 0f;
            float sectionWidth = innerRect.width - 16f;

            DrawGeneralSettings(new Rect(0, curY, sectionWidth, 120f));
            curY += 130f;

            curY = DrawPawnListSection(new Rect(0, curY, sectionWidth, 0), groupData.options, "GroupOptions", ref optionsScrollPos, curY);

            curY = DrawPawnListSection(new Rect(0, curY, sectionWidth, 0), groupData.traders, "Traders", ref tradersScrollPos, curY);

            curY = DrawPawnListSection(new Rect(0, curY, sectionWidth, 0), groupData.carriers, "Carriers", ref carriersScrollPos, curY);

            curY = DrawPawnListSection(new Rect(0, curY, sectionWidth, 0), groupData.guards, "Guards", ref guardsScrollPos, curY);

            Widgets.EndScrollView();

            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 60f, inRect.height - 40f, 120f, 35f), LanguageManager.Get("Close")))
            {
                Close();
            }
        }

        private float CalculateTotalViewHeight(Rect rect)
        {
            float height = 130f;

            height += CalculatePawnListHeight(groupData.options);
            height += 10f;

            height += CalculatePawnListHeight(groupData.traders);
            height += 10f;

            height += CalculatePawnListHeight(groupData.carriers);
            height += 10f;

            height += CalculatePawnListHeight(groupData.guards);
            height += 10f;

            return height;
        }

        private float CalculatePawnListHeight(List<PawnGenOptionData> list)
        {
            float headerHeight = 40f;
            float listItemHeight = 28f;
            float contentHeight = headerHeight + Math.Max(list.Count * listItemHeight, 80f);
            return contentHeight;
        }

        private void DrawGeneralSettings(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            listing.Label("<b>" + LanguageManager.Get("GeneralSettings") + "</b>");
            listing.Gap(10f);

            Rect nameRect = listing.GetRect(24f);
            Widgets.Label(new Rect(nameRect.x, nameRect.y, 150f, 24f), LanguageManager.Get("GroupName") + ":");
            string newLabel = Widgets.TextField(new Rect(nameRect.x + 160f, nameRect.y, 300f, 24f), groupData.customLabel ?? "");
            if (newLabel != (groupData.customLabel ?? ""))
            {
                UndoManager.RecordState(groupData);
                groupData.customLabel = InputValidator.SanitizeName(newLabel);
            }
            listing.Gap(6f);

            Rect commRect = listing.GetRect(24f);
            Widgets.Label(new Rect(commRect.x, commRect.y, 150f, 24f), LanguageManager.Get("Commonality") + ":");
            string commBuffer = groupData.commonality.ToString();
            float newComm = groupData.commonality;
            Widgets.TextFieldNumeric(new Rect(commRect.x + 160f, commRect.y, 100f, 24f), ref newComm, ref commBuffer);
            if (newComm != groupData.commonality)
            {
                UndoManager.RecordState(groupData);
                groupData.commonality = newComm;
            }
            listing.Gap(6f);

            Rect pointsRect = listing.GetRect(24f);
            Widgets.Label(new Rect(pointsRect.x, pointsRect.y, 150f, 24f), LanguageManager.Get("MaxTotalPoints") + ":");
            string pointsBuffer = groupData.maxTotalPoints.ToString();
            float newPoints = groupData.maxTotalPoints;
            Widgets.TextFieldNumeric(new Rect(pointsRect.x + 160f, pointsRect.y, 100f, 24f), ref newPoints, ref pointsBuffer);
            if (newPoints != groupData.maxTotalPoints)
            {
                UndoManager.RecordState(groupData);
                groupData.maxTotalPoints = newPoints;
            }

            listing.End();
        }

        private float DrawPawnListSection(Rect rect, List<PawnGenOptionData> list, string labelKey, ref Vector2 listScrollPos, float curY)
        {
            float height = CalculatePawnListHeight(list);
            Rect sectionRect = new Rect(rect.x, curY, rect.width, height);

            Rect headerRect = new Rect(sectionRect.x + 5f, sectionRect.y, sectionRect.width - 10f, 30f);
            Widgets.Label(headerRect, "<b>" + LanguageManager.Get(labelKey) + "</b>");

            if (Widgets.ButtonText(new Rect(headerRect.xMax - 125f, headerRect.y, 120f, 24f), LanguageManager.Get("AddPawn")))
            {
                Find.WindowStack.Add(new Dialog_PawnKindPicker((kinds) => {
                    UndoManager.RecordState(groupData);
                    foreach (var kind in kinds)
                    {
                        list.Add(new PawnGenOptionData { kindDefName = kind.defName, selectionWeight = 10f });
                    }
                }, factionDef));
            }

            Rect listOutRect = new Rect(sectionRect.x + 5f, sectionRect.y + 35f, sectionRect.width - 10f, sectionRect.height - 40f);
            float listHeight = list.Count * 28f;
            Rect viewRect = new Rect(0, 0, listOutRect.width - 16f, listHeight);

            Widgets.BeginScrollView(listOutRect, ref listScrollPos, viewRect);

            float y = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                var opt = list[i];
                Rect row = new Rect(0, y, viewRect.width, 24f);

                if (i % 2 == 1) Widgets.DrawAltRect(row);
                if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);

                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(opt.kindDefName);
                string kindLabel = kind?.LabelCap ?? opt.kindDefName;
                if (kind != null)
                {
                     kindLabel += $" ({kind.combatPower})";
                }

                Widgets.Label(new Rect(row.x + 5f, row.y, 250f, 24f), kindLabel);

                string factionLabel = GetFactionLabelForKind(kind);
                Widgets.Label(new Rect(row.x + 260f, row.y, 140f, 24f), factionLabel);

                Widgets.Label(new Rect(row.x + 410f, row.y, 60f, 24f), LanguageManager.Get("Weight") + ":");
                string buffer = opt.selectionWeight.ToString();
                float newWeight = opt.selectionWeight;
                Widgets.TextFieldNumeric(new Rect(row.x + 470f, row.y, 60f, 24f), ref newWeight, ref buffer);
                if (Math.Abs(newWeight - opt.selectionWeight) > 0.001f)
                {
                    UndoManager.RecordState(groupData);
                    opt.selectionWeight = newWeight;
                }

                if (Widgets.ButtonText(new Rect(row.x + 540f, row.y, 50f, 24f), LanguageManager.Get("Delete")))
                {
                    UndoManager.RecordState(groupData);
                    list.RemoveAt(i);
                    i--;
                }

                y += 28f;
            }

            Widgets.EndScrollView();

            return curY + height;
        }

        private string GetGroupDisplayLabel()
        {
            if (!string.IsNullOrWhiteSpace(groupData?.customLabel))
                return groupData.customLabel;

            PawnGroupKindDef kind = DefDatabase<PawnGroupKindDef>.GetNamedSilentFail(groupData?.kindDefName);
            if (kind != null)
            {
                return GroupListPanel.GetTranslatedKindLabel(kind);
            }

            return groupData?.kindDefName ?? LanguageManager.Get("Group");
        }

        private string GetFactionLabelForKind(PawnKindDef kind)
        {
            if (kind == null) return "-";

            // 1. 尝试从 defaultFactionType 字段获取
            if (defaultFactionTypeField != null)
            {
                var faction = defaultFactionTypeField.GetValue(kind) as FactionDef;
                if (faction != null) return faction.LabelCap;
            }

            // 2. 如果当前有指定派系，检查兵种是否属于该派系
            if (factionDef != null)
            {
                var factionKinds = FactionGearEditor.GetFactionKinds(factionDef);
                if (factionKinds != null && factionKinds.Any(k => k.defName == kind.defName))
                {
                    return factionDef.LabelCap;
                }
            }

            // 3. 尝试从所有派系中查找该兵种所属的派系
            foreach (var faction in DefDatabase<FactionDef>.AllDefsListForReading)
            {
                if (faction.pawnGroupMakers != null)
                {
                    foreach (var pgm in faction.pawnGroupMakers)
                    {
                        // 检查 options
                        if (pgm.options != null)
                        {
                            foreach (var opt in pgm.options)
                            {
                                if (opt.kind != null && opt.kind.defName == kind.defName)
                                {
                                    return faction.LabelCap;
                                }
                            }
                        }
                        // 检查 traders
                        if (pgm.traders != null)
                        {
                            foreach (var opt in pgm.traders)
                            {
                                if (opt.kind != null && opt.kind.defName == kind.defName)
                                {
                                    return faction.LabelCap;
                                }
                            }
                        }
                        // 检查 carriers
                        if (pgm.carriers != null)
                        {
                            foreach (var opt in pgm.carriers)
                            {
                                if (opt.kind != null && opt.kind.defName == kind.defName)
                                {
                                    return faction.LabelCap;
                                }
                            }
                        }
                        // 检查 guards
                        if (pgm.guards != null)
                        {
                            foreach (var opt in pgm.guards)
                            {
                                if (opt.kind != null && opt.kind.defName == kind.defName)
                                {
                                    return faction.LabelCap;
                                }
                            }
                        }
                    }
                }
            }

            return "-";
        }

        private static bool LooksMissingTranslation(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return true;

            if (label.IndexOf("missing translation", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (label.IndexOf("missing label", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (label.Contains("缺少翻译") || label.Contains("未翻译"))
                return true;

            return false;
        }
    }
}
