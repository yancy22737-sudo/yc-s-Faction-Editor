using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class ForcedAppearance : IExposable
    {
        public string hairDefName;
        public string beardDefName;
        public string bodyTypeDefName;
        public string headTypeDefName;
        public Color? skinColor;
        public Color? hairColor;
        public List<string> tattooDefNames;
        public float chance = 1f;

        [Unsaved]
        private HairDef cachedHairDef;
        [Unsaved]
        private BeardDef cachedBeardDef;
        [Unsaved]
        private BodyTypeDef cachedBodyTypeDef;
        [Unsaved]
        private HeadTypeDef cachedHeadTypeDef;

        public HairDef HairDef => Resolve(ref cachedHairDef, hairDefName);
        public BeardDef BeardDef => Resolve(ref cachedBeardDef, beardDefName);
        public BodyTypeDef BodyTypeDef => Resolve(ref cachedBodyTypeDef, bodyTypeDefName);
        public HeadTypeDef HeadTypeDef => Resolve(ref cachedHeadTypeDef, headTypeDefName);

        private T Resolve<T>(ref T cache, string defName) where T : Def
        {
            if (cache == null && !string.IsNullOrEmpty(defName))
                cache = DefDatabase<T>.GetNamedSilentFail(defName);
            return cache;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref hairDefName, "hairDef");
            Scribe_Values.Look(ref beardDefName, "beardDef");
            Scribe_Values.Look(ref bodyTypeDefName, "bodyTypeDef");
            Scribe_Values.Look(ref headTypeDefName, "headTypeDef");
            Scribe_Values.Look(ref skinColor, "skinColor");
            Scribe_Values.Look(ref hairColor, "hairColor");
            Scribe_Collections.Look(ref tattooDefNames, "tattoos", LookMode.Value);
            Scribe_Values.Look(ref chance, "chance", 1f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                ResolveReferences();
        }

        public void ResolveReferences()
        {
            if (!string.IsNullOrEmpty(hairDefName))
                cachedHairDef = DefDatabase<HairDef>.GetNamedSilentFail(hairDefName);
            if (!string.IsNullOrEmpty(beardDefName))
                cachedBeardDef = DefDatabase<BeardDef>.GetNamedSilentFail(beardDefName);
            if (!string.IsNullOrEmpty(bodyTypeDefName))
                cachedBodyTypeDef = DefDatabase<BodyTypeDef>.GetNamedSilentFail(bodyTypeDefName);
            if (!string.IsNullOrEmpty(headTypeDefName))
                cachedHeadTypeDef = DefDatabase<HeadTypeDef>.GetNamedSilentFail(headTypeDefName);
        }
    }
}
