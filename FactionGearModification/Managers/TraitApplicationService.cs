using RimWorld;
using Verse;

namespace FactionGearCustomizer.Managers
{
    public static class TraitApplicationService
    {
        public static void ApplyTraits(Pawn pawn, KindGearData kindData)
        {
            if (pawn?.story?.traits == null || kindData == null) return;
            if (!kindData.ForceOverrideTraits) return;
            if (kindData.ForcedTraits == null) return;

            if (kindData.ForceOverrideTraits)
            {
                pawn.story.traits.allTraits.Clear();
            }

            foreach (var ft in kindData.ForcedTraits)
            {
                if (ft.TraitDef == null) continue;
                if (!Rand.Chance(ft.chance)) continue;

                var trait = new Trait(ft.TraitDef, ft.degree);
                pawn.story.traits.GainTrait(trait);
            }
        }
    }
}
