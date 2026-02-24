using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class GearItem : IExposable
    {
        public string thingDefName;
        public float weight = 1f;
        
        // 缓存 ThingDef 引用，避免频繁访�?DefDatabase
        [Unsaved]
        private ThingDef cachedThingDef;

        public GearItem() { }

        public GearItem(string thingDefName, float weight = 1f)
        {
            this.thingDefName = thingDefName;
            this.weight = weight;
            // 在创建时就解�?ThingDef 并缓�?            if (!string.IsNullOrEmpty(thingDefName))
            {
                cachedThingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingDefName, "thingDefName");
            Scribe_Values.Look(ref weight, "weight", 1f);
            
            // 加载后重新缓�?ThingDef 引用
            if (!string.IsNullOrEmpty(thingDefName))
            {
                cachedThingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
            }
        }

        public ThingDef ThingDef
        {
            get
            {
                // 如果未缓存或缓存失效，重新解析并缓存
                if (cachedThingDef == null && !string.IsNullOrEmpty(thingDefName))
                {
                    cachedThingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                }
                return cachedThingDef;
            }
        }
    }
}