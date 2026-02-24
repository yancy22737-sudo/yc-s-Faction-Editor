using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;

namespace FactionGearCustomizer.Compat
{
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
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to initialize CE compatibility: {ex.Message}");
            }
        }

        public static ThingDef GetDefaultAmmoFor(ThingDef weaponDef)
        {
            if (!IsCEActive || weaponDef == null) return null;
            if (_compPropertiesAmmoUserType == null) Init();
            if (_compPropertiesAmmoUserType == null) return null;

            // Find CompProperties_AmmoUser in weaponDef.comps
            // weaponDef.comps is List<CompProperties>
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

            // Get AmmoSet
            var ammoSet = _ammoSetField.GetValue(ammoUserProps);
            if (ammoSet == null) return null;

            // Get AmmoTypes list (List<AmmoLink>)
            var ammoLinks = _ammoTypesField.GetValue(ammoSet) as System.Collections.IList;
            if (ammoLinks == null || ammoLinks.Count == 0) return null;

            // Get first ammo link
            var firstLink = ammoLinks[0];
            
            // Get ammo ThingDef
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
            if (!IsCEActive || weaponDef == null) return null;
            if (_compPropertiesAmmoUserType == null) Init();
            if (_compPropertiesAmmoUserType == null) return null;

            // Find CompProperties_AmmoUser in weaponDef.comps
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

            // Get AmmoSet
            var ammoSet = _ammoSetField.GetValue(ammoUserProps);
            if (ammoSet == null) return null;

            return (ammoSet as Def)?.label;
        }

        public static List<string> GetAllAmmoSetLabels(List<ThingDef> weapons)
        {
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
            if (_compAmmoUserType == null) Init();
            if (_compAmmoUserType == null) return;

            try
            {
                // 获取武器的 CompAmmoUser
                var compAmmoUser = weapon.AllComps.FirstOrDefault(c => c.GetType() == _compAmmoUserType || c.GetType().IsSubclassOf(_compAmmoUserType));
                if (compAmmoUser == null) return;

                // 获取当前弹药类型和弹匣容量
                var currentAmmo = _currentAmmoProperty?.GetValue(compAmmoUser) as ThingDef;
                int magSize = _magSizeProperty != null ? (int)_magSizeProperty.GetValue(compAmmoUser) : 0;

                if (currentAmmo == null || magSize <= 0) return;

                // 计算需要生成的弹药数量（2-3个弹匣的弹药量）
                int ammoCount = magSize * Rand.Range(2, 4);

                // 创建弹药
                Thing ammo = ThingMaker.MakeThing(currentAmmo);
                if (ammo == null) return;

                ammo.stackCount = ammoCount;

                // 添加到 pawn 的 inventory
                if (pawn.inventory?.innerContainer != null && ammo.holdingOwner == null)
                {
                    pawn.inventory.innerContainer.TryAdd(ammo, true);
                }
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
            if (_compAmmoUserType == null) Init();
            if (_compAmmoUserType == null) return false;

            return weapon.AllComps.Any(c => c.GetType() == _compAmmoUserType || c.GetType().IsSubclassOf(_compAmmoUserType));
        }
    }
}
