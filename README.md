# yc's Faction Editor

[![Steam Workshop](https://img.shields.io/badge/Steam-Workshop-blue?logo=steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=3670833973)
[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-blue.svg)](https://rimworldgame.com/)
[![Harmony](https://img.shields.io/badge/Dependency-Harmony-orange.svg)](https://github.com/pardeike/HarmonyRimWorld)

实时定制 RimWorld 派系单位的装备生成逻辑。

## 简介

本 Mod 通过 Harmony Patch 技术拦截 `PawnGenerator.GeneratePawn()` 方法，在不修改游戏本体的情况下，实现对派系单位装备、服装、健康状态的深度自定义。支持装备品质、材质、生物编码等高级设置，以及群组生成逻辑的全面定制。

## 功能

### 装备系统
| 功能 | 说明 |
|------|------|
| 武器定制 | 强制装备指定武器，支持品质、材质、生物编码设置 |
| 服装定制 | 强制着装指定装备，支持预算控制、颜色设置 |
| 背包物品 | 添加特定物品到单位背包，支持堆叠数量配置 |
| 健康状态 | 添加 Hediff（疾病、义体、成瘾等），支持概率和部位控制 |

### 群组系统
| 功能 | 说明 |
|------|------|
| 袭击群组 | 修改 Raid 事件的兵种组成和权重 |
| 商队群组 | 修改 Trader Caravan 的商人、守卫、驮运动物组成 |
| 定居点群组 | 修改派系定居点的防御兵种配置 |
| 访客群组 | 修改 Visitor 事件的兵种组成 |

### 派系系统
| 功能 | 说明 |
|------|------|
| 基础信息 | 修改派系名称、描述、图标、颜色 |
| 好感度 | 覆盖派系与玩家的默认关系 |
| 异种人 | 配置 Biotech DLC 异种人出现概率 |
| 新派系 | 基于模板创建新派系，支持地图定居点生成 |

## 安装

1. 订阅 [Harmony](https://steamcommunity.com/workshop/filedetails/?id=2009463077)
2. 订阅本 Mod
3. 在 Mod 列表中确保 Harmony 排在第一位，本 Mod 排在后面

## 使用指南

### 基础操作

1. 启动游戏，进入 Mod 设置（主菜单 → 选项 → Mod 设置）
2. 选择 **yc's Faction Editor**
3. 在左侧面板选择要修改的派系
4. 在中间面板选择具体兵种
5. 在右侧面板配置装备、服装或健康状态
6. 点击保存

### 武器配置

```
武器面板
├── 强制装备列表
│   └── 添加/移除特定武器
├── 品质设置
│   └── 极差/较差/一般/良好/极佳/大师/传奇
├── 材质选择
│   └── 玻璃钢/铀/黄金等（取决于武器是否可由材料制作）
└── 生物编码概率
    └── 0-100%，决定武器是否绑定到特定角色
```

### 服装配置

```
服装面板
├── 强制着装
│   └── 指定必须穿戴的装备
├── 预算范围
│   └── 控制生成装备的市场价值范围
├── 仅使用选定
│   └── 开启后移除默认装备，只使用自定义装备
└── 强制裸体
    └── 生成时不穿着任何服装
```

### 健康状态配置

```
健康状态面板
├── 强制健康状态
│   └── 添加特定的 Hediff
├── 概率控制
│   └── 设置每个健康状态的出现概率
└── 部位指定
    └── 针对特定身体部位添加健康状态
```

### 群组生成配置

```
群组面板
├── 袭击群组
│   └── 修改派系袭击时的兵种组成
├── 商队群组
│   └── 修改派系商队的兵种和动物组成
└── 定居点群组
    └── 修改派系定居点的防御兵种
```

## UI 界面指南

### 主界面布局

```
┌─────────────────────────────────────────────────────────────┐
│  [保存] [撤销] [重做] [预览] [预设管理] [帮助]               │  ← 顶部工具栏
├──────────────┬────────────────┬─────────────────────────────┤
│              │                │                             │
│  派系列表    │   兵种列表     │      配置编辑面板           │
│              │                │                             │
│  [帝国]      │   [骑兵]       │  ┌─────────────────────┐    │
│  [海盗]      │   [枪手]       │  │ 武器配置            │    │
│  [部落]      │   [首领]       │  ├─────────────────────┤    │
│              │                │  │ 服装配置            │    │
│  [新建派系]  │   [添加兵种]   │  ├─────────────────────┤    │
│              │                │  │ 健康状态            │    │
│              │                │  ├─────────────────────┤    │
│              │                │  │ 群组生成            │    │
│              │                │  └─────────────────────┘    │
├──────────────┴────────────────┴─────────────────────────────┤
│  状态栏: 当前预设 | 修改状态 | 提示信息                      │
└─────────────────────────────────────────────────────────────┘
```

### 派系列表面板

**功能**：浏览和选择游戏中的所有派系

| 元素 | 说明 |
|------|------|
| 派系名称 | 显示派系标签，已修改的派系会显示标记 |
| 派系颜色 | 显示派系代表色 |
| 新建派系按钮 | 基于模板创建新派系（仅游戏中可用） |
| 搜索框 | 按名称快速筛选派系 |

**操作**：
- 左键点击：选择派系
- 右键点击：打开派系编辑菜单（修改名称、图标等）

### 兵种列表面板

**功能**：显示选中派系的所有可用兵种

| 元素 | 说明 |
|------|------|
| 兵种名称 | 显示 PawnKindDef 标签 |
| 添加按钮 | 向当前派系添加新的兵种（仅游戏中可用） |
| 数量显示 | 显示该兵种在当前配置中的数量 |

**操作**：
- 左键点击：选择兵种进行配置
- 右键点击：复制/粘贴配置、重置为默认

### 配置编辑面板

#### 简单模式
适合快速配置常用选项：
- 快速添加武器/服装
- 设置基础品质
- 调整预算范围

#### 高级模式
适合精细控制所有参数：
- 特定装备槽位配置
- 材质和颜色精确设置
- 复杂的概率控制
- 多池随机选择

### 顶部工具栏

| 按钮 | 功能 | 快捷键 |
|------|------|--------|
| 保存 | 保存当前所有修改到预设 | Ctrl+S |
| 撤销 | 撤销上一步操作 | Ctrl+Z |
| 重做 | 重做已撤销的操作 | Ctrl+Y |
| 预览 | 打开预览窗口测试配置 | - |
| 预设管理 | 导入/导出/切换预设 | - |
| 帮助 | 打开帮助文档 | F1 |

### 常用对话框

#### 物品选择器
```
┌─────────────────────────────────────┐
│  [搜索...] [Mod筛选▼] [类型筛选▼]   │
├─────────────────────────────────────┤
│  ┌─────┐ ┌─────┐ ┌─────┐           │
│  │物品A│ │物品B│ │物品C│ ...        │  ← 图标网格
│  └─────┘ └─────┘ └─────┘           │
├─────────────────────────────────────┤
│  选中: 物品A                        │
│  品质: [一般▼] 材质: [钢铁▼]         │
│  生物编码: [ ] 概率: [50%]          │
├─────────────────────────────────────┤
│           [确认] [取消]             │
└─────────────────────────────────────┘
```

**筛选功能**：
- **Mod 筛选**：只显示特定 Mod 的物品
- **科技等级筛选**：按 TechLevel 过滤
- **类型筛选**：武器/服装/物品分类
- **文本搜索**：按名称快速查找

#### 预设管理器
```
┌─────────────────────────────────────┐
│  预设管理                           │
├─────────────────────────────────────┤
│  ┌─────────────────────────────┐    │
│  │ ★ 当前预设                  │    │
│  │   预设1                     │    │
│  │   预设2                     │    │
│  └─────────────────────────────┘    │
├─────────────────────────────────────┤
│  [新建] [重命名] [复制] [删除]      │
│  [导出到文件] [从文件导入]          │
└─────────────────────────────────────┘
```

## 工作流指南

### 基础工作流：修改单个兵种装备

```
1. 选择目标派系
   └─ 在左侧面板点击派系名称
   
2. 选择目标兵种
   └─ 在中间面板点击兵种名称
   
3. 配置武器
   └─ 点击"添加武器"按钮
   └─ 在物品选择器中选择武器
   └─ 设置品质、材质、生物编码
   
4. 配置服装（可选）
   └─ 点击"添加服装"按钮
   └─ 选择装备部位和具体物品
   
5. 保存配置
   └─ 点击顶部"保存"按钮
   
6. 测试（可选）
   └─ 点击"预览"按钮生成测试单位
```

### 高级工作流：创建完整的派系配置

```
1. 创建新预设
   └─ 预设管理 → 新建
   └─ 输入预设名称
   
2. 配置基础派系
   └─ 选择原版派系作为模板
   └─ 修改派系名称、描述、图标
   
3. 配置核心兵种
   └─ 对每个关键兵种：
      ├─ 设置武器配置
      ├─ 设置服装配置
      ├─ 设置健康状态（如义体）
      └─ 复制配置到相似兵种
   
4. 配置群组生成
   └─ 修改袭击群组组成
   └─ 修改商队群组组成
   
5. 导出预设
   └─ 预设管理 → 导出到文件
   └─ 分享给其他玩家
```

### 批量工作流：快速配置多个兵种

```
1. 配置源兵种
   └─ 选择一个兵种完成完整配置
   
2. 复制配置
   └─ 右键点击源兵种 → 复制配置
   
3. 批量粘贴
   └─ 选择多个目标兵种（Ctrl+点击）
   └─ 右键 → 粘贴配置
   
4. 微调
   └─ 对每个兵种进行个别调整
   
5. 保存
```

## 最佳实践

### 配置管理

1. **预设命名规范**
   - 使用描述性名称：`中世纪强化版`、`未来战争`
   - 包含版本号：`我的配置_v1.2`
   - 避免特殊字符

2. **定期备份**
   - 重要配置导出为文件备份
   - 在重大修改前创建副本预设

3. **模块化配置**
   - 按派系创建独立预设
   - 使用复制粘贴复用相似配置

### 性能优化

1. **避免过度配置**
   - 不需要修改的兵种保持默认
   - 避免为每个兵种添加过多装备选项

2. **预算控制**
   - 合理设置预算范围
   - 避免配置超出派系科技等级的装备

3. **缓存利用**
   - 界面会自动缓存派系和兵种数据
   - 频繁切换时无需重复加载

### 兼容性建议

1. **与 Combat Extended 配合**
   - 本 Mod 自动处理 CE 弹药
   - 配置 CE 武器时无需额外设置弹药

2. **与 Total Control 配合**
   - 两个 Mod 可同时使用
   - 注意配置优先级，后加载的 Mod 生效

3. **存档迁移**
   - 跨存档使用需导出预设文件
   - 不同版本 RimWorld 可能存在兼容性问题

### 调试技巧

1. **使用预览功能**
   - 在保存前预览配置效果
   - 多次生成查看随机性表现

2. **检查日志**
   - 开启开发者模式查看详细日志
   - 搜索 `[FactionGearCustomizer]` 标签

3. **逐步测试**
   - 先配置单个兵种测试
   - 确认无误后再批量应用

## 技术原理

### 核心机制

本 Mod 通过 **Harmony Patch** 技术实现对 RimWorld 原生生成逻辑的拦截和修改：

#### 装备注入点
```csharp
[HarmonyPatch(typeof(PawnGenerator), "GeneratePawn")]
[HarmonyPriority(Priority.Last)]
public static class Patch_GeneratePawn
{
    public static void Postfix(Pawn __result, PawnGenerationRequest request)
    {
        // 在 pawn 生成后，根据配置应用自定义装备
        GearApplier.ApplyCustomGear(__result, faction);
    }
}
```

**原理说明**：
- 使用 Harmony 的 `Postfix` 补丁在 `PawnGenerator.GeneratePawn()` 方法执行后介入
- 通过 `Priority.Last` 确保在其他 Mod 之后执行，获得最终控制权
- 利用 `ThreadStatic` 标志防止递归调用

#### 数据持久化架构

```
存档数据结构:
├── FactionGearPreset (预设)
│   ├── factionGearData: List<FactionGearData>
│   └── version: string
│
└── FactionGearData (派系数据)
    ├── factionDefName: string
    ├── kindGearData: List<KindGearData>
    ├── XenotypeChances: Dictionary<string, float>
    ├── groupMakers: List<PawnGroupMakerData>
    └── 派系编辑字段 (Label, Description, IconPath, Color)
```

#### 装备应用流程

```
Pawn 生成
    ↓
Patch_GeneratePawn.Postfix()
    ↓
GearApplier.ApplyCustomGear()
    ↓
├─ ApplyWeapons()      → 应用武器配置
├─ ApplyApparel()      → 应用服装配置
├─ ApplyInventory()    → 应用背包物品
└─ ApplyHediffs()      → 应用健康状态/义体
```

#### 预算控制系统

```csharp
// 预算检查逻辑
float budget = kindData.ApparelMoney?.RandomInRange ?? pawn.kindDef?.apparelMoney.RandomInRange ?? 0f;
float currentSpent = pawn.apparel.WornApparel.Sum(a => a.MarketValue);

if (currentSpent + app.MarketValue > budget)
{
    // 超出预算，跳过该物品
    created.Destroy();
    continue;
}
```

#### 科技等级限制

```csharp
private static bool IsTechLevelAllowed(ThingDef def, TechLevel? limit)
{
    if (!limit.HasValue) return true;
    TechLevel itemTechLevel = def.techLevel;
    if (itemTechLevel == TechLevel.Undefined) return true;
    return itemTechLevel <= limit.Value;  // 只允许低于或等于限制等级的物品
}
```

## 游戏机制

### 装备生成流程

```
游戏事件触发
    │
    ├─ 袭击生成 (Raid)
    ├─ 商队生成 (Caravan)
    ├─ 访客生成 (Visitor)
    ├─ 任务生成 (Quest)
    └─ 其他事件
    │
    ▼
PawnGenerator.GeneratePawn()
    │
    ▼
[Harmony Postfix] Patch_GeneratePawn
    │
    ▼
GearApplier.ApplyCustomGear()
    │
    ├─ 检查是否存在自定义配置
    ├─ 检查兵种匹配
    └─ 应用配置
        │
        ├─ ApplyWeapons()    → 替换/添加武器
        ├─ ApplyApparel()    → 替换/添加服装
        ├─ ApplyInventory()  → 添加背包物品
        └─ ApplyHediffs()    → 添加健康状态
```

### 配置生效时机

| 游戏场景 | 生效时机 | 影响对象 |
|----------|----------|----------|
| 派系袭击 | 袭击事件生成时 | 所有袭击者 |
| 商队 | 商队生成时 | 商人、守卫、驮运动物 |
| 访客 | 访客组生成时 | 访客及其护卫 |
| 任务 | 任务目标生成时 | 任务相关的派系单位 |
| 定居点 | 新存档或定居点生成时 | 派系定居点的防御者 |

### 配置优先级

```
1. 预览模式配置 (PreviewPreset)
   └─ 用于实时预览功能

2. 存档特定配置 (Save-specific)
   └─ 存储在当前存档中的配置

3. 全局配置 (Global Settings)
   └─ Mod 设置中的默认配置

4. 原版默认 (Vanilla)
   └─ 游戏原生生成逻辑
```

### 数据存储

**存档内存储**
```
SaveFile/
└── FactionGearGameComponent
    ├── factionGearPresets: List<FactionGearPreset>
    └── currentPresetIndex: int
```

**预设文件存储**
```
RimWorld/
└── Mods/
    └── FactionGearCustomizer/
        └── Presets/
            ├── PresetName_1.json
            └── PresetName_2.json
```

### 冲突解决机制

当多个 Mod 同时修改同一单位时：

1. **Harmony 优先级**：本 Mod 使用 `Priority.Last` 确保最后执行
2. **装备覆盖策略**：后应用的配置会覆盖先应用的配置
3. **合并策略**：部分配置（如背包物品）采用追加而非替换

### 性能考虑

- **缓存机制**：派系和兵种数据在首次访问后缓存
- **延迟加载**：配置数据按需加载，不占用启动时间
- **字典索引**：使用字典优化兵种数据查询，O(1) 复杂度

## 兼容性

| Mod/DLC | 状态 | 说明 |
|---------|------|------|
| Harmony | 必需 | 本 Mod 依赖 Harmony 库 |
| Combat Extended | 兼容 | 自动为 CE 武器生成弹药 |
| Total Control | 兼容 | 功能有重叠，可同时使用 |
| Biotech DLC | 支持 | 异种人概率设置 |
| Royalty DLC | 支持 | 生物编码武器 |
| Ideology DLC | 支持 | 风格设置 |

## 技术信息

### 技术栈
- **语言**: C# (.NET Framework 4.8)
- **框架**: RimWorld 1.6 Modding API
- **依赖**: Harmony 2.x
- **补丁**: HarmonyLib Postfix/Prefix

### 项目结构
```
FactionGearModification/
├── Core/                           # 核心模块
│   ├── FactionGearCustomizerMod.cs    # Mod 入口类
│   ├── FactionGearCustomizerSettings.cs # 设置管理
│   ├── FactionGearGameComponent.cs    # 游戏组件（存档数据）
│   ├── Startup.cs                     # 启动逻辑
│   └── ModVersion.cs                  # 版本信息
│
├── Data/                           # 数据模型
│   ├── FactionGearData.cs             # 派系装备数据
│   ├── KindGearData.cs                # 兵种装备数据
│   ├── PawnGroupData.cs               # 群组生成数据
│   ├── GearItem.cs                    # 物品定义
│   ├── ForcedHediff.cs                # 强制健康状态
│   ├── SpecRequirementEdit.cs         # 特定需求编辑
│   └── Enums.cs                       # 枚举定义
│
├── Managers/                       # 管理器
│   ├── FactionGearManager.cs          # 装备管理器
│   ├── FactionDefManager.cs           # 派系定义管理器
│   ├── GearApplier.cs                 # 装备应用器（核心逻辑）
│   ├── FactionSpawnManager.cs         # 派系生成管理器
│   ├── PresetFactionImporter.cs       # 预设导入器
│   ├── UndoManager.cs                 # 撤销管理器
│   └── LanguageManager.cs             # 多语言管理器
│
├── Patches/                        # Harmony 补丁
│   ├── Patch_GeneratePawn.cs          # 核心：Pawn生成补丁
│   ├── Patch_GameInit.cs              # 游戏初始化
│   ├── Patch_FactionDef_FactionIcon.cs # 派系图标补丁
│   └── Patch_FloatMenuMakerWorld.cs   # 世界地图菜单补丁
│
├── UI/                             # 用户界面
│   ├── FactionGearMainTabWindow.cs    # 主窗口
│   ├── FactionGearEditor.cs           # 编辑器核心
│   ├── Panels/                        # 面板组件
│   ├── Dialogs/                       # 对话框
│   └── Pickers/                       # 选择器
│
├── IO/                             # 输入输出
│   └── PresetIOManager.cs             # 预设文件IO
│
└── Compat/                         # 兼容性
    └── CECompat.cs                    # Combat Extended 兼容
```

### 关键类说明

| 类名 | 职责 | 核心方法 |
|------|------|----------|
| `FactionGearCustomizerMod` | Mod 入口 | `PatchAllSafely()`, `DoSettingsWindowContents()` |
| `Patch_GeneratePawn` | 装备注入点 | `Postfix()` |
| `GearApplier` | 装备应用逻辑 | `ApplyCustomGear()`, `ApplyWeapons()`, `ApplyApparel()` |
| `FactionGearData` | 派系数据容器 | `GetOrCreateKindData()`, `ExposeData()` |
| `KindGearData` | 兵种数据容器 | `DeepCopy()`, `ResetToDefault()` |
| `FactionGearEditor` | UI 编辑器核心 | `DrawEditor()`, `InitializeWorkingSettings()` |

## 常见问题

**Q: 会影响已存在的单位吗？**  
A: 不会。只影响配置保存后新生成的单位。

**Q: 可以中途加入存档吗？**  
A: 可以。

**Q: 卸载后会怎样？**  
A: Mod 配置数据丢失，游戏存档安全，可能显示少量红字错误。

**Q: 支持 Mod 派系吗？**  
A: 支持。

## 更新日志

### v1.2
- 新增派系修改功能
- 新增群组修改功能
- 新增兵种修改功能
- 新增异种人生成概率设置
- 优化存档逻辑
- 修复若干 Bug

### v1.1
- 多语言支持（简体中文/英文）

## 链接

- [GitHub](https://github.com/yancy22737-sudo/FactionGearCustomizer)
- [爱发电](https://afdian.com/a/yancy12138)

## 致谢

- [Ludeon Studios](https://ludeon.com/) - RimWorld
- [pardeike](https://github.com/pardeike) - Harmony
- Total Control - 灵感来源
