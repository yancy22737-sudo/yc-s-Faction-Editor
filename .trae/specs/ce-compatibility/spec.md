# [Combat Extended Compatibility] Spec

## Why
Combat Extended (CE) 是一款极大地改变了 RimWorld 战斗机制的模组，引入了新的弹药系统、护甲机制、负重计算等。为了让 Faction Gear Customizer 能够在使用 CE 的环境下正常工作，并正确处理装备的 CE 相关属性，必须添加针对性的兼容性补丁。

## What Changes
- 添加检测 Combat Extended 模组是否激活的逻辑。
- 创建独立的 CE 兼容模块，封装所有与 CE 相关的逻辑和补丁。
- 在模组初始化阶段，根据检测结果决定是否加载 CE 兼容模块。
- 确保装备生成的 CE 属性（如 Bulk, Mass, AmmoSet 等）被正确处理或保留。

## Impact
- **Affected specs**: 装备生成与属性修改逻辑。
- **Affected code**: 模组初始化入口 (Mod/Startup), 装备生成相关类。

## ADDED Requirements
### Requirement: CE Detection
系统必须能够检测当前游戏环境中是否加载了 Combat Extended 模组。

#### Scenario: CE Active
- **WHEN** 游戏启动且加载了 Combat Extended。
- **THEN** 模组初始化流程中会调用 CE 兼容模块的初始化方法。

#### Scenario: CE Inactive
- **WHEN** 游戏启动且未加载 Combat Extended。
- **THEN** 模组按原逻辑初始化，不加载任何 CE 相关代码，避免报错。

### Requirement: CE Patch Application
如果检测到 CE，系统必须应用必要的 Harmony 补丁或逻辑调整，以确保：
1. 生成的装备拥有正确的 CE 属性（Bulk, Mass, Armor penetration 等）。
2. 避免与 CE 自身的机制发生冲突。

## MODIFIED Requirements
### Requirement: Gear Generation
原有的装备生成逻辑需要调整，以便在 CE 模式下能够正确读取和应用 CE 的 Def 属性。
