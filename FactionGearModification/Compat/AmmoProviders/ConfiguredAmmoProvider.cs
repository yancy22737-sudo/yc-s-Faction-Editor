using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using Verse;
using FactionGearCustomizer.Compat.AmmoProviders;

namespace FactionGearCustomizer
{
    /// <summary>
    /// 自定义弹药提供者 - 通过 XML 配置支持任意 combat mod
    /// 
    /// 配置示例 (在 Defs/AmmoMappings.xml 中):
    /// 
    /// &lt;?xml version="1.0" encoding="utf-8" ?&gt;
    /// &lt;Defs&gt;
    ///   &lt;FactionGearCustomizer.AmmoMappingDef&gt;
    ///     &lt;defName&gt;MyCombatMod_AmmoMapping&lt;/defName&gt;
    ///     &lt;modName&gt;My Combat Mod&lt;/modName&gt;
    ///     &lt;providerName&gt;My Custom Combat Mod&lt;/providerName&gt;
    ///     &lt;weaponAmmoMappings&gt;
    ///       &lt;li&gt;
    ///         &lt;weaponDefName&gt;Gun_AssaultRifle&lt;/weaponDefName&gt;
    ///         &lt;ammoDefName&gt;Ammo_556x45mm&lt;/ammoDefName&gt;
    ///       &lt;/li&gt;
    ///     &lt;/weaponAmmoMappings&gt;
    ///     &lt;ammoCategories&gt;
    ///       &lt;li&gt;Ammo_556x45mm&lt;/li&gt;
    ///       &lt;li&gt;Ammo_762x39mm&lt;/li&gt;
    ///     &lt;/ammoCategories&gt;
    ///   &lt;/FactionGearCustomizer.AmmoMappingDef&gt;
    /// &lt;/Defs&gt;
    /// </summary>
    public class AmmoMappingDef : Def
    {
        /// <summary>
        /// 目标 Mod 名称（用于检测是否激活）
        /// </summary>
        public string modName;

        /// <summary>
        /// 提供者显示名称
        /// </summary>
        public string providerName;

        /// <summary>
        /// 武器到弹药的映射列表
        /// </summary>
        public List<WeaponAmmoMapping> weaponAmmoMappings;

        /// <summary>
        /// 弹药类型列表（用于筛选）
        /// </summary>
        public List<string> ammoCategories;

        /// <summary>
        /// 默认弹药数量倍数（相对于弹匣容量）
        /// </summary>
        public int defaultAmmoMultiplier = 3;
    }

    [Serializable]
    public class WeaponAmmoMapping
    {
        /// <summary>
        /// 武器 DefName
        /// </summary>
        public string weaponDefName;

        /// <summary>
        /// 默认弹药 DefName
        /// </summary>
        public string ammoDefName;

        /// <summary>
        /// 可选的弹匣容量
        /// </summary>
        public int magazineSize = 30;

        /// <summary>
        /// 所有可用弹药类型
        /// </summary>
        public List<string> availableAmmo;
    }

    /// <summary>
    /// 基于 XML 配置的弹药提供者
    /// </summary>
    public class ConfiguredAmmoProvider : IAmmoProvider
    {
        private AmmoMappingDef _mappingDef;
        private Dictionary<string, WeaponAmmoMapping> _weaponToAmmoMap;
        private HashSet<string> _ammoSet;

        public ConfiguredAmmoProvider(AmmoMappingDef mappingDef)
        {
            _mappingDef = mappingDef;
            BuildCache();
        }

        public string ProviderName => _mappingDef.providerName ?? _mappingDef.defName;

        public bool IsActive
        {
            get
            {
                if (string.IsNullOrEmpty(_mappingDef.modName))
                    return true; // 没有指定 modName 则默认激活

                return ModsConfig.IsActive(_mappingDef.modName);
            }
        }

