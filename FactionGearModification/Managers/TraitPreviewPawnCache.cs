using RimWorld;
using Verse;

namespace FactionGearCustomizer.Managers
{
    /// <summary>
    /// Minimal cached preview pawn for trait tooltip/info generation.
    /// Never spawned, never registered to world/map. UI-thread only.
    /// </summary>
    public static class TraitPreviewPawnCache
    {
        private static Pawn malePawn;
        private static Pawn femalePawn;

        public static void Clear()
        {
            malePawn = null;
            femalePawn = null;
        }

        public static Pawn GetOrCreate(Gender gender)
        {
            if (gender == Gender.Female)
            {
                if (femalePawn == null)
                    femalePawn = BuildPreviewPawn(Gender.Female);
                return femalePawn;
            }

            if (malePawn == null)
                malePawn = BuildPreviewPawn(Gender.Male);
            return malePawn;
        }

        public static Trait PrepareTrait(TraitDef def, int degree, Gender gender = Gender.Male)
        {
            if (def == null) return null;
            Pawn pawn = GetOrCreate(gender);
            if (pawn?.story?.traits == null) return new Trait(def, degree);

            pawn.story.traits.allTraits.Clear();
            var trait = new Trait(def, degree);
            pawn.story.traits.GainTrait(trait);
            return trait;
        }

        private static Pawn BuildPreviewPawn(Gender gender)
        {
            try
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.AllDefsListForReading
                    .Find(k => k.race != null && k.race.race != null && k.race.race.Humanlike);
                if (kind == null) return null;

                // Use the short constructor pattern already proven in this project
                var req = new PawnGenerationRequest(
                    kind,
                    null,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    true,   // forceGenerateNewPawn
                    false,  // allowDead
                    false,  // allowDowned
                    false,  // canGeneratePawnRelations
                    false,  // mustBeCapableOfViolence
                    0f,     // colonistRelationChanceFactor
                    false,  // forceAddFreeWarmLayerIfNeeded
                    true,   // allowGay
                    false,  // allowPregnant
                    false,  // allowFood
                    false   // inhabitant
                );

                Pawn p = PawnGenerator.GeneratePawn(req);
                if (p != null)
                    p.gender = gender;
                return p;
            }
            catch
            {
                return null;
            }
        }
    }
}
