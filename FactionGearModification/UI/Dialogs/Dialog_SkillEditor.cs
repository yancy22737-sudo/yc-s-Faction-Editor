using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Dialogs
{
    public class Dialog_SkillEditor : Window
    {
        private readonly ForcedSkill skill;

        public override Vector2 InitialSize => new Vector2(420f, 300f);

        public Dialog_SkillEditor(ForcedSkill skill)
        {
            this.skill = skill ?? new ForcedSkill();
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            draggable = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                float y = 0f;
                string skillName = "???";
                if (skill.SkillDef != null) skillName = skill.SkillDef.LabelCap;
                else if (!string.IsNullOrEmpty(skill.skillDefName)) skillName = skill.skillDefName;

                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, y, inRect.width, 35f), skillName);
                Text.Font = GameFont.Small;
                y += 36f;

                // Base level slider
                Widgets.Label(new Rect(0f, y, 100f, 28f), LanguageManager.Get("BaseLevel") + ":");
                int newLevel = Mathf.RoundToInt(Widgets.HorizontalSlider(
                    new Rect(105f, y, inRect.width - 150f, 28f), skill.level, 0f, 20f, false, null, "0", "20"));
                Widgets.Label(new Rect(inRect.width - 40f, y, 40f, 28f), skill.level.ToString());
                if (newLevel != skill.level)
                {
                    skill.level = newLevel;
                    FactionGearEditor.MarkDirty();
                }
                y += 34f;

                // Random range header
                Widgets.Label(new Rect(0f, y, inRect.width, 24f), "<b>" + LanguageManager.Get("RandomRange") + ":</b>");
                y += 28f;

                // Min
                Widgets.Label(new Rect(0f, y, 80f, 28f), LanguageManager.Get("SkillMin") + ":");
                int newMin = Mathf.RoundToInt(Widgets.HorizontalSlider(
                    new Rect(85f, y, inRect.width - 130f, 28f), skill.minLevel, 0f, 20f, false, null, "0", "20"));
                Widgets.Label(new Rect(inRect.width - 40f, y, 40f, 28f), skill.minLevel.ToString());
                if (newMin != skill.minLevel)
                {
                    skill.minLevel = Mathf.Min(newMin, skill.maxLevel);
                    FactionGearEditor.MarkDirty();
                }
                y += 32f;

                // Max
                Widgets.Label(new Rect(0f, y, 80f, 28f), LanguageManager.Get("SkillMax") + ":");
                int newMax = Mathf.RoundToInt(Widgets.HorizontalSlider(
                    new Rect(85f, y, inRect.width - 130f, 28f), skill.maxLevel, 0f, 20f, false, null, "0", "20"));
                Widgets.Label(new Rect(inRect.width - 40f, y, 40f, 28f), skill.maxLevel.ToString());
                if (newMax != skill.maxLevel)
                {
                    skill.maxLevel = Mathf.Max(newMax, skill.minLevel);
                    FactionGearEditor.MarkDirty();
                }
                y += 36f;

                // Passion buttons
                Widgets.Label(new Rect(0f, y, 80f, 28f), LanguageManager.Get("Passion") + ":");
                string[] pNames = { "None", "Minor", "Major" };
                for (int i = 0; i < 3; i++)
                {
                    Passion p = (Passion)i;
                    bool isActive = skill.passion == p;
                    Rect btnR = new Rect(85f + i * 75f, y, 70f, 28f);
                    if (Widgets.ButtonText(btnR, pNames[i], true, true, true))
                    {
                        skill.passion = p;
                        FactionGearEditor.MarkDirty();
                    }
                    if (isActive) Widgets.DrawHighlight(btnR);
                }
                y += 36f;

                // Chance slider
                Widgets.Label(new Rect(0f, y, 80f, 28f), LanguageManager.Get("Chance") + ":");
                float newChance = Widgets.HorizontalSlider(
                    new Rect(85f, y, inRect.width - 130f, 28f), skill.chance, 0f, 1f, false, null, "0%", "100%");
                Widgets.Label(new Rect(inRect.width - 40f, y, 40f, 28f), (skill.chance * 100f).ToString("F0") + "%");
                if (System.Math.Abs(newChance - skill.chance) > 0.001f)
                {
                    skill.chance = newChance;
                    FactionGearEditor.MarkDirty();
                }

                Rect closeBtn = new Rect(inRect.width / 2f - 60f, inRect.height - 35f, 120f, 30f);
                if (Widgets.ButtonText(closeBtn, LanguageManager.Get("Close")))
                    Close();
            }
            catch (System.Exception ex)
            {
                Log.Error("[FactionGearCustomizer] Dialog_SkillEditor error: " + ex);
            }
        }
    }
}
