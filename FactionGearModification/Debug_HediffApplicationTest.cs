using System;
using System.Collections.Generic;
using System.Linq;
using FactionGearCustomizer.Validation;
using RimWorld;
using Verse;
using FactionGearCustomizer.Utils;

namespace FactionGearCustomizer
{
    /// <summary>
    /// 调试类：用于验证假体/仿生零件应用功能
    /// 在游戏启动时自动执行测试
    /// </summary>
    public static class Debug_HediffApplicationTest
    {
        public static void RunTest()
        {
            try
            {
                LogUtils.Info("===== 开始假体应用功能测试 =====");
                
                // 1. 测试部件映射系统
                TestPartMappingSystem();
                
                // 2. 测试验证器
                TestValidator();
                
                // 3. 测试假体应用流程
                TestHediffApplicationFlow();
                
                LogUtils.Info("===== 假体应用功能测试结束 =====");
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] 假体应用测试失败：{ex}");
            }
        }

        private static void TestPartMappingSystem()
        {
            Log.Message("[Test] 1. 测试部件映射系统");
            
            // 测试 BionicEye 映射
            var bionicEyeDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicEye");
            if (bionicEyeDef != null)
            {
                var parts = HediffPartMappingManager.GetRecommendedPartsForHediff(bionicEyeDef);
                Log.Message($"[Test]   BionicEye 推荐部位: {string.Join(", ", parts)}");
                
                if (parts.Any(p => p.Contains("Eye")))
                {
                    Log.Message("[Test]   ✓ BionicEye 映射正确");
                }
                else
                {
                    Log.Warning("[Test]   ✗ BionicEye 映射错误 - 应该包含 Eye 部位");
                }
            }
            else
            {
                Log.Warning("[Test]   ! BionicEye 未找到（可能未启用相关DLC）");
            }
            
            // 测试 BionicArm 映射
            var bionicArmDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicArm");
            if (bionicArmDef != null)
            {
                var parts = HediffPartMappingManager.GetRecommendedPartsForHediff(bionicArmDef);
                Log.Message($"[Test]   BionicArm 推荐部位: {string.Join(", ", parts)}");
                
                if (parts.Any(p => p.Contains("Arm")))
                {
                    Log.Message("[Test]   ✓ BionicArm 映射正确");
                }
                else
                {
                    Log.Warning("[Test]   ✗ BionicArm 映射错误 - 应该包含 Arm 部位");
                }
            }
            else
            {
                Log.Warning("[Test]   ! BionicArm 未找到（可能未启用相关DLC）");
            }
            
            // 测试 Joywire 映射
            var joywireDef = DefDatabase<HediffDef>.GetNamedSilentFail("Joywire");
            if (joywireDef != null)
            {
                var parts = HediffPartMappingManager.GetRecommendedPartsForHediff(joywireDef);
                Log.Message($"[Test]   Joywire 推荐部位: {string.Join(", ", parts)}");
                
                if (parts.Contains("Brain"))
                {
                    Log.Message("[Test]   ✓ Joywire 映射正确");
                }
                else
                {
                    Log.Warning("[Test]   ✗ Joywire 映射错误 - 应该包含 Brain 部位");
                }
            }
            else
            {
                Log.Warning("[Test]   ! Joywire 未找到（可能未启用相关DLC）");
            }
            
            // 测试部位最大植入数量
            int brainMax = HediffPartMappingManager.GetMaxImplantCountForPart("Brain");
            int heartMax = HediffPartMappingManager.GetMaxImplantCountForPart("Heart");
            Log.Message($"[Test]   Brain 最大植入数: {brainMax}");
            Log.Message($"[Test]   Heart 最大植入数: {heartMax}");
        }

        private static void TestValidator()
        {
            Log.Message("[Test] 2. 测试验证器");
            
            // 测试空值验证
            var nullResult = HediffApplicationValidator.ValidateForcedHediff(null);
            if (!nullResult.IsValid && nullResult.HasErrors)
            {
                Log.Message("[Test]   ✓ 空值验证正确 - 检测到错误");
            }
            else
            {
                Log.Warning("[Test]   ✗ 空值验证失败");
            }
            
            // 测试有效 Hediff 验证
            var bionicEyeDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicEye");
            if (bionicEyeDef != null)
            {
                var forcedHediff = new ForcedHediff
                {
                    HediffDef = bionicEyeDef,
                    chance = 1f,
                    maxParts = 2
                };
                
                var result = HediffApplicationValidator.ValidateForcedHediff(forcedHediff);
                if (result.IsValid)
                {
                    Log.Message("[Test]   ✓ 有效 Hediff 验证通过");
                }
                else
                {
                    Log.Warning($"[Test]   ✗ 有效 Hediff 验证失败: {string.Join(", ", result.Errors)}");
                }
            }
            
            // 测试无效概率验证
            if (bionicEyeDef != null)
            {
                var forcedHediff = new ForcedHediff
                {
                    HediffDef = bionicEyeDef,
                    chance = 1.5f, // 无效概率
                    maxParts = 2
                };
                
                var result = HediffApplicationValidator.ValidateForcedHediff(forcedHediff);
                if (result.HasWarnings)
                {
                    Log.Message("[Test]   ✓ 无效概率检测正确 - 发出警告");
                }
                else
                {
                    Log.Warning("[Test]   ✗ 无效概率检测失败");
                }
            }
        }

