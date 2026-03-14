using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;
using FactionGearCustomizer.Compat.AmmoProviders;

namespace FactionGearCustomizer.Compat
{
    /// <summary>
    /// CE 兼容性类 - 保留向后兼容性
    /// 新的代码请使用 AmmoProviderManager
    /// </summary>
    public static class CECompat
    {
        private static bool? _isCEActive;
        public static bool IsCEActive
        {
            get
            {
                if (!_isCEActive.HasValue)
                {
                    _isCEActive = ModsConfig.IsActive("CETeam.CombatExtended");
                }
                return _isCEActive.Value;
            }
        }

        // 委托给新的 AmmoProviderManager 实现
        private static AmmoProviders.CEAmmoProvider CEProvider => 
            new AmmoProviders.CEAmmoProvider();

        // Cache reflection info
        private static Type _compPropertiesAmmoUserType; // CompProperties_AmmoUser
        private static FieldInfo _ammoSetField; // In CompProperties_AmmoUser
        private static FieldInfo _magazineSizeField; // In CompProperties_AmmoUser
        
        private static Type _ammoSetDefType;
        private static FieldInfo _ammoTypesField; // In AmmoSetDef
        
        private static Type _ammoLinkType;
        private static FieldInfo _ammoField; // In AmmoLink

        private static Type _compAmmoUserType; // CompAmmoUser
        private static MethodInfo _tryReduceAmmoMethod; // In CompAmmoUser
        private static PropertyInfo _currentAmmoProperty; // In CompAmmoUser
        private static PropertyInfo _magSizeProperty; // In CompAmmoUser

        private static Type _compPropertiesAmmoUserType2; // CompProperties_AmmoUser for ammo items
        private static Type _ammoDefType; // AmmoDef

        private static StatDef _bulkStat;

        public static void Init()
        {
            if (!IsCEActive) return;

            try
            {
                // AccessTools is from HarmonyLib
                _compPropertiesAmmoUserType = AccessTools.TypeByName("CombatExtended.CompProperties_AmmoUser");
                if (_compPropertiesAmmoUserType != null)
                {
                    _ammoSetField = AccessTools.Field(_compPropertiesAmmoUserType, "ammoSet");
                    _magazineSizeField = AccessTools.Field(_compPropertiesAmmoUserType, "magazineSize");
                }

                _ammoSetDefType = AccessTools.TypeByName("CombatExtended.AmmoSetDef");
                if (_ammoSetDefType != null)
                {
                    _ammoTypesField = AccessTools.Field(_ammoSetDefType, "ammoTypes");
                }

                _ammoLinkType = AccessTools.TypeByName("CombatExtended.AmmoLink");
                if (_ammoLinkType != null)
                {
                    _ammoField = AccessTools.Field(_ammoLinkType, "ammo");
                }

                _compAmmoUserType = AccessTools.TypeByName("CombatExtended.CompAmmoUser");
                if (_compAmmoUserType != null)
                {
                    _tryReduceAmmoMethod = AccessTools.Method(_compAmmoUserType, "TryReduceAmmoCount");
                    _currentAmmoProperty = AccessTools.Property(_compAmmoUserType, "CurrentAmmo");
                    _magSizeProperty = AccessTools.Property(_compAmmoUserType, "MagSize");
                }

                _bulkStat = DefDatabase<StatDef>.GetNamedSilentFail("Bulk");

                _ammoDefType = AccessTools.TypeByName("CombatExtended.AmmoDef");
                _compPropertiesAmmoUserType2 = AccessTools.TypeByName("CombatExtended.CompProperties_AmmoUser");
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to initialize CE compatibility: {ex.Message}");
            }
        }

        public static ThingDef GetDefaultAmmoFor(ThingDef weaponDef)
        {
            // 优先使用新的 AmmoProviderManager
            if (AmmoProviderManager.IsActive())
            {
                return AmmoProviderManager.GetDefaultAmmo(weaponDef);
            }

            // 向后兼容：如果新的管理器未激活，使用旧实现
            if (!IsCEActive || weaponDef == null) return null;
            if (_compPropertiesAmmoUserType == null) Init();
            if (_compPropertiesAmmoUserType == null) return null;

            CompProperties ammoUserProps = null;
            if (weaponDef.comps != null)
            {
                foreach (var comp in weaponDef.comps)
                {
                    if (comp.GetType() == _compPropertiesAmmoUserType || comp.GetType().IsSubclassOf(_compPropertiesAmmoUserType))
                    {
                        ammoUserProps = comp;
                        break;
                    }
                }
            }
            
            if (ammoUserProps == null) return null;

            var ammoSet = _ammoSetField.GetValue(ammoUserProps);
            if (ammoSet == null) return null;

            var ammoLinks = _ammoTypesField.GetValue(ammoSet) as System.Collections.IList;
            if (ammoLinks == null || ammoLinks.Count == 0) return null;

            var firstLink = ammoLinks[0];
            var ammoDef = _ammoField.GetValue(firstLink) as ThingDef;
            
            return ammoDef;
        }

