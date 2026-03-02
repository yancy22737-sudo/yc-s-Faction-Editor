using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Compat.AmmoProviders
{
    /// <summary>
    /// 弹药提供者管理器 - 管理所有弹药提供者并统一调用
    /// </summary>
    public static class AmmoProviderManager
    {
        private static List<IAmmoProvider> _providers = new List<IAmmoProvider>();
        private static bool _initialized = false;

        /// <summary>
        /// 所有已注册的弹药提供者
        /// </summary>
        public static IEnumerable<IAmmoProvider> Providers => _providers.AsReadOnly();

        /// <summary>
        /// 所有激活的弹药提供者
        /// </summary>
        public static IEnumerable<IAmmoProvider> ActiveProviders => _providers.Where(p => p.IsActive);

        /// <summary>
        /// 检查是否有任何激活的弹药提供者
        /// </summary>
        public static bool IsActive()
        {
            if (!_initialized) Initialize();
            return _providers.Any(p => p.IsActive);
        }

        /// <summary>
        /// 初始化所有弹药提供者
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _providers.Clear();

            // 注册内置的弹药提供者
            RegisterProvider(new CEAmmoProvider());

            // 未来可以在这里注册其他 combat mod 的弹药提供者

            _initialized = true;
            Log.Message($"[FactionGearCustomizer] Ammo Provider Manager initialized with {_providers.Count} providers");
        }

        /// <summary>
        /// 注册弹药提供者
        /// </summary>
        public static void RegisterProvider(IAmmoProvider provider)
        {
            if (provider == null)
            {
                Log.Warning("[FactionGearCustomizer] Attempted to register null ammo provider");
                return;
            }

            if (_providers.Any(p => p.GetType() == provider.GetType()))
            {
                Log.Warning($"[FactionGearCustomizer] Ammo provider {provider.ProviderName} already registered");
                return;
            }

            _providers.Add(provider);
            Log.Message($"[FactionGearCustomizer] Registered ammo provider: {provider.ProviderName}");
        }

        /// <summary>
        /// 取消注册弹药提供者
        /// </summary>
        public static void UnregisterProvider<T>() where T : IAmmoProvider
        {
            var provider = _providers.FirstOrDefault(p => p is T);
            if (provider != null)
            {
                _providers.Remove(provider);
                Log.Message($"[FactionGearCustomizer] Unregistered ammo provider: {provider.ProviderName}");
            }
        }

        /// <summary>
        /// 获取所有激活的弹药提供者
        /// </summary>
        public static List<IAmmoProvider> GetActiveProviders()
        {
            if (!_initialized) Initialize();
            return _providers.Where(p => p.IsActive).ToList();
        }

        #region 武器弹药查询

        /// <summary>
        /// 检查武器是否需要任何已注册的弹药
        /// </summary>
        public static bool WeaponNeedsAmmo(ThingDef weaponDef)
        {
            if (!_initialized) Initialize();
            if (weaponDef == null) return false;

            return ActiveProviders.Any(p => p.WeaponNeedsAmmo(weaponDef));
        }

        /// <summary>
        /// 获取武器的默认弹药（从第一个匹配的提供者）
        /// </summary>
        public static ThingDef GetDefaultAmmo(ThingDef weaponDef)
        {
            if (!_initialized) Initialize();
            if (weaponDef == null) return null;

            foreach (var provider in ActiveProviders)
            {
                var ammo = provider.GetDefaultAmmo(weaponDef);
                if (ammo != null)
                {
                    return ammo;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取武器的所有可用弹药类型（合并所有提供者）
        /// </summary>
        public static List<ThingDef> GetAllAvailableAmmo(ThingDef weaponDef)
        {
            if (!_initialized) Initialize();
            if (weaponDef == null) return new List<ThingDef>();

            HashSet<ThingDef> ammoDefs = new HashSet<ThingDef>();

            foreach (var provider in ActiveProviders)
            {
                var ammoList = provider.GetAllAvailableAmmo(weaponDef);
                if (ammoList != null)
                {
                    foreach (var ammo in ammoList)
                    {
                        ammoDefs.Add(ammo);
                    }
                }
            }

            return ammoDefs.ToList();
        }

        /// <summary>
        /// 获取建议的弹药数量
        /// </summary>
        public static int GetSuggestedAmmoCount(ThingDef weaponDef, ThingDef ammoDef)
        {
            if (!_initialized) Initialize();
            if (weaponDef == null || ammoDef == null) return 60;

            foreach (var provider in ActiveProviders)
            {
                if (provider.IsAmmo(ammoDef))
                {
                    return provider.GetSuggestedAmmoCount(weaponDef, ammoDef);
                }
            }

            // 默认值
            return 60;
        }

        /// <summary>
        /// 获取弹药组标签
        /// </summary>
        public static string GetAmmoSetLabel(ThingDef weaponDef)
        {
            if (!_initialized) Initialize();
            if (weaponDef == null) return null;

            foreach (var provider in ActiveProviders)
            {
                var label = provider.GetAmmoSetLabel(weaponDef);
                if (!string.IsNullOrEmpty(label))
                {
                    return label;
                }
            }

            return null;
        }

        /// <summary>
        /// 检查物品是否为任何已注册提供者管理的弹药
        /// </summary>
        public static bool IsAmmo(ThingDef thingDef)
        {
            if (!_initialized) Initialize();
            if (thingDef == null) return false;

            return ActiveProviders.Any(p => p.IsAmmo(thingDef));
        }

        /// <summary>
        /// 获取所有激活提供者的弹药组标签列表
        /// </summary>
        public static List<string> GetAllAmmoSetLabels(List<ThingDef> weapons)
        {
            if (!_initialized) Initialize();
            if (weapons == null) return new List<string>();

            HashSet<string> labels = new HashSet<string>();

            foreach (var provider in ActiveProviders)
            {
                var providerLabels = provider.GetAllAmmoSetLabels(weapons);
                if (providerLabels != null)
                {
                    foreach (var label in providerLabels)
                    {
                        labels.Add(label);
                    }
                }
            }

            return labels.OrderBy(x => x).ToList();
        }

        #endregion

        #region 自动弹药添加

        /// <summary>
        /// 为武器自动添加弹药到配置
        /// </summary>
        public static void TryAutoAddAmmo(ThingDef weaponDef, List<SpecRequirementEdit> inventoryList)
        {
            if (!_initialized) Initialize();
            if (weaponDef == null || inventoryList == null) return;
            if (!weaponDef.IsRangedWeapon) return;

            var ammo = GetDefaultAmmo(weaponDef);
            if (ammo == null) return;

            // 检查是否已存在该弹药
            if (inventoryList.Any(x => x?.Thing == ammo)) return;

            int ammoCount = GetSuggestedAmmoCount(weaponDef, ammo);

            inventoryList.Add(new SpecRequirementEdit
            {
                Thing = ammo,
                CountRange = new IntRange(ammoCount, ammoCount * 2),
                SelectionMode = ApparelSelectionMode.AlwaysTake
            });

            Log.Message($"[FactionGearCustomizer] Auto-added ammo {ammo.label} for weapon {weaponDef.label}");
        }

        #endregion
    }
}
