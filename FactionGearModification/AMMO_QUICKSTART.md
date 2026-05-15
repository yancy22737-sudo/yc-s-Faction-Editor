# 快速参考指南 - 弹药兼容系统

## 🚀 快速开始

### 为你的 Combat Mod 添加弹药支持（3 步）

#### 步骤 1: 创建 XML 文件

在 Mod 的 `Defs/` 目录下创建文件：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <FactionGearCustomizer.AmmoMappingDef>
    <defName>MyMod_AmmoConfig</defName>
    <modName>MyMod.MyCombatMod</modName>
    <providerName>My Weapons</providerName>
    
    <weaponAmmoMappings>
      <li>
        <weaponDefName>MyGunName</weaponDefName>
        <ammoDefName>MyAmmoName</ammoDefName>
        <magazineSize>30</magazineSize>
      </li>
    </weaponAmmoMappings>
    
    <ammoCategories>
      <li>MyAmmoName</li>
    </ammoCategories>
  </FactionGearCustomizer.AmmoMappingDef>
</Defs>
```

#### 步骤 2: 获取 DefName

在游戏中查看物品的 DefName：
- 开启开发者模式
- 鼠标悬停在物品上
- 查看显示的信息

#### 步骤 3: 测试

- 启动游戏
- 打开 Faction Gear Customizer 编辑器
- 添加你的武器
- 检查是否自动添加了弹药

## 📋 模板速查

### 基础模板（单一弹药）

```xml
<FactionGearCustomizer.AmmoMappingDef>
  <defName>BasicConfig</defName>
  <modName>MyMod.Weapons</modName>
  <providerName>My Weapons</providerName>
  
  <weaponAmmoMappings>
    <li>
      <weaponDefName>AssaultRifle</weaponDefName>
      <ammoDefName>Ammo_556mm</ammoDefName>
      <magazineSize>30</magazineSize>
    </li>
  </weaponAmmoMappings>
  
  <ammoCategories>
    <li>Ammo_556mm</li>
  </ammoCategories>
</FactionGearCustomizer.AmmoMappingDef>
```

### 高级模板（多种弹药类型）

```xml
<FactionGearCustomizer.AmmoMappingDef>
  <defName>AdvancedConfig</defName>
  <modName>MyMod.CombatExtended</modName>
  <providerName>Combat Mod</providerName>
  
  <weaponAmmoMappings>
    <!-- 突击步枪 -->
    <li>
      <weaponDefName>Rifle_AR</weaponDefName>
      <ammoDefName>Ammo_556NATO</ammoDefName>
      <magazineSize>30</magazineSize>
      <availableAmmo>
        <li>Ammo_556NATO</li>
        <li>Ammo_556NATO_AP</li>
        <li>Ammo_556NATO_HP</li>
      </availableAmmo>
    </li>
    
    <!-- 狙击步枪 -->
    <li>
      <weaponDefName>Rifle_Sniper</weaponDefName>
      <ammoDefName>Ammo_308Win</ammoDefName>
      <magazineSize>5</magazineSize>
      <availableAmmo>
        <li>Ammo_308Win</li>
        <li>Ammo_308Win_AP</li>
      </availableAmmo>
    </li>
    
    <!-- 霰弹枪 -->
    <li>
      <weaponDefName>Shotgun</weaponDefName>
      <ammoDefName>Ammo_12Gauge</ammoDefName>
      <magazineSize>8</magazineSize>
      <availableAmmo>
        <li>Ammo_12Gauge</li>
        <li>Ammo_12Gauge_Slug</li>
      </availableAmmo>
    </li>
  </weaponAmmoMappings>
  
  <ammoCategories>
    <li>Ammo_556NATO</li>
    <li>Ammo_308Win</li>
    <li>Ammo_12Gauge</li>
    <li>Ammo_9mm</li>
    <li>Ammo_45ACP</li>
  </ammoCategories>
  
  <defaultAmmoMultiplier>3</defaultAmmoMultiplier>
</FactionGearCustomizer.AmmoMappingDef>
```

### 能量武器模板

```xml
<FactionGearCustomizer.AmmoMappingDef>
  <defName>EnergyWeaponsConfig</defName>
  <modName>MyMod.LaserGuns</modName>
  <providerName>Laser Weapons</providerName>
  
  <weaponAmmoMappings>
    <li>
      <weaponDefName>LaserRifle</weaponDefName>
      <ammoDefName>EnergyCell</ammoDefName>
      <magazineSize>50</magazineSize>
    </li>
    <li>
      <weaponDefName>PlasmaCaster</weaponDefName>
      <ammoDefName>MicrofusionCell</ammoDefName>
      <magazineSize>20</magazineSize>
    </li>
  </weaponAmmoMappings>
  
  <ammoCategories>
    <li>EnergyCell</li>
    <li>MicrofusionCell</li>
  </ammoCategories>
</FactionGearCustomizer.AmmoMappingDef>
```

## 🎯 常用字段说明

### weaponAmmoMappings 配置

| 字段 | 说明 | 示例 |
|------|------|------|
| weaponDefName | 武器的 DefName | `Gun_AssaultRifle` |
| ammoDefName | 默认弹药 DefName | `Ammo_556x45mm` |
| magazineSize | 弹匣容量 | `30` |
| availableAmmo | 可用弹药列表 | 见高级模板 |

### ammoCategories 配置

列出所有弹药类型，用于 UI 筛选：

```xml
<ammoCategories>
  <li>Ammo_556x45mm</li>
  <li>Ammo_762x39mm</li>
  <li>Ammo_9mm</li>
</ammoCategories>
```

## 🔧 代码 API 速查

### 检查武器是否需要弹药

```csharp
if (AmmoProviderManager.WeaponNeedsAmmo(weaponDef))
{
    // 需要弹药
}
```

### 获取默认弹药

```csharp
ThingDef ammo = AmmoProviderManager.GetDefaultAmmo(weaponDef);
```

### 获取所有可用弹药

```csharp
List<ThingDef> allAmmo = AmmoProviderManager.GetAllAvailableAmmo(weaponDef);
```

### 获取建议弹药数量

```csharp
int count = AmmoProviderManager.GetSuggestedAmmoCount(weaponDef, ammoDef);
```

### 自动添加弹药到配置

```csharp
AmmoProviderManager.TryAutoAddAmmo(weaponDef, inventoryList);
```

## ❓ 常见问题

### Q: 如何知道 Mod 的包名？

A: 查看 Steam 创意工坊页面或 Mod 的 About/Manifest.xml 文件

### Q: 多个武器使用同一种弹药怎么办？

A: 为每个武器都配置一个映射条目

### Q: 可以配置弹药不？

A: 可以，`ammoCategories` 是可选的

### Q: 配置不生效怎么办？

A: 检查：
1. XML 语法是否正确
2. DefName 是否准确
3. Mod 是否已激活
4. 查看日志文件

## 📞 需要帮助？

查看完整文档：`AMMO_SYSTEM_README.md`

---

**提示**: 建议先复制示例模板，然后根据需要修改。
