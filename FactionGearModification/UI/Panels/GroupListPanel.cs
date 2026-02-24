using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;
using FactionGearCustomizer;
using FactionGearCustomizer.UI.Dialogs;

namespace FactionGearCustomizer.UI.Panels
{
    public static class GroupListPanel
    {
        public static float GetViewHeight(List<PawnGroupMakerData> groups)
        {
            float headerHeight = 30f;
            float listY = headerHeight + 10f;
            const float rowHeight = 58f;
            float listHeight = (groups?.Count ?? 0) * rowHeight;
            return listY + listHeight + 20f; // Add padding
        }

        public static void Draw(Rect rect, ref List<PawnGroupMakerData> groups, FactionDef factionDef)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);
            
            if (groups == null)
            {
                groups = new List<PawnGroupMakerData>();
            }
            List<PawnGroupMakerData> groupList = groups;

            // Header
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
            Widgets.Label(headerRect, $"{LanguageManager.Get("PawnGroups")} ({groups.Count})");
            
            // Help Button
            Rect helpRect = new Rect(headerRect.x + headerRect.width - 120f - 30f, headerRect.y, 24f, 24f);
            if (Widgets.ButtonText(helpRect, "?"))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    LanguageManager.Get("PawnGroupGuideContent"),
                    LanguageManager.Get("Close"),
                    null,
                    null,
                    null,
                    LanguageManager.Get("PawnGroupGuideTitle"),
                    false,
                    null,
                    null
                ));
            }

            // Add Button
            if (Widgets.ButtonText(new Rect(headerRect.x + headerRect.width - 120f, headerRect.y, 120f, 24f), LanguageManager.Get("AddGroup")))
            {
                List<PawnGroupKindDef> allKinds = DefDatabase<PawnGroupKindDef>.AllDefsListForReading?.ToList();
                if (allKinds == null || allKinds.Count == 0)
                {
                    Log.Warning("[FGM] No PawnGroupKindDef found in DefDatabase");
                    return;
                }

                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var kind in allKinds)
                {
                    string kindDefName = kind.defName;
                    string kindLabel = GetTranslatedKindLabel(kind);
                    options.Add(new FloatMenuOption(kindLabel, () =>
                    {
                        var newGroup = new PawnGroupMakerData();
                        newGroup.kindDefName = kindDefName;
                        newGroup.customLabel = GenerateDefaultGroupLabel(kind, groupList);
                        groupList.Add(newGroup);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // List
            float listY = headerRect.yMax + 10f;
            const float rowHeight = 58f;
            
            float y = listY;
            for (int i = 0; i < (groups?.Count ?? 0); i++)
            {
                var group = groups[i];
                Rect row = new Rect(innerRect.x, y, innerRect.width, rowHeight - 4f);
                if (i % 2 == 1) Widgets.DrawAltRect(row);

                PawnGroupKindDef kind = DefDatabase<PawnGroupKindDef>.GetNamedSilentFail(group.kindDefName);
                EnsureGroupHasLabel(group, kind, groups, i);

                float btnW = 55f;
                float btnGap = 5f;
                float btnAreaWidth = btnW * 3f + btnGap * 2f;
                float contentWidth = row.width - btnAreaWidth - 10f;
                Rect nameRect = new Rect(row.x + 5f, row.y + 4f, contentWidth, 24f);

                string controlName = $"FGM_GroupName_{i}";
                GUI.SetNextControlName(controlName);
                string newLabel = Widgets.TextField(nameRect, group.customLabel ?? "");
                if (newLabel != group.customLabel)
                {
                    group.customLabel = InputValidator.SanitizeName(newLabel);
                }

                bool hasFocus = GUI.GetNameOfFocusedControl() == controlName;
                if (!hasFocus && string.IsNullOrWhiteSpace(group.customLabel))
                    group.customLabel = GenerateDefaultGroupLabel(kind, groups);
                
                // Commonality
                Widgets.Label(new Rect(row.x + 5f, row.y + 31f, 80f, 20f), LanguageManager.Get("Commonality") + ":");
                string buffer = group.commonality.ToString();
                Widgets.TextFieldNumeric(new Rect(row.x + 90f, row.y + 31f, 60f, 20f), ref group.commonality, ref buffer);

                string kindText = GetKindDisplayText(kind, group?.kindDefName);
                if (!string.IsNullOrWhiteSpace(kindText))
                {
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(new Rect(row.x + 160f, row.y + 33f, contentWidth - 165f, 18f), kindText);
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }

                // Buttons
                float btnX = row.x + row.width - btnAreaWidth;
                if (Widgets.ButtonText(new Rect(btnX, row.y + 8f, btnW, 30f), LanguageManager.Get("Preview")))
                {
                    Find.WindowStack.Add(new Dialog_PawnGroupGenerationPreview(group, factionDef));
                }

                if (Widgets.ButtonText(new Rect(btnX + btnW + btnGap, row.y + 8f, btnW, 30f), LanguageManager.Get("Edit")))
                {
                    Find.WindowStack.Add(new Dialog_EditPawnGroup(group, factionDef));
                }
                
                if (Widgets.ButtonText(new Rect(btnX + (btnW + btnGap) * 2f, row.y + 8f, btnW, 30f), LanguageManager.Get("Remove")))
                {
                    if (FactionGearCustomizerMod.Settings.suppressDeleteGroupConfirmation)
                    {
                        groups.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        var groupToRemove = group;
                        Find.WindowStack.Add(new Dialog_ConfirmDeleteGroup(
                            LanguageManager.Get("ConfirmDeleteGroup"),
                            () => { 
                                if (groupToRemove != null) groupList.Remove(groupToRemove);
                            },
                            LanguageManager.Get("DeleteGroup")
                        ));
                    }
                }

                y += rowHeight;
            }
        }

        private static string GetGroupDisplayLabel(PawnGroupMakerData group, PawnGroupKindDef kind)
        {
            if (!string.IsNullOrWhiteSpace(group?.customLabel))
                return group.customLabel;

            if (kind != null)
            {
                string kindLabel = kind.LabelCap.ToString();
                if (LooksMissingTranslation(kindLabel))
                    kindLabel = kind.defName;
                return kindLabel;
            }

            return group?.kindDefName ?? LanguageManager.Get("Group");
        }

        private static void EnsureGroupHasLabel(PawnGroupMakerData group, PawnGroupKindDef kind, List<PawnGroupMakerData> groups, int listIndex)
        {
            if (group == null)
                return;

            string controlName = $"FGM_GroupName_{listIndex}";
            if (GUI.GetNameOfFocusedControl() == controlName)
                return;

            if (!string.IsNullOrWhiteSpace(group.customLabel))
                return;

            group.customLabel = GenerateDefaultGroupLabel(kind, groups);
        }

        public static string GetTranslatedKindLabel(PawnGroupKindDef kind)
        {
            if (kind == null) return null;

            string translationKey = "PawnGroupKind_" + kind.defName;
            string translated = LanguageManager.Get(translationKey);
            if (!string.IsNullOrEmpty(translated) && translated != translationKey)
                return translated;

            string label = kind.LabelCap.ToString();
            if (LooksMissingTranslation(label))
                return kind.defName;

            return label;
        }

        private static string GenerateDefaultGroupLabel(PawnGroupKindDef kind, List<PawnGroupMakerData> groups)
        {
            string baseLabel = GetTranslatedKindLabel(kind);
            if (string.IsNullOrWhiteSpace(baseLabel))
                baseLabel = LanguageManager.Get("Group");

            int index = 1;
            while (index < 1000)
            {
                string candidate = LanguageManager.Get("GroupDefaultName", baseLabel, index);
                if (string.IsNullOrWhiteSpace(candidate) || string.Equals(candidate, "GroupDefaultName", StringComparison.OrdinalIgnoreCase))
                    candidate = $"{baseLabel} #{index}";
                if (groups == null || !groups.Any(g => string.Equals(g?.customLabel, candidate, StringComparison.OrdinalIgnoreCase)))
                    return candidate;
                index++;
            }
            return $"{baseLabel} {System.DateTime.Now.Ticks}";
        }

        private static string GetKindDisplayText(PawnGroupKindDef kind, string kindDefName)
        {
            if (kind != null)
            {
                string kindLabel = GetTranslatedKindLabel(kind);
                return $"{kindLabel} ({kind.defName})";
            }

            if (!string.IsNullOrWhiteSpace(kindDefName))
                return kindDefName;

            return null;
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
