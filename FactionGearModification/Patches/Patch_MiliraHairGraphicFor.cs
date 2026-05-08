using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace FactionGearCustomizer.Patches
{
    /// <summary>
    /// Harmony Prefix for ALL Milira hair PawnRenderNode.GraphicFor methods.
    /// Prevents ArgumentNullException when hairDef is null (pawn has no hair)
    /// by returning null to skip the node. Otherwise lets Milira's code run normally.
    /// </summary>
    public static class Patch_MiliraHairGraphicFor
    {
        private const string HarmonyId = "com.factiongearcustomizer.milirahairfix";
        private static bool patchApplied = false;

        public static void SchedulePatch()
        {
            LongEventHandler.QueueLongEvent(ApplyIfNeeded, "MiliraHairFix", false, null);
        }

        private static void ApplyIfNeeded()
        {
            if (patchApplied) return;

            try
            {
                var milianHairTypes = new List<Type>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        TryAddType(assembly, "Milira.PawnRenderNode_MilianHair", milianHairTypes);
                        TryAddType(assembly, "Milira.PawnRenderNode_MiliraHair", milianHairTypes);
                        TryAddType(assembly, "Milira.PawnRenderNode_MilianHairBG", milianHairTypes);

                        if (assembly.GetName().Name.Contains("Milira"))
                        {
                            foreach (var t in assembly.GetTypes())
                            {
                                if (t.Name.Contains("Hair") && t.IsSubclassOf(typeof(PawnRenderNode))
                                    && !milianHairTypes.Contains(t))
                                    milianHairTypes.Add(t);
                            }
                        }
                    }
                    catch { }
                }

                if (milianHairTypes.Count == 0)
                {
                    Log.Message("[FactionGearCustomizer] Milira hair type not found, skipping safety patch.");
                    return;
                }

                Log.Message($"[FactionGearCustomizer] Found {milianHairTypes.Count} Milira hair render node type(s): {string.Join(", ", milianHairTypes.Select(t => t.FullName))}");

                var harmony = new Harmony(HarmonyId);
                var prefix = new HarmonyMethod(typeof(Patch_MiliraHairGraphicFor), nameof(Prefix));
                int patched = 0;

                foreach (var hairType in milianHairTypes)
                {
                    // 只打补丁到在该类型上声明的覆写方法，不碰基类实现
                    var method = AccessTools.DeclaredMethod(hairType, "GraphicFor");
                    if (method == null) continue;

                    harmony.Patch(method, prefix: prefix);
                    patched++;
                }

                patchApplied = true;
                Log.Message($"[FactionGearCustomizer] Milira safety patch applied to {patched} method(s).");
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to apply Milira safety patch: {ex}");
            }
        }

        private static void TryAddType(System.Reflection.Assembly assembly, string typeName, List<Type> types)
        {
            try
            {
                var t = assembly.GetType(typeName);
                if (t != null && !types.Contains(t)) types.Add(t);
            }
            catch { }
        }

        /// <summary>
        /// 仅在 PortraitsCache 肖像渲染期间 hairDef 为 null 时跳过。
        /// 游戏内渲染让 Milira 原代码正常运行。
        /// </summary>
        [ThreadStatic]
        public static bool IsRenderingPortrait;

        private static bool Prefix(Pawn pawn, ref Graphic __result)
        {
            if (IsRenderingPortrait && pawn?.story?.hairDef == null)
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}
