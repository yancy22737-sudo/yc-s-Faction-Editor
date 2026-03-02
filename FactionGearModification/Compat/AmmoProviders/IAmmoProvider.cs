using RimWorld;
using System.Collections.Generic;
using Verse;

namespace FactionGearCustomizer.Compat.AmmoProviders
{
    /// <summary>
    /// 弹药提供者接口 - 用于支持各种 combat mod 的弹药系统
    /// </summary>
    public interface IAmmoProvider
    {
        /// <summary>
        /// 弹药提供者名称（用于日志和调试）
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 检查此提供者是否处于激活状态
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 检查武器是否需要此提供者管理的弹药
        /// </summary>
        bool WeaponNeedsAmmo(ThingDef weaponDef);

        /// <summary>
        /// 获取武器的默认弹药
        /// </summary>
        ThingDef GetDefaultAmmo(ThingDef weaponDef);

        /// <summary>
        /// 获取武器的所有可用弹药类型
        /// </summary>
        List<ThingDef> GetAllAvailableAmmo(ThingDef weaponDef);

        /// <summary>
        /// 获取建议的弹药数量
        /// </summary>
        int GetSuggestedAmmoCount(ThingDef weaponDef, ThingDef ammoDef);

        /// <summary>
        /// 获取弹药组标签（用于 UI 显示）
        /// </summary>
        string GetAmmoSetLabel(ThingDef weaponDef);

        /// <summary>
        /// 检查物品是否为弹药
        /// </summary>
        bool IsAmmo(ThingDef thingDef);

        /// <summary>
        /// 获取所有弹药组标签 (用于 UI 筛选器)
        /// </summary>
        List<string> GetAllAmmoSetLabels(List<ThingDef> weapons);
    }
}
