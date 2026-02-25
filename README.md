# yc's Faction Editor

[![Steam Workshop](https://img.shields.io/badge/Steam-Workshop-blue?logo=steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=3670833973)
[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-blue.svg)](https://rimworldgame.com/)
[![Harmony](https://img.shields.io/badge/Dependency-Harmony-orange.svg)](https://github.com/pardeike/HarmonyRimWorld)

实时定制 RimWorld 派系单位的装备生成逻辑。

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
├── Core/              # Mod 入口、设置、游戏组件
├── Data/              # 数据模型
├── Managers/          # 管理器
├── Patches/           # Harmony 补丁
├── UI/                # 用户界面
├── IO/                # 输入输出
└── Compat/            # 兼容性
```

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

