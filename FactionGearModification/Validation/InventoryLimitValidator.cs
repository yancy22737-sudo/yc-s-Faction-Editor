using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using FactionGearCustomizer.Compat;

namespace FactionGearCustomizer.Validation
{
    public static class InventoryLimitValidator
    {
        private const int MAX_INVENTORY_ITEMS = 50;
        private const float MAX_TOTAL_MASS = 35f;
        private const float MAX_TOTAL_BULK = 50f;
        private const float CRITICAL_MASS_MULTIPLIER = 1.5f;
        private const float CRITICAL_BULK_MULTIPLIER = 1.5f;

        private static Dictionary<string, ValidationCacheEntry> _cache = new Dictionary<string, ValidationCacheEntry>();
        private static int _cacheVersion = 0;

        private struct ValidationCacheEntry
        {
            public int CacheVersion;
            public float TotalMass;
            public float TotalBulk;
            public int ItemCount;
            public bool IsValid;
        }

        public static ValidationResult ValidateAddItem(KindGearData kindData, ThingDef newItem, int addCount = 1)
        {
            if (kindData?.InventoryItems == null)
                return ValidationResult.Success();

            if (newItem == null)
                return ValidationResult.Failed("InvalidItem");

            float newItemMass = GetItemMass(newItem);
            float newItemBulk = GetItemBulk(newItem);

            var currentStats = CalculateCurrentStats(kindData);

            int projectedCount = currentStats.ItemCount + addCount;
            float projectedMass = currentStats.TotalMass + (newItemMass * addCount);
            float projectedBulk = currentStats.TotalBulk + (newItemBulk * addCount);

            var warnings = new List<string>();
            var criticalWarnings = new List<string>();

            if (projectedCount > MAX_INVENTORY_ITEMS)
            {
                return ValidationResult.Failed("InventoryItemCountLimit", MAX_INVENTORY_ITEMS);
            }

            float criticalMass = MAX_TOTAL_MASS * CRITICAL_MASS_MULTIPLIER;
            if (projectedMass > criticalMass)
            {
                criticalWarnings.Add($"MassCritical:{projectedMass:F1}/{criticalMass:F1}");
            }
            else if (projectedMass > MAX_TOTAL_MASS)
            {
                warnings.Add($"MassWarning:{projectedMass:F1}/{MAX_TOTAL_MASS:F1}");
            }

            if (CECompat.IsCEActive)
            {
                float criticalBulk = MAX_TOTAL_BULK * CRITICAL_BULK_MULTIPLIER;
                if (projectedBulk > criticalBulk)
                {
                    criticalWarnings.Add($"BulkCritical:{projectedBulk:F1}/{criticalBulk:F1}");
                }
                else if (projectedBulk > MAX_TOTAL_BULK)
                {
                    warnings.Add($"BulkWarning:{projectedBulk:F1}/{MAX_TOTAL_BULK:F1}");
                }
            }

            if (criticalWarnings.Count > 0)
            {
                return ValidationResult.Critical(string.Join("|", criticalWarnings));
            }

            if (warnings.Count > 0)
            {
                return ValidationResult.Warning(string.Join("|", warnings));
            }

            return ValidationResult.Success();
        }

        public static ValidationResult ValidateItemCountRange(KindGearData kindData, SpecRequirementEdit item, IntRange newRange)
        {
            if (kindData?.InventoryItems == null || item == null)
                return ValidationResult.Success();

            int currentMax = item.CountRange.max;
            int newMax = newRange.max;

            if (newMax <= currentMax)
                return ValidationResult.Success();

            int additionalCount = newMax - currentMax;

            if (item.Thing != null)
            {
                return ValidateAddItem(kindData, item.Thing, additionalCount);
            }

            var currentStats = CalculateCurrentStats(kindData);
            int projectedCount = currentStats.ItemCount + additionalCount;

            if (projectedCount > MAX_INVENTORY_ITEMS)
            {
                return ValidationResult.Failed("InventoryItemCountLimit", MAX_INVENTORY_ITEMS);
            }

            return ValidationResult.Success();
        }

        public static InventoryStats GetCurrentStats(KindGearData kindData)
        {
            return CalculateCurrentStats(kindData);
        }

