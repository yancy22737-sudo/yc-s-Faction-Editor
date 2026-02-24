using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.UI;

namespace FactionGearCustomizer
{
    /// <summary>
    /// Patch FactionDef.FactionIcon 属性，正确处理自定义图标路径（Custom:前缀）
    /// 防止游戏在加载自定义图标时报错
    /// </summary>
    [HarmonyPatch(typeof(FactionDef), nameof(FactionDef.FactionIcon), MethodType.Getter)]
    public static class Patch_FactionDef_FactionIcon
    {
        public static bool Prefix(FactionDef __instance, ref Texture2D __result)
        {
            // 检查是否是自定义图标路径
            if (!__instance.factionIconPath.NullOrEmpty() && __instance.factionIconPath.StartsWith("Custom:"))
            {
                string iconName = __instance.factionIconPath.Substring(7);
                Texture2D customIcon = CustomIconManager.GetIcon(iconName);
                if (customIcon != null)
                {
                    __result = customIcon;
                    return false; // 跳过原始方法
                }
                // 如果自定义图标加载失败，返回默认错误纹理
                __result = BaseContent.BadTex;
                return false; // 跳过原始方法
            }

            // 对于非自定义路径，让原始方法执行
            return true;
        }
    }
}
