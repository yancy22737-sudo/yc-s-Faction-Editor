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
        // 保存defName，确保引用不会在深拷贝或数据同步时丢失
        public string hediffDefName;
        public List<string> partsDefNames;
        
        // 缓存引用
        [Unsaved]
        private HediffDef cachedHediffDef;
        [Unsaved]
        private List<BodyPartDef> cachedParts;
        
        public HediffPoolType PoolType = HediffPoolType.None;
        public int maxParts = 0;
        public IntRange maxPartsRange = default(IntRange);
        public float chance = 1f;
        public FloatRange severityRange = default(FloatRange);

        public bool IsPool => PoolType != HediffPoolType.None;

        public HediffDef HediffDef
        {
            get
            {
                // 【修复】防御性编程：访问时自动重新解析
                if (cachedHediffDef == null && !string.IsNullOrEmpty(hediffDefName))
                {
                    cachedHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
                }
                return cachedHediffDef;
            }
            set
            {
                cachedHediffDef = value;
                hediffDefName = value?.defName;
            }
        }

        public List<BodyPartDef> parts
        {
            get
            {
                if (cachedParts == null && partsDefNames != null)
                {
                    cachedParts = new List<BodyPartDef>();
                    foreach (string partDefName in partsDefNames)
                    {
                        BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(partDefName);
                        if (def != null) cachedParts.Add(def);
                    }
                }
                return cachedParts;
            }
            set
            {
                cachedParts = value;
                if (value != null)
                {
                    partsDefNames = value.Select(p => p.defName).ToList();
                }
                else
                {
                    partsDefNames = null;
                }
            }
        }

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
            Scribe_Values.Look(ref hediffDefName, "hediffDef");
            Scribe_Values.Look(ref PoolType, "poolType", HediffPoolType.None);
            Scribe_Values.Look(ref maxParts, "maxParts");
            Scribe_Values.Look(ref maxPartsRange, "maxPartsRange");
            Scribe_Values.Look(ref chance, "chance");
            Scribe_Values.Look(ref severityRange, "severityRange");
            Scribe_Collections.Look(ref partsDefNames, "parts", LookMode.Value);
            
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
            if (!string.IsNullOrEmpty(hediffDefName))
            {
                cachedHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            }
            
            if (partsDefNames != null)
            {
                cachedParts = new List<BodyPartDef>();
                foreach (string partDefName in partsDefNames)
                {
                    BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(partDefName);
                    if (def != null) cachedParts.Add(def);
                }
            }
        }
    }
}