        public static float GetBulk(ThingDef def)
        {
            if (!IsCEActive || def == null) return 0f;
            if (_bulkStat == null) Init();
            if (_bulkStat == null) return 0f;
            return def.GetStatValueAbstract(_bulkStat);
        }

        public static float GetMass(ThingDef def)
        {
            return def.BaseMass;
        }

        public static string GetAmmoSetLabel(ThingDef weaponDef)
        {
            // 优先使用新的 AmmoProviderManager
            if (AmmoProviderManager.IsActive())
            {
                return AmmoProviderManager.GetAmmoSetLabel(weaponDef);
            }

            // 向后兼容：旧实现
            if (!IsCEActive || weaponDef == null) return null;
            if (_compPropertiesAmmoUserType == null) Init();
            if (_compPropertiesAmmoUserType == null) return null;

            CompProperties ammoUserProps = null;
            if (weaponDef.comps != null)
            {
                foreach (var comp in weaponDef.comps)
                {
                    if (comp.GetType() == _compPropertiesAmmoUserType || comp.GetType().IsSubclassOf(_compPropertiesAmmoUserType))
                    {
                        ammoUserProps = comp;
                        break;
                    }
                }
            }
            
            if (ammoUserProps == null) return null;

            var ammoSet = _ammoSetField.GetValue(ammoUserProps);
            if (ammoSet == null) return null;

            return (ammoSet as Def)?.label;
        }