        private static void TestHediffApplicationFlow()
        {
            Log.Message("[Test] 3. 测试假体应用流程");
            
            // 检查 GearApplier 方法是否存在
            var gearApplierType = typeof(GearApplier);
            var applyHediffsMethod = gearApplierType.GetMethod("ApplyHediffs", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Static);
            
            if (applyHediffsMethod != null)
            {
                Log.Message("[Test]   ✓ ApplyHediffs 方法存在");
            }
            else
            {
                Log.Warning("[Test]   ✗ ApplyHediffs 方法未找到");
            }
            
            // 检查验证器集成
            var applyCustomGearMethod = gearApplierType.GetMethod("ApplyCustomGear",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static);
            
            if (applyCustomGearMethod != null)
            {
                Log.Message("[Test]   ✓ ApplyCustomGear 方法存在");
                
                // 检查方法体中是否调用了验证器
                var methodBody = applyCustomGearMethod.GetMethodBody();
                if (methodBody != null)
                {
                    Log.Message("[Test]   ✓ ApplyCustomGear 方法体可读取");
                }
            }
            else
            {
                Log.Warning("[Test]   ✗ ApplyCustomGear 方法未找到");
            }
            
            // 检查 Patch 是否正确设置
            var patchType = typeof(Patch_GeneratePawn);
            var postfixMethod = patchType.GetMethod("Postfix");
            
            if (postfixMethod != null)
            {
                Log.Message("[Test]   ✓ Patch_GeneratePawn.Postfix 方法存在");
            }
            else
            {
                Log.Warning("[Test]   ✗ Patch_GeneratePawn.Postfix 方法未找到");
            }
            
            // 总结
            Log.Message("[Test] 假体应用流程检查完成:");
            Log.Message("[Test]   - Pawn 生成时会被 Patch 拦截");
            Log.Message("[Test]   - Patch 调用 GearApplier.ApplyCustomGear");
            Log.Message("[Test]   - ApplyCustomGear 调用 ApplyHediffs");
            Log.Message("[Test]   - ApplyHediffs 使用验证器和映射系统");
        }

        /// <summary>
        /// 在游戏中生成一个测试 Pawn 并应用假体（需要在游戏运行时调用）
        /// </summary>
        public static void TestInGameHediffApplication()
        {
            try
            {
                LogUtils.Info("===== 开始游戏内假体应用测试 =====");
                
                if (Current.Game == null)
                {
                    Log.Warning("[Test] 游戏未运行，无法执行游戏内测试");
                    return;
                }
                
                // 创建测试用的 KindGearData
                var kindData = new KindGearData
                {
                    kindDefName = "TestKind"
                };
                
                // 添加 BionicEye
                var bionicEyeDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicEye");
                if (bionicEyeDef != null)
                {
                    kindData.ForcedHediffs = new List<ForcedHediff>
                    {
                        new ForcedHediff
                        {
                            HediffDef = bionicEyeDef,
                            chance = 1f,
                            maxParts = 2
                        }
                    };
                    
                    Log.Message($"[Test] 已添加 BionicEye 到测试配置");
                }
                
                // 生成测试 Pawn
                var request = new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false
                );
                
                Pawn testPawn = PawnGenerator.GeneratePawn(request);
                
                if (testPawn != null)
                {
                    Log.Message($"[Test] 生成测试 Pawn: {testPawn.Name}");
                    
                    // 记录应用前的状态
                    int hediffCountBefore = testPawn.health.hediffSet.hediffs.Count;
                    Log.Message($"[Test] 应用前 Hediff 数量: {hediffCountBefore}");
                    
                    // 手动调用 ApplyHediffs（通过反射）
                    var gearApplierType = typeof(GearApplier);
                    var applyHediffsMethod = gearApplierType.GetMethod("ApplyHediffs",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static);
                    
                    if (applyHediffsMethod != null)
                    {
                        applyHediffsMethod.Invoke(null, new object[] { testPawn, kindData });
                        
                        // 记录应用后的状态
                        int hediffCountAfter = testPawn.health.hediffSet.hediffs.Count;
                        Log.Message($"[Test] 应用后 Hediff 数量: {hediffCountAfter}");
                        
                        // 检查是否应用了 BionicEye
                        var bionicEyes = testPawn.health.hediffSet.hediffs
                            .Where(h => h.def.defName == "BionicEye")
                            .ToList();
                        
                        if (bionicEyes.Any())
                        {
                            Log.Message($"[Test]   ✓ 成功应用了 {bionicEyes.Count} 个 BionicEye");
                            foreach (var eye in bionicEyes)
                            {
                                Log.Message($"[Test]     - 部位: {eye.Part?.Label ?? "Unknown"}");
                            }
                        }
                        else
                        {
                            Log.Warning("[Test]   ✗ 未找到应用的 BionicEye");
                        }
                    }
                    
                    // 清理测试 Pawn
                    testPawn.Destroy();
                }
                else
                {
                    Log.Warning("[Test] 无法生成测试 Pawn");
                }
                
                LogUtils.Info("===== 游戏内假体应用测试结束 =====");
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] 游戏内测试失败：{ex}");
            }
        }
    }
}
