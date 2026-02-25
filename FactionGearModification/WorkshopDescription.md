# yc's Faction Editor / yc的派系编辑器

<p align="center">
  <b>一款功能强大的 RimWorld 派系编辑器</b><br>
  <i>A Powerful RimWorld Faction Editor</i>
</p>

<p align="center">
  <a href="#功能特性">功能特性</a> •
  <a href="#技术原理">技术原理</a> •
  <a href="#项目架构">项目架构</a> •
  <a href="#使用方法">使用方法</a> •
  <a href="#兼容性">兼容性</a>
</p>

---

## 🎯 简介

**yc's Faction Editor** 是一个 RimWorld 1.6 Mod，用于实时定制派系单位的装备生成逻辑。通过 Harmony Patch 技术拦截游戏原生 Pawn 生成流程，在不修改游戏本体的情况下实现装备、服装、健康状态的深度自定义。

### 主要功能
- 派系兵种装备定制（武器、服装、背包物品）
- 群组生成逻辑修改（袭击、商队、定居点）
- 健康状态与义体植入配置
- 异种人概率调整（Biotech DLC）
- 派系基础信息编辑（名称、图标、好感度）

---

## ✨ 功能特性

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

### 编辑器功能
- **三栏布局**：派系列表 → 兵种列表 → 配置面板
- **Mod 筛选**：按 Mod 来源筛选物品，支持多条件组合过滤
- **预设系统**：导出/导入 JSON 格式预设文件
- **实时预览**：无需生成事件即可预览配置效果
- **撤销重做**：支持操作历史回溯

---

## 🔧 技术原理

### 核心机制

本 Mod 通过 **Harmony Patch** 技术实现对 RimWorld 原生生成逻辑的拦截和修改：

#### 1. 装备注入点
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

#### 2. 数据持久化架构

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

#### 3. 装备应用流程

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

#### 4. 预算控制系统

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

#### 5. 科技等级限制

```csharp
private static bool IsTechLevelAllowed(ThingDef def, TechLevel? limit)
{
    if (!limit.HasValue) return true;
    TechLevel itemTechLevel = def.techLevel;
    if (itemTechLevel == TechLevel.Undefined) return true;
    return itemTechLevel <= limit.Value;  // 只允许低于或等于限制等级的物品
}
```

---

## 🏗️ 项目架构

### 目录结构

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
│   │   ├── FactionListPanel.cs
│   │   ├── KindListPanel.cs
│   │   ├── GearEditPanel.cs
│   │   └── ...
│   ├── Dialogs/                       # 对话框
│   │   ├── Dialog_PawnGroupGenerationPreview.cs
│   │   └── ...
│   └── Pickers/                       # 选择器
│       └── ThingPickerFilterBar.cs
│
├── IO/                             # 输入输出
│   └── PresetIOManager.cs             # 预设文件IO
│
├── Compat/                         # 兼容性
│   └── CECompat.cs                    # Combat Extended 兼容
│
└── About/
    └── About.xml                      # Mod 元数据
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

---

## 📖 使用指南

### 基础操作流程

1. 进入游戏，打开 Mod 设置（主菜单 → 选项 → Mod 设置）
2. 找到 **yc's Faction Editor** 的编辑器按钮
3. 在左侧选择你想要修改的 **派系**
4. 在中间列表选择具体的 **兵种**
5. 在右侧面板配置该兵种的 **武器、服装** 或 **健康状态**
6. 点击保存，配置即刻生效

### 详细配置说明

#### 武器配置
- **强制装备列表**：指定兵种必定携带的武器
- **品质设置**：可选 极差/较差/一般/良好/极佳/大师/传奇
- **材质选择**：如玻璃钢、铀、黄金等（取决于武器是否可由材料制作）
- **生物编码概率**：0-100%，决定武器是否绑定到特定角色

#### 服装配置
- **强制着装**：指定必须穿戴的装备
- **预算范围**：控制生成装备的市场价值范围
- **仅使用选定**：开启后移除该兵种默认装备，只使用自定义装备
- **强制裸体**：特殊选项，生成时不穿着任何服装

#### 健康状态配置
- **强制健康状态**：添加特定的 Hediff（如疾病、义体、成瘾等）
- **概率控制**：设置每个健康状态的出现概率
- **部位指定**：针对特定身体部位添加健康状态

#### 群组生成配置
- **袭击群组**：修改派系袭击时的兵种组成
- **商队群组**：修改派系商队的兵种和动物组成
- **定居点群组**：修改派系定居点的防御兵种

### 配置生效机制

| 操作类型 | 生效时机 | 影响范围 |
|----------|----------|----------|
| 保存配置 | 立即 | 新生成的单位 |
| 修改装备 | 保存后 | 下次生成的该兵种 |
| 修改群组 | 保存后 | 下次生成的群组 |
| 加载预设 | 加载后 | 新生成的单位 |

### 重要注意事项

