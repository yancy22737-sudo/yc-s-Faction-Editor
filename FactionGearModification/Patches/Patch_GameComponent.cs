using HarmonyLib;
using RimWorld;
using Verse;
using FactionGearCustomizer.Core;
using System.Reflection;

namespace FactionGearCustomizer.Patches
{
    /// <summary>
    /// 确保 FactionGearGameComponent 被添加到游戏中
    /// </summary>
    [HarmonyPatch(typeof(Game))]
    [HarmonyPatch("FillComponents")]
    public static class Patch_Game_FillComponents
    {
        public static void Postfix(Game __instance)
        {
            if (__instance.GetComponent<FactionGearGameComponent>() == null)
            {
                __instance.components.Add(new FactionGearGameComponent(__instance));
            }
        }
    }

    /// <summary>
    /// 确保在加载游戏时 FactionGearGameComponent 被正确初始化
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Game_InitComponents
    {
        private static MethodBase TargetMethod()
        {
            // 尝试查找 InitComponents 方法，如果不存在则返回 null
            var method = AccessTools.Method(typeof(Game), "InitComponents");
            return method;
        }

        public static bool Prepare()
        {
            // 只有在方法存在时才应用此Patch
            return TargetMethod() != null;
        }

        public static void Postfix(Game __instance)
        {
            var component = __instance.GetComponent<FactionGearGameComponent>();
            if (component != null)
            {
                Log.Message("[FactionGearCustomizer] FactionGearGameComponent initialized.");
            }
        }
    }
}
