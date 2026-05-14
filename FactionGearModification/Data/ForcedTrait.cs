using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class ForcedTrait : IExposable
    {
        public string traitDefName;
        public int degree = 0;
        public float chance = 1f;

        [Unsaved]
        private TraitDef cachedTraitDef;

        public TraitDef TraitDef
        {
            get
            {
                if (cachedTraitDef == null && !string.IsNullOrEmpty(traitDefName))
                    cachedTraitDef = DefDatabase<TraitDef>.GetNamedSilentFail(traitDefName);
                return cachedTraitDef;
            }
            set
            {
                cachedTraitDef = value;
                traitDefName = value?.defName;
            }
        }

        // Always uses native translated TaggedString (LabelCap) for display
        public string DegreeLabel
        {
            get
            {
                if (TraitDef?.degreeDatas != null && degree >= 0 && degree < TraitDef.degreeDatas.Count)
                {
                    var dd = TraitDef.degreeDatas[degree];
                    // LabelCap = native translated TaggedString; label = raw untranslated fallback
                    string s = dd.LabelCap.ToString();
                    return string.IsNullOrEmpty(s) ? (dd.label ?? TraitDef.defName) : s;
                }
                string lc = TraitDef?.LabelCap.ToString();
                if (!string.IsNullOrEmpty(lc))
                    return degree == 0 ? lc : lc + " +" + degree;
                return degree == 0 ? (TraitDef?.defName ?? "???") : (TraitDef?.defName ?? "???") + " +" + degree;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref traitDefName, "traitDef");
            Scribe_Values.Look(ref degree, "degree", 0);
            Scribe_Values.Look(ref chance, "chance", 1f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                ResolveReferences();
        }

        public void ResolveReferences()
        {
            if (!string.IsNullOrEmpty(traitDefName))
                cachedTraitDef = DefDatabase<TraitDef>.GetNamedSilentFail(traitDefName);
        }
    }
}
