using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class ForcedGene : IExposable
    {
        public string geneDefName;
        public bool asEndogene = false;
        public float chance = 1f;

        [Unsaved]
        private GeneDef cachedGeneDef;

        public GeneDef GeneDef
        {
            get
            {
                if (cachedGeneDef == null && !string.IsNullOrEmpty(geneDefName))
                    cachedGeneDef = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
                return cachedGeneDef;
            }
            set
            {
                cachedGeneDef = value;
                geneDefName = value?.defName;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref geneDefName, "geneDef");
            Scribe_Values.Look(ref asEndogene, "asEndogene", false);
            Scribe_Values.Look(ref chance, "chance", 1f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                ResolveReferences();
        }

        public void ResolveReferences()
        {
            if (!string.IsNullOrEmpty(geneDefName))
                cachedGeneDef = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
        }
    }
}
