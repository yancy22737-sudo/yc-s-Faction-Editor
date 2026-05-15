# 通用弹药兼容系统文档

## 📋 概述

本系统提供了一个**可扩展的架构**，用于支持各种 combat mod 的弹药系统。通过统一的接口和管理器，可以轻松地添加对新的 combat mod 的支持，而无需修改核心代码。

## 🏗️ 架构设计

### 核心组件

```
AmmoProviderManager (弹药提供者管理器)
    ↓
    注册和管理多个 IAmmoProvider 实现
    ↓
    ├── CEAmmoProvider (Combat Extended 支持)
    ├── ConfiguredAmmoProvider (XML 配置支持)
    └── CustomAmmoProvider (其他 combat mod)
```

### 设计模式

- **策略模式**：每个 `IAmmoProvider` 实现是一种弹药处理策略
- **工厂模式**：`AmmoProviderManager` 负责创建和管理提供者实例
- **观察者模式**：提供者可以动态注册和注销

## 📁 文件结构

```
FactionGearModification/
├── Compat/
│   ├── AmmoProviders/
│   │   ├── IAmmoProvider.cs              # 弹药提供者接口
│   │   ├── AmmoProviderManager.cs        # 弹药提供者管理器
│   │   ├── CEAmmoProvider.cs             # Combat Extended 实现
│   │   ├── ConfiguredAmmoProvider.cs     # XML 配置实现
│   │   └── CustomCombatModAmmoProvider.cs # 自定义实现模板
│   └── CECompat.cs                       # 向后兼容层
└── 1.6/Defs/AmmoMappings/
    └── AmmoMappings_Example.xml          # XML 配置示例
```

## 🔧 接口定义

### IAmmoProvider

```csharp
public interface IAmmoProvider
{
    string ProviderName { get; }                    // 提供者名称
    bool IsActive { get; }                          // 是否激活
    bool WeaponNeedsAmmo(ThingDef weaponDef);      // 武器是否需要弹药
    ThingDef GetDefaultAmmo(ThingDef weaponDef);   // 获取默认弹药
    List<ThingDef> GetAllAvailableAmmo(ThingDef weaponDef); // 获取所有弹药
    int GetSuggestedAmmoCount(ThingDef weaponDef, ThingDef ammoDef); // 建议数量
    string GetAmmoSetLabel(ThingDef weaponDef);    // 弹药组标签
    bool IsAmmo(ThingDef thingDef);                // 是否为弹药
}
```

## 🚀 使用方法

### 方法 1: 使用 XML 配置（推荐）

适用于大多数 combat mod，无需编写代码。

1. **创建 XML 配置文件**

在 `Defs/AmmoMappings/` 目录创建 XML 文件：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <FactionGearCustomizer.AmmoMappingDef>
    <defName>MyCombatMod_Mapping</defName>
    <modName>MyMod.MyCombatMod</modName>
    <providerName>My Combat Mod</providerName>
    
    <weaponAmmoMappings>
      <li>
        <weaponDefName>MyMod_AssaultRifle</weaponDefName>
        <ammoDefName>MyMod_Ammo_556mm</ammoDefName>
        <magazineSize>30</magazineSize>
        <availableAmmo>
          <li>MyMod_Ammo_556mm</li>
          <li>MyMod_Ammo_556mm_AP</li>
        </availableAmmo>
      </li>
    </weaponAmmoMappings>
    
    <ammoCategories>
      <li>MyMod_Ammo_556mm</li>
      <li>MyMod_Ammo_762mm</li>
    </ammoCategories>
  </FactionGearCustomizer.AmmoMappingDef>
</Defs>
```

2. **自动生效**

游戏启动时会自动加载配置并注册弹药提供者。

### 方法 2: 编写自定义提供者

适用于需要复杂逻辑的 combat mod。

1. **复制模板**

复制 `CustomCombatModAmmoProvider.cs` 并重命名。

2. **实现接口**

```csharp
public class MyCombatModAmmoProvider : IAmmoProvider
{
    public string ProviderName => "My Combat Mod";
    
    public bool IsActive => ModsConfig.IsActive("MyMod.MyCombatMod");
    
    public bool WeaponNeedsAmmo(ThingDef weaponDef)
    {
        // 实现你的检测逻辑
        return weaponDef.comps?.Any(c => c.GetType().Name == "MyAmmoComponent") ?? false;
    }
    
