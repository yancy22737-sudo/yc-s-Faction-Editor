using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public enum HediffPoolType
    {
        None,
        AnyDebuff,
        AnyDrugHigh,
        AnyAddiction,
        AnyImplant,
        AnyIllness,
        AnyBuff
    }

    public class ForcedHediff : IExposable
    {
        public HediffDef HediffDef;
        public HediffPoolType PoolType = HediffPoolType.None;
        public int maxParts = 0;
        public IntRange maxPartsRange = default(IntRange);
        public float chance = 1f;
        public FloatRange severityRange = default(FloatRange);
        public List<BodyPartDef> parts;

        public bool IsPool => PoolType != HediffPoolType.None;

        public string GetDisplayLabel()
        {
            if (IsPool)
            {
                return GetPoolLabel(PoolType);
            }
            return HediffDef?.LabelCap ?? "Unknown";
        }

        private string GetPoolLabel(HediffPoolType type)
        {
            switch (type)
            {
                case HediffPoolType.AnyDebuff: return LanguageManager.Get("HediffPool_AnyDebuff");
                case HediffPoolType.AnyDrugHigh: return LanguageManager.Get("HediffPool_AnyDrugHigh");
                case HediffPoolType.AnyAddiction: return LanguageManager.Get("HediffPool_AnyAddiction");
                case HediffPoolType.AnyImplant: return LanguageManager.Get("HediffPool_AnyImplant");
                case HediffPoolType.AnyIllness: return LanguageManager.Get("HediffPool_AnyIllness");
                case HediffPoolType.AnyBuff: return LanguageManager.Get("HediffPool_AnyBuff");
                default: return "Unknown Pool";
            }
        }

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

            Scribe_Values.Look(ref PoolType, "poolType", HediffPoolType.None);
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
