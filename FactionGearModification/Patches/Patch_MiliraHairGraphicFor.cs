using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace FactionGearCustomizer.Patches
{
    /// <summary>
    /// 延迟应用的 Harmony Prefix 补丁：拦截 Milira mod 的 PawnRenderNode_MilianHair.GraphicFor 方法，
    /// 在路径参数为 null 时跳过原始调用，避免 ArgumentNullException。
    ///
    /// v1.5.4 修复策略：
    /// - 不再使用 [StaticConstructorOnStartup]（时机太早，Milira 程序集可能未加载，
    ///   且 FactionGearCustomizerMod.HarmonyInstance 可能尚未初始化）
    /// - 改为在 Mod 构造函数中通过 LongEventHandler.QueueLongEvent 延迟到游戏完全加载后应用
    /// - 使用独立的 Harmony 实例，不依赖主 mod 的 HarmonyInstance
    /// - 搜索 Milira 程序集时尝试多种类型名称
    /// - Prefix 方法使用 object 参数 + 反射，避免因签名不匹配导致补丁无效
    /// </summary>
    public static class Patch_MiliraHairGraphicFor
    {
        private const string HarmonyId = "com.factiongearcustomizer.milirahairfix";
        private static bool patchApplied = false;

        /// <summary>
        /// 由 FactionGearCustomizerMod 构造函数调用，
        /// 通过 LongEventHandler.QueueLongEvent 延迟到所有 mod 加载完成后再应用补丁。
        /// </summary>
        public static void SchedulePatch()
        {
            LongEventHandler.QueueLongEvent(ApplyIfNeeded, "MiliraHairFix", false, null);
        }

        private static void ApplyIfNeeded()
        {
            if (patchApplied) return;

            try
            {
                // 搜索 Milira 程序集中的头发渲染节点类型
                Type milianHairType = null;
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        // 尝试多种可能的类型名称
                        milianHairType = assembly.GetType("Milira.PawnRenderNode_MilianHair");
                        if (milianHairType != null) break;

                        milianHairType = assembly.GetType("Milira.PawnRenderNode_MiliraHair");
                        if (milianHairType != null) break;

                        // 搜索 Milira 程序集中所有包含 "Hair" 的 PawnRenderNode 子类
                        if (assembly.GetName().Name.Contains("Milira"))
                        {
                            var allTypes = assembly.GetTypes();
                            foreach (var t in allTypes)
                            {
                                if (t.Name.Contains("Hair") && t.IsSubclassOf(typeof(PawnRenderNode)))
                                {
                                    milianHairType = t;
                                    break;
                                }
                            }
                            if (milianHairType != null) break;
                        }
                    }
                    catch
                    {
                        // 忽略加载异常（某些程序集可能拒绝反射访问）
                    }
                }

                if (milianHairType == null)
                {
                    // Milira mod 未安装，不需要补丁
                    Log.Message("[FactionGearCustomizer] Milira hair type not found, skipping safety patch.");
                    return;
                }

                Log.Message($"[FactionGearCustomizer] Found Milira hair render node type: {milianHairType.FullName}");

                // 查找 GraphicFor 方法
                var graphicForMethod = AccessTools.Method(milianHairType, "GraphicFor");
                if (graphicForMethod == null)
                {
                    // 尝试查找其他可能的方法名
                    var allMethods = AccessTools.GetDeclaredMethods(milianHairType);
                    foreach (var m in allMethods)
                    {
                        if (m.Name.Contains("Graphic") && m.ReturnType == typeof(Graphic) && m.GetParameters().Length >= 1)
                        {
                            graphicForMethod = m;
                            break;
                        }
                    }
                }

                if (graphicForMethod == null)
                {
                    Log.Warning($"[FactionGearCustomizer] Found {milianHairType.FullName} but no GraphicFor method.");
                    return;
                }

                Log.Message($"[FactionGearCustomizer] Found GraphicFor method: {graphicForMethod}");

                // 使用独立的 Harmony 实例，避免依赖主 mod 的 HarmonyInstance
                var harmony = new Harmony(HarmonyId);

                var prefixMethod = new HarmonyMethod(
                    typeof(Patch_MiliraHairGraphicFor).GetMethod(
                        nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));

                harmony.Patch(graphicForMethod, prefix: prefixMethod);
                patchApplied = true;
                Log.Message("[FactionGearCustomizer] Milira safety patch applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Failed to apply Milira safety patch: {ex}");
            }
        }

        /// <summary>
        /// Prefix：在 GraphicFor 执行前检查 hair path 是否安全。
        /// 使用通用的 Pawn 参数匹配，不依赖特定参数名。
        /// </summary>
        private static bool Prefix(Pawn pawn, ref Graphic __result)
        {
            try
            {
                // 检查 Pawn 的 story/hair 定义
                // 某些 Pawn 类型（如机械体）没有 story，此时 story 为 null
                if (pawn?.story?.hairDef == null)
                {
                    // hairDef 为 null 意味着此 Pawn 没有头发定义，
                    // Milira 的 GraphicFor 会构造 null 路径传给 GraphicDatabase.Get，
                    // 导致 ArgumentNullException。直接返回安全默认值。
                    __result = BaseContent.BadGraphic;
                    return false; // 跳过原始方法
                }
            }
            catch
            {
                // 如果访问 pawn.story 失败，也返回安全默认值
                __result = BaseContent.BadGraphic;
                return false;
            }

            return true; // hairDef 存在且路径有效，继续执行原始方法
        }
    }
}
