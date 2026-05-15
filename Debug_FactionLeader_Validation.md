# FactionLeader 字段验证指南

## 📋 概述

本文档说明如何验证 RimWorld 原生代码中 `factionLeader` 字段的存在性和可读性。

## 🔍 背景

在 XML 文件中，多个 PawnKindDef 使用了 `<factionLeader>true</factionLeader>` 标记，例如：
- `PawnKinds_Tribal.xml` L186: Tribal_ChiefMelee
- `PawnKinds_Mercenary.xml` L309: Mercenary_Slasher
- `PawnKinds_Empire.xml` L1281
- `PawnKinds_Outlander.xml` L111
- `PawnKinds_Waster.xml` L84
- `PawnKinds_TradersGuild.xml` L223

## 🛠️ 验证方法

### 方法 1: 使用调试类自动测试

项目中已添加 `Debug_FactionLeaderTest.cs`，会在游戏启动时自动运行测试。

**测试内容**：
1. 检查 `PawnKindDef` 类型是否存在 `factionLeader` 字段
2. 验证字段类型是否为 `bool`
3. 测试读取具体的派系领袖兵种（Tribal_ChiefMelee, Mercenary_Slasher）
4. 测试读取普通兵种（应该返回 false）

**查看日志**：
启动游戏后，在日志中搜索 `[FactionGearCustomizer] ===== 开始 factionLeader 字段测试 =====`

**期望输出**：
```
[FactionGearCustomizer] ===== 开始 factionLeader 字段测试 =====
[Test] PawnKindDef 类型：RimWorld.PawnKindDef
[Test] PawnKindDef 字段总数：XX
[Test] ✓ 找到 factionLeader 字段!
[Test]   字段类型：System.Boolean
[Test]   是否公共：False/True
[Test]   是否私有：False/True
[Test] Tribal_ChiefMelee 的 factionLeader 值：True
[Test] ✓ 值类型为 bool: True
[Test] Mercenary_Slasher 的 factionLeader 值：True
[Test] Tribal_Pen 的 factionLeader 值：False (应该为 false)
[FactionGearCustomizer] ===== factionLeader 字段测试结束 =====
```

### 方法 2: 手动验证代码

在控制台或 Mod 中添加以下代码：

```csharp
using System.Reflection;
using RimWorld;
using Verse;

// 获取 PawnKindDef 类型
Type pawnKindType = typeof(PawnKindDef);

// 查找 factionLeader 字段
FieldInfo factionLeaderField = pawnKindType.GetField(
    "factionLeader", 
    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
);

if (factionLeaderField != null)
{
    Log.Message($"✓ 找到 factionLeader 字段，类型：{factionLeaderField.FieldType}");
    
    // 测试读取
    var tribalChief = DefDatabase<PawnKindDef>.GetNamedSilentFail("Tribal_ChiefMelee");
    if (tribalChief != null)
    {
        object value = factionLeaderField.GetValue(tribalChief);
        Log.Message($"Tribal_ChiefMelee.factionLeader = {value}");
    }
}
else
{
    Log.Warning("✗ 未找到 factionLeader 字段");
}
```

### 方法 3: 检查 XML 定义

查看 RimWorld 安装目录或 Mod 目录中的 XML 文件：

```bash
# 使用 grep 搜索
grep -r "factionLeader" */PawnKinds_*.xml

# 或使用 rg (ripgrep)
rg "factionLeader" --type xml
```

## 📊 验证结果

### 如果字段存在
- ✓ 我们的实现是正确的
- ✓ `FactionGearEditor.IsFactionLeader()` 方法可以正常工作
- ✓ UI 中会正确显示派系领袖标识

### 如果字段不存在
可能的原因：
1. **字段名称不同**：可能是 `isFactionLeader`、`FactionLeader` 等其他命名
2. **通过属性访问**：可能是 Property 而非 Field
3. **RimWorld 版本差异**：不同版本可能使用不同的字段名
4. **XML 直接加载到其他位置**：可能加载到了其他类或字典中

**解决方案**：
- 检查测试日志中列出的所有包含 "faction" 或 "leader" 的字段
- 使用 Harmony Patch 检查游戏运行时的实际数据
- 查阅 RimWorld 官方文档或源码

## 🔧 调试工具

### 反编译工具
- **dnSpy** - .NET 调试器和反编译器
- **JetBrains dotPeek** - 免费 .NET 反编译器
- **ILSpy** - 开源 .NET 反编译器

使用这些工具可以查看 `PawnKindDef` 的完整定义。

### RimWorld 源码
- **GitHub**: 搜索 RimWorld 相关仓库
- **RimWorld Wiki**: https://www.rimworldwiki.com/
- **Ludeon 论坛**: https://ludeon.com/forum/

## 📝 当前实现

我们的实现使用了反射访问：

```csharp
public static bool IsFactionLeader(PawnKindDef kindDef)
{
    if (kindDef == null) return false;

    if (factionLeaderField == null)
    {
        factionLeaderField = typeof(PawnKindDef).GetField("factionLeader", 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic);
    }

    if (factionLeaderField != null)
    {
        try
        {
            var value = factionLeaderField.GetValue(kindDef);
            if (value is bool boolValue)
            {
                return boolValue;
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"[FactionGearCustomizer] Failed to get factionLeader field for {kindDef.defName}: {ex.Message}");
        }
    }

    return false;
}
```

**特点**：
- ✅ 支持公共和私有字段
- ✅ 包含异常处理
- ✅ 缓存 FieldInfo 提高性能
- ✅ 类型安全检查

## ✅ 测试清单

启动游戏后，请验证以下内容：

- [ ] 日志中显示找到 `factionLeader` 字段
- [ ] 字段类型为 `System.Boolean`
- [ ] `Tribal_ChiefMelee` 的值为 `True`
- [ ] `Mercenary_Slasher` 的值为 `True`
- [ ] 普通兵种（如 `Tribal_Pen`）的值为 `False` 或 `null`
- [ ] UI 中正确显示派系领袖标识

## 🐛 故障排除

### 问题：日志显示未找到 factionLeader 字段

**解决方案**：
1. 检查日志中列出的所有相似字段名
2. 尝试不同的字段名（如 `isFactionLeader`、`leader` 等）
3. 检查是否应该使用 Property 而非 Field

### 问题：字段值为 null

**可能原因**：
- XML 未正确加载
- 该 PawnKindDef 没有设置 factionLeader
- 字段类型不是 bool

**解决方案**：
1. 确认测试的 PawnKindDef 在 XML 中确实设置了 `<factionLeader>true</factionLeader>`
2. 检查字段类型是否为 bool
3. 尝试其他已知有 factionLeader 的 PawnKindDef

### 问题：UI 中不显示派系领袖标识

**检查步骤**：
1. 确认日志中测试通过
2. 检查 `FactionGearEditor.IsFactionLeader()` 返回值
3. 查看 UI 代码是否正确调用了该方法
4. 检查翻译键 `FactionLeader` 是否存在

## 📚 参考资料

- RimWorld Def 系统文档
- C# 反射 API 文档
- XML 到 C# 对象映射机制

## 📞 支持

如有问题，请查看：
- 游戏日志文件
- Steam 创意工坊页面
- GitHub 仓库 Issues
- QQ 群：984338429