        public static List<string> GetAllAmmoSetLabels(List<ThingDef> weapons)
        {
            // 优先使用新的 AmmoProviderManager
            if (AmmoProviderManager.IsActive())
            {
                return AmmoProviderManager.GetAllAmmoSetLabels(weapons);
            }

            // 向后兼容：旧实现
            if (!IsCEActive || weapons == null) return new List<string>();
            HashSet<string> labels = new HashSet<string>();
            foreach(var w in weapons)
            {
                string label = GetAmmoSetLabel(w);
                if (!string.IsNullOrEmpty(label))
                {
                    labels.Add(label);
                }
            }
            return labels.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// 为使用 CE 弹药的武器生成并添加弹药到 pawn 的 inventory
        /// </summary>
        public static void GenerateAndAddAmmoForWeapon(Pawn pawn, ThingWithComps weapon)
        {
            if (!IsCEActive || pawn == null || weapon == null) return;
            if (_compAmmoUserType == null || _compPropertiesAmmoUserType == null) Init();

            try
            {
                if (pawn.inventory?.innerContainer == null) return;

                ThingDef ammoDef = GetCurrentAmmoFromComp(weapon);
                int magSize = GetCurrentMagSizeFromComp(weapon);

                if (ammoDef == null)
                {
                    ammoDef = GetDefaultAmmoFor(weapon.def);
                }

                if (magSize <= 0)
                {
                    magSize = GetWeaponMagazineSize(weapon.def);
                }

                if (ammoDef == null || magSize <= 0) return;

                int targetAmmoCount = magSize * Rand.Range(2, 4);
                int existingAmmoCount = pawn.inventory.innerContainer
                    .Where(t => t.def == ammoDef)
                    .Sum(t => t.stackCount);

                int toAdd = targetAmmoCount - existingAmmoCount;
                if (toAdd <= 0) return;

                AddAmmoToInventory(pawn, ammoDef, toAdd);
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to generate ammo for weapon {weapon?.def?.defName}: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查武器是否需要 CE 弹药
        /// </summary>
        public static bool WeaponNeedsAmmo(ThingWithComps weapon)
        {
            if (!IsCEActive || weapon == null) return false;
            if (_compAmmoUserType == null || _compPropertiesAmmoUserType == null) Init();

            if (_compAmmoUserType != null &&
                weapon.AllComps.Any(c => c.GetType() == _compAmmoUserType || c.GetType().IsSubclassOf(_compAmmoUserType)))
            {
                return true;
            }

            if (_compPropertiesAmmoUserType == null || weapon.def?.comps == null) return false;

            return weapon.def.comps.Any(c => _compPropertiesAmmoUserType.IsAssignableFrom(c.GetType()));
        }

        private static ThingDef GetCurrentAmmoFromComp(ThingWithComps weapon)
        {
            if (_compAmmoUserType == null || _currentAmmoProperty == null) return null;

            var compAmmoUser = weapon.AllComps.FirstOrDefault(c => c.GetType() == _compAmmoUserType || c.GetType().IsSubclassOf(_compAmmoUserType));
            if (compAmmoUser == null) return null;
            return _currentAmmoProperty.GetValue(compAmmoUser) as ThingDef;
        }

        private static int GetCurrentMagSizeFromComp(ThingWithComps weapon)
        {
            if (_compAmmoUserType == null || _magSizeProperty == null) return 0;

            var compAmmoUser = weapon.AllComps.FirstOrDefault(c => c.GetType() == _compAmmoUserType || c.GetType().IsSubclassOf(_compAmmoUserType));
            if (compAmmoUser == null) return 0;

            var magSize = _magSizeProperty.GetValue(compAmmoUser);
            return magSize is int size ? size : 0;
        }

        private static int GetWeaponMagazineSize(ThingDef weaponDef)
        {
            if (weaponDef?.comps == null || _compPropertiesAmmoUserType == null) return 0;

            foreach (var comp in weaponDef.comps)
            {
                if (!_compPropertiesAmmoUserType.IsAssignableFrom(comp.GetType())) continue;
                var magSizeValue = _magazineSizeField?.GetValue(comp);
                if (magSizeValue is int magSize)
                {
                    return magSize;
                }
            }

            return 0;
        }

        private static void AddAmmoToInventory(Pawn pawn, ThingDef ammoDef, int count)
        {
            if (count <= 0 || pawn?.inventory?.innerContainer == null || ammoDef == null) return;

            int remaining = count;
            int stackLimit = ammoDef.stackLimit > 0 ? ammoDef.stackLimit : count;

            while (remaining > 0)
            {
                Thing ammo = ThingMaker.MakeThing(ammoDef);
                if (ammo == null) return;

                int requestCount = Math.Min(remaining, stackLimit);
                ammo.stackCount = requestCount;

                if (!pawn.inventory.innerContainer.TryAdd(ammo, true))
                {
                    if (ammo.holdingOwner == null) ammo.Destroy();
                    return;
                }

                remaining -= requestCount;
            }
        }

        /// <summary>
        /// 检查物品是否为CE弹药
        /// </summary>
        public static bool IsCEAmmo(ThingDef thingDef)
        {
            if (!IsCEActive || thingDef == null) return false;
            if (_ammoDefType == null) Init();
            if (_ammoDefType == null) return false;

            return _ammoDefType.IsAssignableFrom(thingDef.GetType());
        }

        /// <summary>
        /// 获取物品的建议数量上限
        /// 对于CE弹药，返回弹匣容量的倍数（10倍）
        /// 对于普通物品，返回stackLimit
        /// </summary>
        public static int GetSuggestedMaxCount(ThingDef thingDef, int defaultMax = 50)
        {
            if (thingDef == null) return defaultMax;

            if (IsCEAmmo(thingDef))
            {
                int magSize = GetAmmoMagSize(thingDef);
                if (magSize > 0)
                {
                    return magSize * 10;
                }
            }

            return thingDef.stackLimit > 0 ? thingDef.stackLimit : defaultMax;
        }

        /// <summary>
        /// 获取弹药的弹匣容量（如果有）
        /// </summary>
        private static int GetAmmoMagSize(ThingDef thingDef)
        {
            if (!IsCEActive || thingDef == null) return 0;
            if (_compPropertiesAmmoUserType2 == null) Init();
            if (_compPropertiesAmmoUserType2 == null) return 0;

            try
            {
                if (thingDef.comps != null)
                {
                    foreach (var comp in thingDef.comps)
                    {
                        if (_compPropertiesAmmoUserType2.IsAssignableFrom(comp.GetType()))
                        {
                            var magSizeValue = _magazineSizeField?.GetValue(comp);
                            if (magSizeValue != null)
                            {
                                return (int)magSizeValue;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return 0;
        }
    }
}
