using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.UI.Utils
{
    public class ThingDefCacheEntry
    {
        public string LabelCap;
        public string LabelCapLower;
        public string DefName;
        public string DefNameLower;
        public string ModSource;
        public string ModSourceLower;
        public float MarketValue;
        public TechLevel TechLevel;
        public bool IsApparel;
        public bool IsWeapon;
        public bool IsRangedWeapon;
        public bool IsMeleeWeapon;
    }

    public static class ThingDefCache
    {
        private static Dictionary<ThingDef, ThingDefCacheEntry> cache = new Dictionary<ThingDef, ThingDefCacheEntry>();
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) return;
            
            cache.Clear();
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def == null) continue;
                
                var entry = new ThingDefCacheEntry
                {
                    LabelCap = def.LabelCap.ToString() ?? def.defName,
                    LabelCapLower = (def.LabelCap.ToString() ?? def.defName).ToLowerInvariant(),
                    DefName = def.defName ?? "",
                    DefNameLower = (def.defName ?? "").ToLowerInvariant(),
                    ModSource = FactionGearManager.GetModSource(def) ?? "",
                    ModSourceLower = (FactionGearManager.GetModSource(def) ?? "").ToLowerInvariant(),
                    MarketValue = def.BaseMarketValue,
                    TechLevel = def.techLevel,
                    IsApparel = def.IsApparel,
                    IsWeapon = def.IsWeapon,
                    IsRangedWeapon = def.IsRangedWeapon,
                    IsMeleeWeapon = def.IsMeleeWeapon
                };
                cache[def] = entry;
            }
            isInitialized = true;
        }

        public static ThingDefCacheEntry Get(ThingDef def)
        {
            if (!isInitialized) Initialize();
            if (def == null) return null;
            
            if (!cache.TryGetValue(def, out var entry))
            {
                entry = new ThingDefCacheEntry
                {
                    LabelCap = def.LabelCap.ToString() ?? def.defName,
                    LabelCapLower = (def.LabelCap.ToString() ?? def.defName).ToLowerInvariant(),
                    DefName = def.defName ?? "",
                    DefNameLower = (def.defName ?? "").ToLowerInvariant(),
                    ModSource = FactionGearManager.GetModSource(def) ?? "",
                    ModSourceLower = (FactionGearManager.GetModSource(def) ?? "").ToLowerInvariant(),
                    MarketValue = def.BaseMarketValue,
                    TechLevel = def.techLevel,
                    IsApparel = def.IsApparel,
                    IsWeapon = def.IsWeapon,
                    IsRangedWeapon = def.IsRangedWeapon,
                    IsMeleeWeapon = def.IsMeleeWeapon
                };
                cache[def] = entry;
            }
            return entry;
        }

        public static void Clear()
        {
            cache.Clear();
            isInitialized = false;
        }

        public static bool MatchesSearch(ThingDef def, string searchLower)
        {
            if (string.IsNullOrEmpty(searchLower)) return true;
            
            var entry = Get(def);
            if (entry == null) return false;
            
            return entry.LabelCapLower.Contains(searchLower) ||
                   entry.DefNameLower.Contains(searchLower) ||
                   entry.ModSourceLower.Contains(searchLower);
        }
    }
}
