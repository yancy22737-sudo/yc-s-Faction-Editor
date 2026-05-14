using RimWorld;
using Verse;

namespace FactionGearCustomizer.Managers
{
    public static class SkillApplicationService
    {
        public static void ApplySkills(Pawn pawn, KindGearData kindData)
        {
            if (pawn?.skills == null || kindData == null) return;
            if (kindData.ForcedSkills == null || kindData.ForcedSkills.Count == 0) return;

            Log.Message($"[FGC][Skills] Applying skills to pawn={pawn.LabelShortCap}, kind={pawn.kindDef?.defName ?? "null"}, configured={kindData.ForcedSkills.Count}, globalRange={kindData.SkillRandomRange}");

            foreach (var fs in kindData.ForcedSkills)
            {
                bool isDefault =
                    fs.level == 0 &&
                    fs.minLevel == 0 &&
                    fs.maxLevel == 20 &&
                    fs.passion == Passion.None &&
                    System.Math.Abs(fs.chance - 1f) < 0.001f;
                if (isDefault)
                {
                    Log.Message($"[FGC][Skills] Skip {fs.skillDefName ?? "null"}: default settings");
                    continue;
                }

                SkillDef def = fs.SkillDef;
                if (def == null && !string.IsNullOrEmpty(fs.skillDefName))
                    def = DefDatabase<SkillDef>.GetNamedSilentFail(fs.skillDefName);
                if (def == null)
                {
                    Log.Warning($"[FGC][Skills] Skip {fs.skillDefName ?? "null"}: SkillDef unresolved");
                    continue;
                }
                if (!Rand.Chance(fs.chance))
                {
                    Log.Message($"[FGC][Skills] Skip {def.defName}: chance roll failed");
                    continue;
                }

                var skillRecord = pawn.skills.GetSkill(def);
                if (skillRecord == null)
                {
                    Log.Warning($"[FGC][Skills] Skip {def.defName}: pawn has no SkillRecord");
                    continue;
                }

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

                Log.Message($"[FGC][Skills] Applied {def.defName}: finalLevel={skillRecord.Level}, finalPassion={skillRecord.passion}");
            }
        }
    }
}
