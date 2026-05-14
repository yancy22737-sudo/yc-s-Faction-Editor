using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class ForcedSkill : IExposable
    {
        public string skillDefName;
        public int level = 10;
        public int minLevel = 0;
        public int maxLevel = 20;
        public float chance = 1f;
        public Passion passion = Passion.None;

        [Unsaved]
        private SkillDef cachedSkillDef;

        public SkillDef SkillDef
        {
            get
            {
                if (cachedSkillDef == null && !string.IsNullOrEmpty(skillDefName))
                    cachedSkillDef = DefDatabase<SkillDef>.GetNamedSilentFail(skillDefName);
                return cachedSkillDef;
            }
            set
            {
                cachedSkillDef = value;
                skillDefName = value?.defName;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref skillDefName, "skillDef");
            Scribe_Values.Look(ref level, "level", 10);
            Scribe_Values.Look(ref minLevel, "minLevel", 0);
            Scribe_Values.Look(ref maxLevel, "maxLevel", 20);
            Scribe_Values.Look(ref chance, "chance", 1f);
            Scribe_Values.Look(ref passion, "passion", Passion.None);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (minLevel > maxLevel) { int t = minLevel; minLevel = maxLevel; maxLevel = t; }
                ResolveReferences();
            }
        }

        public void ResolveReferences()
        {
            if (!string.IsNullOrEmpty(skillDefName))
                cachedSkillDef = DefDatabase<SkillDef>.GetNamedSilentFail(skillDefName);
        }
    }
}
