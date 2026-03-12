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

    public enum ItemPoolType
    {
        None,
        AnyFood,
        AnyMeal,
        AnyRawFood,
        AnyMeat,
        AnyVegetable,
        AnyMedicine,
        AnySocialDrug,
        AnyHardDrug
    }

    public class SpecRequirementEdit : IExposable
    {
        // 保存defName，确保引用不会在深拷贝或数据同步时丢失
        public string thingDefName;
        public string materialDefName;
        public string styleDefName;
        
        // 缓存引用
        [Unsaved]
        private ThingDef cachedThing;
        [Unsaved]
        private ThingDef cachedMaterial;
        [Unsaved]
        private ThingStyleDef cachedStyle;
        
        public QualityCategory? Quality;
        public bool Biocode;
        public Color Color;
        public ApparelSelectionMode SelectionMode = ApparelSelectionMode.AlwaysTake;
        public float SelectionChance = 1f;
        public float weight = 1f;
        public IntRange CountRange = new IntRange(1, 1);
        public ItemPoolType PoolType = ItemPoolType.None;

        public SpecRequirementEdit() { }

        public ThingDef Thing
        {
            get
            {
                // 【修复】防御性编程：访问时自动重新解析
                if (cachedThing == null && !string.IsNullOrEmpty(thingDefName))
                {
                    cachedThing = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                }
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
                {
                    cachedMaterial = DefDatabase<ThingDef>.GetNamedSilentFail(materialDefName);
                }
                return cachedMaterial;
            }
            set
            {
                cachedMaterial = value;
                materialDefName = value?.defName;
            }
        }

        public ThingStyleDef Style
        {
            get
            {
                if (cachedStyle == null && !string.IsNullOrEmpty(styleDefName))
                {
                    cachedStyle = DefDatabase<ThingStyleDef>.GetNamedSilentFail(styleDefName);
                }
                return cachedStyle;
            }
            set
            {
                cachedStyle = value;
                styleDefName = value?.defName;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingDefName, "thing");
            Scribe_Values.Look(ref materialDefName, "material");
            Scribe_Values.Look(ref styleDefName, "style");
            
            Scribe_Values.Look(ref Quality, "quality");
            Scribe_Values.Look(ref Biocode, "biocode");
            Scribe_Values.Look(ref Color, "color");
            Scribe_Values.Look(ref SelectionMode, "selectionMode", ApparelSelectionMode.AlwaysTake);
            Scribe_Values.Look(ref SelectionChance, "selectionChance", 1f);
            Scribe_Values.Look(ref weight, "weight", 1f);
            Scribe_Values.Look(ref CountRange, "countRange", new IntRange(1, 1));
            Scribe_Values.Look(ref PoolType, "poolType", ItemPoolType.None);
            
            // 加载后重新缓存引用
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ResolveReferences();
            }
        }

        /// <summary>
        /// 【修复】显式解析所有引用，确保在深拷贝/数据同步后引用有效
        /// </summary>
        public void ResolveReferences()
        {
            if (!string.IsNullOrEmpty(thingDefName))
            {
                cachedThing = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
            }
            if (!string.IsNullOrEmpty(materialDefName))
            {
                cachedMaterial = DefDatabase<ThingDef>.GetNamedSilentFail(materialDefName);
            }
            if (!string.IsNullOrEmpty(styleDefName))
            {
                cachedStyle = DefDatabase<ThingStyleDef>.GetNamedSilentFail(styleDefName);
            }
        }
    }
}