        private void BuildCache()
        {
            _weaponToAmmoMap = new Dictionary<string, WeaponAmmoMapping>();
            _ammoSet = new HashSet<string>();

            if (_mappingDef.weaponAmmoMappings != null)
            {
                foreach (var mapping in _mappingDef.weaponAmmoMappings)
                {
                    if (!string.IsNullOrEmpty(mapping.weaponDefName))
                    {
                        _weaponToAmmoMap[mapping.weaponDefName] = mapping;
                    }

                    if (!string.IsNullOrEmpty(mapping.ammoDefName))
                    {
                        _ammoSet.Add(mapping.ammoDefName);
                    }

                    if (mapping.availableAmmo != null)
                    {
                        foreach (var ammo in mapping.availableAmmo)
                        {
                            _ammoSet.Add(ammo);
                        }
                    }
                }
            }

            if (_mappingDef.ammoCategories != null)
            {
                foreach (var ammo in _mappingDef.ammoCategories)
                {
                    _ammoSet.Add(ammo);
                }
            }
        }

        public bool WeaponNeedsAmmo(ThingDef weaponDef)
        {
            if (weaponDef == null) return false;
            return _weaponToAmmoMap.ContainsKey(weaponDef.defName);
        }

        public ThingDef GetDefaultAmmo(ThingDef weaponDef)
        {
            if (weaponDef == null) return null;

            if (_weaponToAmmoMap.TryGetValue(weaponDef.defName, out var mapping))
            {
                if (!string.IsNullOrEmpty(mapping.ammoDefName))
                {
                    return DefDatabase<ThingDef>.GetNamedSilentFail(mapping.ammoDefName);
                }
            }

            return null;
        }

        public List<ThingDef> GetAllAvailableAmmo(ThingDef weaponDef)
        {
            List<ThingDef> ammoList = new List<ThingDef>();
            if (weaponDef == null) return ammoList;

            if (_weaponToAmmoMap.TryGetValue(weaponDef.defName, out var mapping))
            {
                HashSet<string> ammoDefs = new HashSet<string>();

                if (!string.IsNullOrEmpty(mapping.ammoDefName))
                {
                    ammoDefs.Add(mapping.ammoDefName);
                }

                if (mapping.availableAmmo != null)
                {
                    foreach (var ammoName in mapping.availableAmmo)
                    {
                        ammoDefs.Add(ammoName);
                    }
                }

                foreach (var ammoName in ammoDefs)
                {
                    var ammoDef = DefDatabase<ThingDef>.GetNamedSilentFail(ammoName);
                    if (ammoDef != null)
                    {
                        ammoList.Add(ammoDef);
                    }
                }
            }

            return ammoList;
        }

        public int GetSuggestedAmmoCount(ThingDef weaponDef, ThingDef ammoDef)
        {
            if (weaponDef == null || ammoDef == null) return 60;

            if (_weaponToAmmoMap.TryGetValue(weaponDef.defName, out var mapping))
            {
                int magSize = mapping.magazineSize > 0 ? mapping.magazineSize : 30;
                return magSize * _mappingDef.defaultAmmoMultiplier;
            }

            return 60;
        }

        public string GetAmmoSetLabel(ThingDef weaponDef)
        {
            if (weaponDef == null) return null;

            if (_weaponToAmmoMap.TryGetValue(weaponDef.defName, out var mapping))
            {
                return ProviderName;
            }

            return null;
        }

        public bool IsAmmo(ThingDef thingDef)
        {
            if (thingDef == null) return false;
            return _ammoSet.Contains(thingDef.defName);
        }

        public List<string> GetAllAmmoSetLabels(List<ThingDef> weapons)
        {
            if (weapons == null) return new List<string>();

            HashSet<string> labels = new HashSet<string>();
            foreach (var w in weapons)
            {
                if (WeaponNeedsAmmo(w))
                {
                    labels.Add(ProviderName);
                }
            }
            return labels.OrderBy(x => x).ToList();
        }
    }

    /// <summary>
    /// 加载 XML 配置并注册弹药提供者
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ConfiguredAmmoProviderLoader
    {
        static ConfiguredAmmoProviderLoader()
        {
            try
            {
                var allMappings = DefDatabase<AmmoMappingDef>.AllDefs;
                foreach (var mapping in allMappings)
                {
                    var provider = new ConfiguredAmmoProvider(mapping);
                    AmmoProviderManager.RegisterProvider(provider);
                    Log.Message($"[FactionGearCustomizer] Loaded configured ammo provider: {provider.ProviderName} for mod: {mapping.modName ?? "Unknown"}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Failed to load configured ammo providers: {ex.Message}");
            }
        }
    }
}
