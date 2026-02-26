using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public enum ItemCategoryFilter
    {
        All,
        Food,
        Medicine,
        SocialDrug,
        HardDrug,
        Ammo
    }

    public class ThingPickerFilterState
    {
        public string SearchText = "";
        public HashSet<string> SelectedMods = new HashSet<string>();
        public int LastModCount = 0;
        public HashSet<string> SelectedAmmoSets = new HashSet<string>();
        public int LastAmmoSetCount = 0;
        public TechLevel? SelectedTechLevel = null;
        public ItemCategoryFilter? SelectedCategory = null;
        public string SortField = "MarketValue";
        public bool SortAscending = false;
        public FloatRange MarketValue = new FloatRange(0f, 100000f);
        public FloatRange Range = new FloatRange(0f, 100f);
        public FloatRange Damage = new FloatRange(0f, 100f);
        public float MaxMarketValue = 100000f;
        public float MaxRange = 100f;
        public float MaxDamage = 100f;

        public void SyncAvailableMods(List<string> allMods)
        {
            bool wasAll = SelectedMods.Count == LastModCount && LastModCount > 0;
            if (SelectedMods.Count == 0 || wasAll)
            {
                SelectedMods = new HashSet<string>(allMods);
            }
            else
            {
                SelectedMods.RemoveWhere(m => !allMods.Contains(m));
                if (SelectedMods.Count == 0) SelectedMods = new HashSet<string>(allMods);
            }
            LastModCount = allMods.Count;
        }

        public void SyncAvailableAmmoSets(List<string> allAmmoSets)
        {
            bool wasAll = SelectedAmmoSets.Count == LastAmmoSetCount && LastAmmoSetCount > 0;
            if (SelectedAmmoSets.Count == 0 || wasAll)
            {
                SelectedAmmoSets = new HashSet<string>(allAmmoSets);
            }
            else
            {
                SelectedAmmoSets.RemoveWhere(a => !allAmmoSets.Contains(a));
                if (SelectedAmmoSets.Count == 0) SelectedAmmoSets = new HashSet<string>(allAmmoSets);
            }
            LastAmmoSetCount = allAmmoSets.Count;
        }

        public void ClampRanges()
        {
            MarketValue = new FloatRange(Mathf.Clamp(MarketValue.min, 0f, MaxMarketValue), Mathf.Clamp(MarketValue.max, 0f, MaxMarketValue));
            Range = new FloatRange(Mathf.Clamp(Range.min, 0f, MaxRange), Mathf.Clamp(Range.max, 0f, MaxRange));
            Damage = new FloatRange(Mathf.Clamp(Damage.min, 0f, MaxDamage), Mathf.Clamp(Damage.max, 0f, MaxDamage));
        }
    }

    public class HediffPickerFilterState
    {
        public string SearchText = "";
        public HashSet<string> SelectedCategories = new HashSet<string>();
        public int LastCategoryCount = 0;

        public void SyncAvailableCategories(List<string> allCategories)
        {
            bool wasAll = SelectedCategories.Count == LastCategoryCount && LastCategoryCount > 0;
            if (SelectedCategories.Count == 0 || wasAll)
            {
                SelectedCategories = new HashSet<string>(allCategories);
            }
            else
            {
                SelectedCategories.RemoveWhere(c => !allCategories.Contains(c));
                if (SelectedCategories.Count == 0) SelectedCategories = new HashSet<string>(allCategories);
            }
            LastCategoryCount = allCategories.Count;
        }
    }

    public static class PickerSession
    {
        public static ThingPickerFilterState Inventory = new ThingPickerFilterState();
        public static ThingPickerFilterState Apparel = new ThingPickerFilterState();
        public static ThingPickerFilterState Weapons = new ThingPickerFilterState();
        public static HediffPickerFilterState Hediffs = new HediffPickerFilterState();
    }
}