1. **仅影响新生成单位**：已存在于地图上的单位不会受到影响
2. **存档安全**：配置保存在存档中，跨存档需要导出预设
3. **预算限制**：如果配置的装备超出预算，系统会自动跳过
4. **科技等级**：装备科技等级高于派系等级时可能被过滤（除非关闭限制）

---

## 🎮 游戏机制详解

### 装备生成流程

当游戏需要生成一个带有派系的单位时，以下流程会被触发：

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
[Harmony Patch] Postfix 拦截
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

### 各游戏场景生效说明

#### 1. 派系袭击 (Raid)
- **生效时机**：袭击事件生成时
- **影响对象**：袭击者中的所有单位
- **特殊说明**：包括空投袭击、步行袭击、机械族袭击

#### 2. 商队 (Caravan)
- **生效时机**：商队生成时
- **影响对象**：商人、守卫、驮运动物
- **特殊说明**：商队群组配置控制兵种组成

#### 3. 访客 (Visitor)
- **生效时机**：访客组生成时
- **影响对象**：访客及其护卫

#### 4. 任务相关
- **生效时机**：任务目标生成时
- **影响对象**：任务相关的所有派系单位
- **特殊说明**：包括救援任务、狩猎任务等

#### 5. 世界地图生成
- **生效时机**：新存档或派系定居点生成时
- **影响对象**：派系定居点的防御者

### 配置优先级

当多个配置可能同时生效时，系统按以下优先级处理：

```
优先级从高到低：

1. 预览模式配置 (PreviewPreset)
   └─ 用于实时预览功能

2. 存档特定配置 (Save-specific)
   └─ 存储在当前存档中的配置

3. 全局配置 (Global Settings)
   └─ Mod 设置中的默认配置

4. 原版默认 (Vanilla)
   └─ 游戏原生生成逻辑
```

### 数据存储位置

#### 存档内存储
```
SaveFile/
└── FactionGearGameComponent
    ├── factionGearPresets: List<FactionGearPreset>
    └── currentPresetIndex: int
```

#### 预设文件存储
```
RimWorld/
└── Mods/
    └── FactionGearCustomizer/
        └── Presets/
            ├── PresetName_1.json
            ├── PresetName_2.json
            └── ...
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

---

## 🤝 兼容性

- **现有存档**：可以安全地加入现有存档。配置仅影响新生成的单位
- **Harmony**：本 Mod 依赖 Harmony 库运行
- **Combat Extended (CE)**：完全兼容，自动为CE武器生成弹药
- **Total Control**：兼容
- **Biotech DLC**：支持异种人概率设置
- **Royalty DLC**：支持生物编码武器
- **Ideology DLC**：支持风格设置

---

## 🚀 更新计划

- [ ] **RimTalk 联动**：生成受派系背景和特定兵种信息影响的动态对话
- [ ] **更多派系选项**：全面修改派系名称、Logo 以及异种人出现概率
- [ ] **派系群组生成**：为派系袭击、援军或商队加入动物、机械族等特殊单位

---

## ⚠️ 常见问题

**Q: 兼容 Total Control 吗?**  
A: 是的。

**Q: 兼容 CE 吗？**  
A: 是的。

**Q: 支持 Mod 派系吗？**  
A: 理论上是的。

**Q: 这会影响已经存在的单位吗？**  
A: 不会。修改只会影响**新生成**的单位（如新刷新的袭击、援军或商队）。

**Q: 可以中途加入存档吗？**  
A: 没问题。

**Q: 如果我卸载 Mod 会怎样？**  
A: 只有 Mod 的配置数据会丢失，游戏存档通常是安全的，会报几行红字，不影响正常游戏。

---

## 📝 最近更新

### v1.2
- **多语言支持**：提供简体中文和英文

---

## 💡 灵感与推荐

本 Mod 的灵感来源于经典的 **Total Control**。  
如果你需要更全面、更底层的游戏机制修改（不仅限于装备），推荐订阅 **Total Control**。

---

## 🛠️ 开发信息

### 技术栈
- **语言**: C# (.NET Framework 4.8)
- **框架**: RimWorld 1.6 Modding API
- **依赖**: Harmony 2.x
- **补丁技术**: HarmonyLib Postfix/Prefix Patches

### 构建要求
- Visual Studio 2019+ 或 Rider
- RimWorld 1.6
- Harmony Mod

---

## 📄 许可证

本项目采用 MIT 许可证。

---

## 🙏 致谢

- **Ludeon Studios** - 创造了 RimWorld 这款伟大的游戏
- **pardeike** - 开发了 Harmony 补丁框架
- **Total Control** - 提供了最初的灵感

---

<p align="center">
  <b>Created by yancy</b><br>
  如果喜欢这个 Mod，请点赞收藏！如有 Bug 或建议，请在评论区留言。
</p>

<p align="center">
  <a href="https://github.com/yancy22737-sudo/FactionGearCustomizer">GitHub</a> •
  <a href="https://afdian.com/a/yancy12138">赞助我</a>
</p>
