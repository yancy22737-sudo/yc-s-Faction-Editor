using RimWorld;
using Verse;

namespace FactionGearCustomizer.Managers
{
    public static class AppearanceApplicationService
    {
        public static void ApplyAppearance(Pawn pawn, KindGearData kindData)
        {
            if (pawn?.story == null || kindData == null) return;
            if (kindData.ForcedAppearance == null) return;

            var app = kindData.ForcedAppearance;
            if (!Rand.Chance(app.chance)) return;

            if (app.HairDef != null)
                pawn.story.hairDef = app.HairDef;

            if (app.BeardDef != null && pawn.style != null)
                pawn.style.beardDef = app.BeardDef;

            if (app.BodyTypeDef != null)
                pawn.story.bodyType = app.BodyTypeDef;

            if (app.HeadTypeDef != null)
                pawn.story.headType = app.HeadTypeDef;

            // Skin/hair color application: these are gene-controlled in Biotech,
            // but for non-Biotech or override cases we set them directly via reflection
            if (app.skinColor.HasValue)
            {
                try { pawn.story.GetType().GetProperty("skinColorOverride")?.SetValue(pawn.story, app.skinColor.Value); }
                catch { /* gene-controlled, skip */ }
            }

            if (app.hairColor.HasValue)
            {
                try { pawn.story.GetType().GetProperty("hairColorOverride")?.SetValue(pawn.story, app.hairColor.Value); }
                catch { /* gene-controlled, skip */ }
            }
        }
    }
}