        private static InventoryStats CalculateCurrentStats(KindGearData kindData)
        {
            if (kindData?.InventoryItems == null)
                return new InventoryStats();

            string cacheKey = kindData.kindDefName ?? "null";

            if (_cache.TryGetValue(cacheKey, out var entry) && entry.CacheVersion == _cacheVersion)
            {
                return new InventoryStats
                {
                    TotalMass = entry.TotalMass,
                    TotalBulk = entry.TotalBulk,
                    ItemCount = entry.ItemCount
                };
            }

            float totalMass = 0f;
            float totalBulk = 0f;
            int itemCount = 0;

            foreach (var item in kindData.InventoryItems)
            {
                if (item == null) continue;

                int count = item.CountRange.max;

                if (item.Thing != null)
                {
                    totalMass += GetItemMass(item.Thing) * count;
                    totalBulk += GetItemBulk(item.Thing) * count;
                    itemCount += count;
                }
                else if (item.PoolType != ItemPoolType.None)
                {
                    float avgMass = GetAveragePoolItemMass(item.PoolType);
                    float avgBulk = GetAveragePoolItemBulk(item.PoolType);
                    totalMass += avgMass * count;
                    totalBulk += avgBulk * count;
                    itemCount += count;
                }
            }

            _cache[cacheKey] = new ValidationCacheEntry
            {
                CacheVersion = _cacheVersion,
                TotalMass = totalMass,
                TotalBulk = totalBulk,
                ItemCount = itemCount
            };

            return new InventoryStats
            {
                TotalMass = totalMass,
                TotalBulk = totalBulk,
                ItemCount = itemCount
            };
        }

        public static void InvalidateCache()
        {
            _cacheVersion++;
            if (_cacheVersion > 10000)
            {
                _cache.Clear();
                _cacheVersion = 0;
            }
        }

        private static float GetItemMass(ThingDef def)
        {
            if (def == null) return 0f;
            return def.BaseMass;
        }

        private static float GetItemBulk(ThingDef def)
        {
            if (def == null) return 0f;
            return CECompat.GetBulk(def);
        }

        private static float GetAveragePoolItemMass(ItemPoolType poolType)
        {
            switch (poolType)
            {
                case ItemPoolType.AnyMeal:
                    return 0.4f;
                case ItemPoolType.AnyMedicine:
                    return 0.5f;
                case ItemPoolType.AnySocialDrug:
                case ItemPoolType.AnyHardDrug:
                    return 0.1f;
                case ItemPoolType.AnyRawFood:
                case ItemPoolType.AnyMeat:
                case ItemPoolType.AnyVegetable:
                    return 0.05f;
                default:
                    return 0.3f;
            }
        }

        private static float GetAveragePoolItemBulk(ItemPoolType poolType)
        {
            switch (poolType)
            {
                case ItemPoolType.AnyMeal:
                    return 1.5f;
                case ItemPoolType.AnyMedicine:
                    return 1.0f;
                case ItemPoolType.AnySocialDrug:
                case ItemPoolType.AnyHardDrug:
                    return 0.5f;
                case ItemPoolType.AnyRawFood:
                case ItemPoolType.AnyMeat:
                case ItemPoolType.AnyVegetable:
                    return 0.3f;
                default:
                    return 1.0f;
            }
        }
    }

    public struct InventoryStats
    {
        public float TotalMass;
        public float TotalBulk;
        public int ItemCount;

        public bool IsWithinLimits
        {
            get
            {
                const int MAX_INVENTORY_ITEMS = 50;
                const float MAX_TOTAL_MASS = 35f;
                const float MAX_TOTAL_BULK = 50f;

                if (ItemCount > MAX_INVENTORY_ITEMS)
                    return false;
                if (TotalMass > MAX_TOTAL_MASS)
                    return false;
                if (CECompat.IsCEActive && TotalBulk > MAX_TOTAL_BULK)
                    return false;
                return true;
            }
        }
    }

    public struct ValidationResult
    {
        public bool IsValid;
        public bool IsWarning;
        public bool IsCritical;
        public string ErrorKey;
        public object[] ErrorArgs;
        public string WarningMessage;

        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Failed(string errorKey, params object[] args)
        {
            return new ValidationResult
            {
                IsValid = false,
                IsWarning = false,
                ErrorKey = errorKey,
                ErrorArgs = args
            };
        }

        public static ValidationResult Warning(string message)
        {
            return new ValidationResult
            {
                IsValid = true,
                IsWarning = true,
                IsCritical = false,
                WarningMessage = message
            };
        }

        public static ValidationResult Critical(string message)
        {
            return new ValidationResult
            {
                IsValid = true,
                IsWarning = true,
                IsCritical = true,
                WarningMessage = message
            };
        }
    }
}
