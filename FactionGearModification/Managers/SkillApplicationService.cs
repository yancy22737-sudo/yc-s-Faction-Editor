using RimWorld;
using Verse;

namespace FactionGearCustomizer.Managers
{
    public static class SkillApplicationService
    {
        public static void ApplySkills(Pawn pawn, KindGearData kindData)
        {
            if (pawn?.skills == null || kindData == null) return;
            if (!kindData.ForceOverrideSkills) return;
            if (kindData.ForcedSkills == null) return;

            foreach (var fs in kindData.ForcedSkills)
            {
                if (fs.SkillDef == null) continue;
                if (!Rand.Chance(fs.chance)) continue;

                var skillRecord = pawn.skills.GetSkill(fs.SkillDef);
                if (skillRecord == null) continue;

                int baseLevel = fs.level;
                int range = kindData.SkillRandomRange;
                if (range > 0)
                    baseLevel = Rand.RangeInclusive(
                        System.Math.Max(0, fs.level - range),
                        System.Math.Min(20, fs.level + range));
                else if (fs.minLevel != fs.maxLevel)
                    baseLevel = Rand.RangeInclusive(fs.minLevel, fs.maxLevel);

                skillRecord.Level = baseLevel;

                if (fs.passion != Passion.None)
                    skillRecord.passion = fs.passion;
            }
        }
    }
}
