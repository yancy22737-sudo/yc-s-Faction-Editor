using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class ForcedHediff : IExposable
    {
        public HediffDef HediffDef;
        public int maxParts = 0;
        public IntRange maxPartsRange = default(IntRange);
        public float chance = 1f;
        public FloatRange severityRange = default(FloatRange);
        public List<BodyPartDef> parts;

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                string defName = HediffDef?.defName;
                Scribe_Values.Look(ref defName, "hediffDef");
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                string defName = null;
                Scribe_Values.Look(ref defName, "hediffDef");
                if (!string.IsNullOrEmpty(defName))
                {
                    HediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                }
            }

            Scribe_Values.Look(ref maxParts, "maxParts");
            Scribe_Values.Look(ref maxPartsRange, "maxPartsRange");
            Scribe_Values.Look(ref chance, "chance");
            Scribe_Values.Look(ref severityRange, "severityRange");

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<string> partsList = parts?.Select(p => p.defName).ToList();
                Scribe_Collections.Look(ref partsList, "parts", LookMode.Value);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<string> partsList = null;
                Scribe_Collections.Look(ref partsList, "parts", LookMode.Value);
                if (partsList != null)
                {
                    parts = new List<BodyPartDef>();
                    foreach (string partDefName in partsList)
                    {
                        BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(partDefName);
                        if (def != null) parts.Add(def);
                    }
                }
            }
        }
    }
}
