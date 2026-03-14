using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;

namespace FactionGearCustomizer.Compat.AmmoProviders
{
    /// <summary>
    /// Combat Extended 弹药提供者
    /// </summary>
    public class CEAmmoProvider : IAmmoProvider
    {
        private static bool? _isCEActive;
        public bool IsActive
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

        public string ProviderName => "Combat Extended";

        // Reflection 缓存
        private Type _compPropertiesAmmoUserType;
        private FieldInfo _ammoSetField;
        private FieldInfo _magazineSizeField;
        private Type _ammoSetDefType;
        private FieldInfo _ammoTypesField;
        private Type _ammoLinkType;
        private FieldInfo _ammoField;
        private Type _compAmmoUserType;
        private Type _ammoDefType;
        private StatDef _bulkStat;

        public CEAmmoProvider()
        {
            if (IsActive)
            {
                Init();
            }
        }

        private void Init()
        {
            try
            {
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
                _ammoDefType = AccessTools.TypeByName("CombatExtended.AmmoDef");
                _bulkStat = DefDatabase<StatDef>.GetNamedSilentFail("Bulk");
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to initialize CE Ammo Provider: {ex.Message}");
            }
        }

        public bool WeaponNeedsAmmo(ThingDef weaponDef)
        {
            if (!IsActive || weaponDef == null) return false;
            if (_compPropertiesAmmoUserType == null) return false;

            return weaponDef.comps != null && weaponDef.comps.Any(c =>
                _compPropertiesAmmoUserType.IsAssignableFrom(c.GetType()));
        }

        public ThingDef GetDefaultAmmo(ThingDef weaponDef)
        {
            if (!IsActive || weaponDef == null) return null;
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

            var ammoSet = _ammoSetField?.GetValue(ammoUserProps);
            if (ammoSet == null) return null;

            var ammoLinks = _ammoTypesField?.GetValue(ammoSet) as System.Collections.IList;
            if (ammoLinks == null || ammoLinks.Count == 0) return null;

            var ammoDef = _ammoField?.GetValue(ammoLinks[0]) as ThingDef;

            return ammoDef;
        }

        public List<ThingDef> GetAllAvailableAmmo(ThingDef weaponDef)
        {
            List<ThingDef> ammoList = new List<ThingDef>();
            if (!IsActive || weaponDef == null) return ammoList;
            if (_compPropertiesAmmoUserType == null) return ammoList;

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

            if (ammoUserProps == null) return ammoList;

            var ammoSet = _ammoSetField?.GetValue(ammoUserProps);
            if (ammoSet == null) return ammoList;

            var ammoLinks = _ammoTypesField?.GetValue(ammoSet) as System.Collections.IList;
            if (ammoLinks == null) return ammoList;

            foreach (var link in ammoLinks)
            {
                var ammoDef = _ammoField?.GetValue(link) as ThingDef;
                if (ammoDef != null)
                {
                    ammoList.Add(ammoDef);
                }
            }

            return ammoList;
        }

        public int GetSuggestedAmmoCount(ThingDef weaponDef, ThingDef ammoDef)
        {
            if (!IsActive || weaponDef == null || ammoDef == null) return 60;

            int magSize = GetMagSize(weaponDef);
            if (magSize > 0)
            {
                return magSize * Rand.Range(2, 4);
            }

            return 60;
        }

        public string GetAmmoSetLabel(ThingDef weaponDef)
        {
            if (!IsActive || weaponDef == null) return null;
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

            var ammoSet = _ammoSetField?.GetValue(ammoUserProps);
            if (ammoSet == null) return null;

            return (ammoSet as Def)?.label;
        }

        public bool IsAmmo(ThingDef thingDef)
        {
            if (!IsActive || thingDef == null) return false;
            if (_ammoDefType == null) return false;

            return _ammoDefType.IsAssignableFrom(thingDef.GetType());
        }

        private int GetMagSize(ThingDef weaponDef)
        {
            if (!IsActive || weaponDef == null) return 0;
            if (_compPropertiesAmmoUserType == null) return 0;

            foreach (var comp in weaponDef.comps)
            {
                if (comp.GetType() == _compPropertiesAmmoUserType || comp.GetType().IsSubclassOf(_compPropertiesAmmoUserType))
                {
                    var magSizeValue = _magazineSizeField?.GetValue(comp);
                    if (magSizeValue != null)
                    {
                        return (int)magSizeValue;
                    }
                }
            }

            return 0;
        }

        public List<string> GetAllAmmoSetLabels(List<ThingDef> weapons)
        {
            if (!IsActive || weapons == null) return new List<string>();
            HashSet<string> labels = new HashSet<string>();
            foreach (var w in weapons)
            {
                string label = GetAmmoSetLabel(w);
                if (!string.IsNullOrEmpty(label))
                {
                    labels.Add(label);
                }
            }
            return labels.OrderBy(x => x).ToList();
        }
    }
}
