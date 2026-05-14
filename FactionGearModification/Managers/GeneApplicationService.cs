using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Managers
{
    public static class GeneApplicationService
    {
        public static void ApplyGenes(Pawn pawn, KindGearData kindData)
        {
            if (!ModsConfig.BiotechActive) return;
            if (pawn?.genes == null || kindData == null) return;
            if (!kindData.ForceOverrideGenes) return;
            if (kindData.ForcedGenes == null) return;

            if (kindData.ForceOverrideGenes)
            {
                var xenogenes = pawn.genes.Xenogenes.ToList();
                foreach (var g in xenogenes)
                    pawn.genes.RemoveGene(g);
            }

            foreach (var fg in kindData.ForcedGenes)
            {
                if (fg.GeneDef == null) continue;
                if (!Rand.Chance(fg.chance)) continue;

                // Only add as xenogene by default; endogene requires custom xenotype
                pawn.genes.AddGene(fg.GeneDef, fg.asEndogene);
            }
        }
    }
}
