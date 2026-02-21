using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public enum ApparelSelectionMode
    {
        AlwaysTake,
        RandomChance,
        FromPool1,
        FromPool2,
        FromPool3,
        FromPool4
    }

    public class SpecRequirementEdit : IExposable
    {
        public ThingDef Thing;
        public ThingDef Material;
        public ThingStyleDef Style;
        public QualityCategory? Quality;
        public bool Biocode;
        public Color Color;
        public ApparelSelectionMode SelectionMode = ApparelSelectionMode.AlwaysTake;
        public float SelectionChance = 1f;
        public float weight = 1f;

        public SpecRequirementEdit() { }

        public void ExposeData()
        {
            // Robust loading for Thing
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                string thingDefName = Thing?.defName;
                Scribe_Values.Look(ref thingDefName, "thing");
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                string thingDefName = null;
                Scribe_Values.Look(ref thingDefName, "thing");
                if (!string.IsNullOrEmpty(thingDefName))
                {
                    Thing = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                }
            }

            // Robust loading for Material
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                string materialDefName = Material?.defName;
                Scribe_Values.Look(ref materialDefName, "material");
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                string materialDefName = null;
                Scribe_Values.Look(ref materialDefName, "material");
                if (!string.IsNullOrEmpty(materialDefName))
                {
                    Material = DefDatabase<ThingDef>.GetNamedSilentFail(materialDefName);
                }
            }

            // Robust loading for Style
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                string styleDefName = Style?.defName;
                Scribe_Values.Look(ref styleDefName, "style");
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                string styleDefName = null;
                Scribe_Values.Look(ref styleDefName, "style");
                if (!string.IsNullOrEmpty(styleDefName))
                {
                    Style = DefDatabase<ThingStyleDef>.GetNamedSilentFail(styleDefName);
                }
            }

            Scribe_Values.Look(ref Quality, "quality");
            Scribe_Values.Look(ref Biocode, "biocode");
            Scribe_Values.Look(ref Color, "color");
            Scribe_Values.Look(ref SelectionMode, "selectionMode", ApparelSelectionMode.AlwaysTake);
            Scribe_Values.Look(ref SelectionChance, "selectionChance");
            Scribe_Values.Look(ref weight, "weight", 1f);
        }
    }
}
