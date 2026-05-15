using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public enum TradeStockCategory
    {
        All,
        Weapons,
        Armor,
        Apparel,
        Items,
        Animals,
        Slaves
    }

    public enum TradeEntryType
    {
        Thing,
        Animal,
        Slave
    }

    public class TradeStockEntry : IExposable
    {
        public string thingDefName;
        public string pawnKindDefName;   // for Animals/Slaves
        public TradeEntryType entryType = TradeEntryType.Thing;
        public QualityCategory quality = QualityCategory.Normal;
        public IntRange countRange = new IntRange(1, 10);
        public float weight = 1f;
        public string materialDefName;
        public bool allowHitPoints = true;

        [Unsaved]
        private ThingDef cachedThing;
        [Unsaved]
        private ThingDef cachedMaterial;
        [Unsaved]
        private PawnKindDef cachedPawnKind;

        public TradeStockEntry() { }

        public TradeStockEntry(string thingDefName)
        {
            this.thingDefName = thingDefName;
        }

        public ThingDef Thing
        {
            get
            {
                if (cachedThing == null && !string.IsNullOrEmpty(thingDefName))
                    cachedThing = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                return cachedThing;
            }
            set
            {
                cachedThing = value;
                thingDefName = value?.defName;
            }
        }

        public ThingDef Material
        {
            get
            {
                if (cachedMaterial == null && !string.IsNullOrEmpty(materialDefName))
                    cachedMaterial = DefDatabase<ThingDef>.GetNamedSilentFail(materialDefName);
                return cachedMaterial;
            }
            set
            {
                cachedMaterial = value;
                materialDefName = value?.defName;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingDefName, "thingDefName");
            Scribe_Values.Look(ref pawnKindDefName, "pawnKindDefName");
            Scribe_Values.Look(ref entryType, "entryType", TradeEntryType.Thing);
            Scribe_Values.Look(ref quality, "quality", QualityCategory.Normal);
            Scribe_Values.Look(ref countRange, "countRange", new IntRange(1, 10));
            Scribe_Values.Look(ref weight, "weight", 1f);
            Scribe_Values.Look(ref materialDefName, "materialDefName");
            Scribe_Values.Look(ref allowHitPoints, "allowHitPoints", true);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                ResolveReferences();
        }

        public void ResolveReferences()
        {
            if (!string.IsNullOrEmpty(thingDefName))
                cachedThing = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
            if (!string.IsNullOrEmpty(materialDefName))
                cachedMaterial = DefDatabase<ThingDef>.GetNamedSilentFail(materialDefName);
        }

        public Thing CreateThing()
        {
            var def = Thing;
            if (def == null) return null;

            // For stuffable items, use specified Material or default stuff
            ThingDef stuff = null;
            if (def.MadeFromStuff)
            {
                stuff = Material ?? GenStuff.DefaultStuffFor(def);
            }

            Thing thing = ThingMaker.MakeThing(def, stuff);

            if (thing.TryGetComp<CompQuality>() is CompQuality qc)
                qc.SetQuality(quality, ArtGenerationContext.Colony);

            if (!allowHitPoints)
                thing.HitPoints = thing.MaxHitPoints;

            return thing;
        }

        public TradeStockEntry DeepCopy()
        {
            return new TradeStockEntry
            {
                thingDefName = this.thingDefName,
                pawnKindDefName = this.pawnKindDefName,
                entryType = this.entryType,
                quality = this.quality,
                countRange = new IntRange(this.countRange.min, this.countRange.max),
                weight = this.weight,
                materialDefName = this.materialDefName,
                allowHitPoints = this.allowHitPoints,
                cachedThing = this.cachedThing,
                cachedMaterial = this.cachedMaterial
            };
        }
    }
}
