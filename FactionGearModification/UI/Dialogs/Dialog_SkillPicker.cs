using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_SkillPicker : Window
    {
        private readonly List<ForcedSkill> targetList;
        private readonly Action<SkillDef> onPickSingle;
        private readonly bool multiSelect;

        private List<SkillDef> allSkills = new List<SkillDef>();
        private HashSet<SkillDef> selected = new HashSet<SkillDef>();
        private HashSet<string> existingDefNames;
        private Vector2 scrollPos;
        private bool skipExisting = true;

        private const float RowHeight = 32f;

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public Dialog_SkillPicker(List<ForcedSkill> targetList)
        {
            this.targetList = targetList;
            multiSelect = true;
            InitCommon();
        }

        private void InitCommon()
        {
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            draggable = true;
            resizeable = true;

            allSkills = DefDatabase<SkillDef>.AllDefs
                .OrderBy(s => s.LabelCap.ToString() ?? s.defName)
                .ToList();

            if (multiSelect && targetList != null)
                existingDefNames = new HashSet<string>(targetList.Where(x => x?.SkillDef != null).Select(x => x.SkillDef.defName));
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 35f), LanguageManager.Get("SkillPickerTitle"));
            Text.Font = GameFont.Small;
            y += 40f;

            // Skip existing checkbox
            if (existingDefNames?.Count > 0)
            {
                Rect skipRect = new Rect(0f, y, inRect.width, 24f);
                Widgets.CheckboxLabeled(skipRect, LanguageManager.Get("SkipExistingItems"), ref skipExisting);
                y += 28f;
            }

            float bottomHeight = multiSelect ? 40f : 36f;
            Rect listRect = new Rect(0f, y, inRect.width, inRect.height - y - bottomHeight);
            DrawSkillList(listRect);

            if (multiSelect)
                DrawBottomMulti(inRect);
        }

        private void DrawSkillList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);

            float contentHeight = allSkills.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, inner.width - 16f, contentHeight);

            Widgets.BeginScrollView(inner, ref scrollPos, viewRect);
            float y = 0f;

            foreach (var skill in allSkills)
            {
                Rect row = new Rect(0f, y, viewRect.width, RowHeight);
                bool alreadyExists = existingDefNames?.Contains(skill.defName) == true;

                if (alreadyExists && skipExisting)
                {
                    GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    Widgets.Label(new Rect(row.x + 24f, row.y, row.width - 24f, RowHeight), $"{skill.LabelCap} ({LanguageManager.Get("AlreadyAdded")})");
                    GUI.color = Color.white;
                }
                else
                {
                    bool isSel = selected.Contains(skill);
                    Widgets.CheckboxLabeled(new Rect(row.x, row.y, row.width, RowHeight), skill.LabelCap, ref isSel, false);
                    if (isSel != selected.Contains(skill))
                    {
                        if (isSel) selected.Add(skill);
                        else selected.Remove(skill);
                    }
                }

                if (alreadyExists)
                {
                    GUI.color = new Color(0.4f, 0.7f, 0.4f);
                    Widgets.Label(new Rect(row.x, row.y + RowHeight - 2f, row.width, 2f), "");
                    GUI.color = Color.white;
                }

                y += RowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawBottomMulti(Rect inRect)
        {
            Rect bottomRect = new Rect(0f, inRect.height - 40f, inRect.width, 40f);
            GUI.BeginGroup(bottomRect);

            float btnW = (bottomRect.width - 20f) / 2f;
            if (Widgets.ButtonText(new Rect(0f, 4f, btnW, 32f), LanguageManager.Get("AddSelected")))
            {
                AddSelectedSkills();
            }

            if (Widgets.ButtonText(new Rect(btnW + 16f, 4f, btnW, 32f), LanguageManager.Get("Cancel")))
            {
                Close();
            }

            GUI.EndGroup();
        }

        private void AddSelectedSkills()
        {
            int added = 0;
            int skipped = 0;

            foreach (var def in selected)
            {
                if (existingDefNames?.Contains(def.defName) == true)
                {
                    skipped++;
                    continue;
                }

                targetList.Add(new ForcedSkill { SkillDef = def, level = 10, chance = 1f });
                added++;
            }

            if (added > 0) FactionGearEditor.MarkDirty();

            Messages.Message(LanguageManager.Get("SkillsAddedMessage", added, skipped), MessageTypeDefOf.PositiveEvent, false);
            selected.Clear();
        }
    }
}