    public ThingDef GetDefaultAmmo(ThingDef weaponDef)
    {
        // 实现弹药获取逻辑
        // 返回弹药 ThingDef 或 null
    }
    
    // ... 实现其他方法
}
```

3. **注册提供者**

在 `AmmoProviderManager.Initialize()` 中添加：

```csharp
RegisterProvider(new MyCombatModAmmoProvider());
```

## 🎯 功能特性

### 1. 自动弹药添加

当用户添加武器时，系统会自动：
- 检测武器是否需要弹药
- 获取默认弹药类型
- 计算建议弹药数量
- 添加到物品清单

```csharp
// 使用示例
AmmoProviderManager.TryAutoAddAmmo(weaponDef, inventoryList);
```

### 2. 多 Mod 支持

系统会自动合并所有激活的弹药提供者：

```csharp
// 获取所有可用弹药
var allAmmo = AmmoProviderManager.GetAllAvailableAmmo(weaponDef);

// 获取所有弹药组标签（用于 UI 筛选）
var labels = AmmoProviderManager.GetAllAmmoSetLabels(weapons);
```

### 3. UI 筛选器集成

自动支持按弹药类型筛选武器：
- 在武器选择器中显示弹药筛选器
- 支持多选弹药类型
- 动态更新可用选项

### 4. 向后兼容

保留了 `CECompat` 类的所有公共方法，旧代码无需修改。

## 📝 配置说明

### AmmoMappingDef 字段

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| defName | string | 是 | 配置的唯一标识 |
| modName | string | 否 | 目标 Mod 的包名（用于检测激活状态） |
| providerName | string | 是 | 提供者显示名称 |
| weaponAmmoMappings | List | 是 | 武器弹药映射列表 |
| ammoCategories | List | 否 | 弹药类型列表（用于 UI 筛选） |
| defaultAmmoMultiplier | int | 否 | 默认弹药数量倍数（相对于弹匣容量） |

### WeaponAmmoMapping 字段

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| weaponDefName | string | 是 | 武器的 DefName |
| ammoDefName | string | 是 | 默认弹药的 DefName |
| magazineSize | int | 否 | 弹匣容量（默认 30） |
| availableAmmo | List | 否 | 所有可用弹药类型的 DefName 列表 |

## 🔍 调试和日志

系统会输出详细的日志信息：

```
[FactionGearCustomizer] Ammo Provider Manager initialized with 2 providers
[FactionGearCustomizer] Registered ammo provider: Combat Extended
[FactionGearCustomizer] Loaded configured ammo provider: My Combat Mod for mod: MyMod.MyCombatMod
[FactionGearCustomizer] Auto-added ammo 5.56x45mm NATO for weapon Assault Rifle
```

## ⚠️ 注意事项

1. **Mod 加载顺序**：确保本 Mod 在目标 combat mod 之后加载
2. **DefName 正确性**：XML 配置中的 DefName 必须准确
3. **性能考虑**：系统会自动缓存反射结果，避免重复查询
4. **空值检查**：所有方法都包含空值检查，不会导致崩溃

## 🎓 最佳实践

### 1. 优先使用 XML 配置

```
XML 配置 > 自定义代码
```

XML 配置更易于维护和分享。

### 2. 提供合理的默认值

```xml
<magazineSize>30</magazineSize>
<defaultAmmoMultiplier>3</defaultAmmoMultiplier>
```

### 3. 包含所有弹药类型

```xml
<availableAmmo>
  <li>Standard_Ammo</li>
  <li>AP_Ammo</li>
  <li>HP_Ammo</li>
</availableAmmo>
```

### 4. 测试验证

- 添加武器后检查弹药是否自动添加
- 验证弹药数量是否合理
- 测试 UI 筛选器是否正常工作

## 🔮 未来扩展

### 可能的改进方向

1. **运行时配置**：允许在游戏内动态调整弹药映射
2. **智能推荐**：根据武器属性自动推荐弹药类型
3. **社区配置库**：建立共享的 XML 配置仓库
4. **自动检测**：通过反射自动发现武器弹药关系

## 📞 支持

如有问题，请提供：
- 日志文件
- XML 配置文件
- 涉及的 Mod 列表
- 具体的错误信息

---

**最后更新**: 2026-02-28
**版本**: v1.0
