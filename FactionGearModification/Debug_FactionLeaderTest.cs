using System;
using System.Reflection;
using RimWorld;
using Verse;
using FactionGearCustomizer.Utils;

namespace FactionGearCustomizer
{
    /// <summary>
    /// 调试类：用于验证 factionLeader 字段的存在性和可读性
    /// 在游戏启动时自动执行测试
    /// </summary>
    public static class Debug_FactionLeaderTest
    {
        public static void RunTest()
        {
            try
            {
                LogUtils.Info("===== 开始 factionLeader 字段测试 =====");
                
                // 1. 检查 PawnKindDef 类型
                Type pawnKindType = typeof(PawnKindDef);
                Log.Message($"[Test] PawnKindDef 类型：{pawnKindType.FullName}");
                
                // 2. 查找所有字段
                FieldInfo[] allFields = pawnKindType.GetFields(
                    BindingFlags.Public | 
                    BindingFlags.NonPublic | 
                    BindingFlags.Instance | 
                    BindingFlags.Static);
                
                Log.Message($"[Test] PawnKindDef 字段总数：{allFields.Length}");
                
                // 3. 查找 factionLeader 字段
                FieldInfo factionLeaderField = pawnKindType.GetField("factionLeader", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (factionLeaderField != null)
                {
                    Log.Message($"[Test] ✓ 找到 factionLeader 字段!");
                    Log.Message($"[Test]   字段类型：{factionLeaderField.FieldType}");
                    Log.Message($"[Test]   是否公共：{factionLeaderField.IsPublic}");
                    Log.Message($"[Test]   是否私有：{factionLeaderField.IsPrivate}");
                    
                    // 4. 测试读取具体的 PawnKindDef
                    var tribalChief = DefDatabase<PawnKindDef>.GetNamedSilentFail("Tribal_ChiefMelee");
                    if (tribalChief != null)
                    {
                        object value = factionLeaderField.GetValue(tribalChief);
                        Log.Message($"[Test] Tribal_ChiefMelee 的 factionLeader 值：{value}");
                        
                        if (value is bool boolValue)
                        {
                            Log.Message($"[Test] ✓ 值类型为 bool: {boolValue}");
                        }
                    }
                    else
                    {
                        Log.Warning("[Test] 未找到 Tribal_ChiefMelee PawnKindDef");
                    }
                    
                    // 5. 测试另一个派系领袖
                    var mercenarySlasher = DefDatabase<PawnKindDef>.GetNamedSilentFail("Mercenary_Slasher");
                    if (mercenarySlasher != null)
                    {
                        object value = factionLeaderField.GetValue(mercenarySlasher);
                        Log.Message($"[Test] Mercenary_Slasher 的 factionLeader 值：{value}");
                    }
                    
                    // 6. 测试普通兵种（应该为 false）
                    var tribalPen = DefDatabase<PawnKindDef>.GetNamedSilentFail("Tribal_Pen");
                    if (tribalPen != null)
                    {
                        object value = factionLeaderField.GetValue(tribalPen);
                        Log.Message($"[Test] Tribal_Pen 的 factionLeader 值：{value} (应该为 false)");
                    }
                }
                else
                {
                    Log.Warning("[Test] ✗ 未找到 factionLeader 字段!");
                    
                    // 列出所有包含 "faction" 或 "leader" 的字段
                    Log.Message("[Test] 搜索包含 'faction' 或 'leader' 的字段:");
                    foreach (var field in allFields)
                    {
                        string fieldName = field.Name.ToLower();
                        if (fieldName.Contains("faction") || fieldName.Contains("leader"))
                        {
                            Log.Message($"[Test]   - {field.Name} ({field.FieldType.Name})");
                        }
                    }
                }
                
                LogUtils.Info("===== factionLeader 字段测试结束 =====");
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] factionLeader 测试失败：{ex}");
            }
        }
    }
}
