using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer.Compat.AmmoProviders
{
    /// <summary>
    /// [示例模板] 自定义 Combat Mod 弹药提供者
    /// 
    /// 使用方法：
    /// 1. 复制此文件并重命名（如：MyCombatModAmmoProvider.cs）
    /// 2. 修改类名和 ProviderName
    /// 3. 实现 IsActive 属性检测 mod 是否激活
    /// 4. 实现各个方法处理弹药逻辑
    /// 5. 在 AmmoProviderManager.Initialize() 中注册
    /// 
    /// 支持的 Combat Mod 示例：
    /// - Combat Extended (已实现)
    /// - Guns Up!
    /// - Modern Weapons
    /// - [其他任何有弹药系统的 mod]
    /// </summary>
    public class CustomCombatModAmmoProvider : IAmmoProvider
    {
        // Mod 检测
        private static bool? _isModActive;
        
        public bool IsActive
        {
            get
            {
                if (!_isModActive.HasValue)
                {
                    // 替换为你的 Mod 的包名
                    _isModActive = ModsConfig.IsActive("YourModName.YourCombatMod");
                }
                return _isModActive.Value;
            }
        }

        public string ProviderName => "Your Combat Mod";

        // 如果需要反射，在这里缓存类型和信息
        // private Type _weaponComponentType;
        // private FieldInfo _ammoTypeField;

        public CustomCombatModAmmoProvider()
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
                // 在这里初始化反射信息
                // 示例：
                // _weaponComponentType = AccessTools.TypeByName("YourMod.WeaponComponent");
                // _ammoTypeField = AccessTools.Field(_weaponComponentType, "ammoType");
                
                Log.Message($"[FactionGearCustomizer] {ProviderName} Ammo Provider initialized");
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to initialize {ProviderName} Ammo Provider: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查武器是否需要此 Mod 的弹药
        /// </summary>
        public bool WeaponNeedsAmmo(ThingDef weaponDef)
        {
            if (!IsActive || weaponDef == null) return false;

            // 实现你的检测逻辑
            // 示例：检查武器是否有特定的 CompProperties
            // return weaponDef.comps?.Any(c => c.GetType() == _weaponComponentType) ?? false;

            return false; // 占位实现
        }

        /// <summary>
        /// 获取武器的默认弹药
        /// </summary>
        public ThingDef GetDefaultAmmo(ThingDef weaponDef)
        {
            if (!IsActive || weaponDef == null) return null;

            // 实现你的弹药获取逻辑
            // 示例：从武器的 component 中读取默认弹药类型

            return null; // 占位实现
        }

        /// <summary>
        /// 获取武器的所有可用弹药类型
        /// </summary>
        public List<ThingDef> GetAllAvailableAmmo(ThingDef weaponDef)
        {
            List<ThingDef> ammoList = new List<ThingDef>();
            if (!IsActive || weaponDef == null) return ammoList;

            // 实现你的弹药列表获取逻辑
            // 示例：从武器 def 中读取所有兼容的弹药类型

            return ammoList; // 占位实现
        }

        /// <summary>
        /// 获取建议的弹药数量
        /// </summary>
        public int GetSuggestedAmmoCount(ThingDef weaponDef, ThingDef ammoDef)
        {
            if (!IsActive || weaponDef == null || ammoDef == null) return 60;

            // 实现你的弹药数量计算逻辑
            // 示例：基于弹匣容量或武器类型计算

            return 60; // 占位实现
        }

        /// <summary>
        /// 获取弹药组标签（用于 UI 显示）
        /// </summary>
        public string GetAmmoSetLabel(ThingDef weaponDef)
        {
            if (!IsActive || weaponDef == null) return null;

            // 实现你的弹药组标签获取逻辑

            return null; // 占位实现
        }

        /// <summary>
        /// 检查物品是否为弹药
        /// </summary>
        public bool IsAmmo(ThingDef thingDef)
        {
            if (!IsActive || thingDef == null) return false;

            // 实现你的弹药检测逻辑
            // 示例：检查 ThingDef 的类型或父类

            return false; // 占位实现
        }

        /// <summary>
        /// 获取所有武器的弹药组标签列表
        /// </summary>
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
