# 派系实例化与据点生成优化计划

## 1. 目标
重构并增强现有的派系生成与据点生成功能，提供更灵活的生成选项（参数化随机生成、手动指定），并确保逻辑符合原版游戏机制，保障存档安全。

## 2. 核心分析

### 2.1 现有问题
*   **据点生成逻辑简陋**：目前的 `SpawnRandomSettlement` 仅使用 `Rand.Range` 随机选点，未进行生物群系（Biome）检查（可能生成在海里）、未检查可通行性、未考虑与玩家或其他据点的距离。
*   **缺乏参数化控制**：无法指定生成据点的数量，也无法设置距离限制（如"距离玩家基地至少 X 格"）。
*   **关系未初始化**：新生成的派系可能未正确初始化与其他派系的外交关系，导致行为异常。
*   **存档安全性**：需要确保自定义 `FactionDef` 在游戏加载前稳定注入，且生成的 `Faction` 实例数据完整。

### 2.2 原生机制参考
*   **地块选择**：应使用 `TileFinder.RandomSettlementTileFor` 或 `TileFinder.IsValidTileForNewSettlement` 来确保地块合法性。
*   **派系关系**：使用 `faction.TryMakeInitialRelationsWith` 初始化关系。
*   **世界对象**：使用 `WorldObjectMaker` 创建 `Settlement`。

## 3. 实施方案

### 3.1 核心管理器重构 (`FactionSpawnManager.cs`)

#### A. 引入高级地块搜索算法
实现一个新的搜索方法，支持以下约束：
*   **数量 (Count)**：尝试寻找 N 个符合条件的地块。
*   **最小距离 (Min Distance)**：距离玩家现有据点（或任意指定中心点）的最小 Tile 距离。
*   **生物群系检查**：排除海洋、冰盖等不可居住区域（除非派系特性允许）。

```csharp
public static bool TryFindSettlementTiles(int count, int minDistanceToPlayer, Faction faction, out List<int> tiles)
```

#### B. 完善派系实例化
在 `SpawnFactionInstance` 中增加：
*   **关系生成**：调用 `faction.TryMakeInitialRelationsWith(Faction.OfPlayer)` 和其他派系。
*   **领袖生成**：确保派系生成后有领袖（如果需要）。

#### C. 存档安全保障
*   **Def 注入时机**：确认 `InjectCustomFactions` 在 `Mod.ctor` 或 `Startup` 中调用，确保在加载存档前 Def 已存在。
*   **数据清理**：在移除自定义派系时，提供清理存档中残留 WorldObject 的选项（虽然通常不建议删除已生成的派系，但可以禁止其生成新据点）。

### 3.2 UI 交互设计

#### A. 新增据点生成配置窗口 (`Dialog_SpawnSettlements`)
当用户点击“生成据点”时，弹出一个配置窗口，包含两个选项卡或模式：

**模式 1：参数化随机生成**
*   **据点数量**：滑块/输入框（例如 1-50）。
*   **最小距离**：滑块（例如 0-100 Tiles，0 为不限制）。
*   **操作按钮**：[生成] - 调用批量生成逻辑。

**模式 2：手动指定 (World Targeter)**
*   **说明文本**：提示用户将切换到世界地图进行点击。
*   **操作按钮**：[选择位置] - 调用 `BeginTargetingForSettlement`。

#### B. 改进手动选点逻辑
优化 `BeginTargetingForSettlement` 中的验证器：
*   使用 `TileFinder.IsValidTileForNewSettlement` 替代简单的 `AnyWorldObjectAt` 检查。
*   在鼠标悬停时显示地块是否合法的提示。

## 4. 任务分解

### 阶段 1：核心逻辑增强 (Core Logic)
- [ ] 修改 `FactionSpawnManager.cs`，引入 `TileFinder` 相关逻辑。
- [ ] 实现 `TryFindSettlementTiles` 方法，支持距离和数量过滤。
- [ ] 优化 `SpawnSettlement` 方法，确保名称生成和派系关联正确。
- [ ] 增加 `GenerateFactionRelations` 辅助方法。

### 阶段 2：UI 实现 (UI Implementation)
- [ ] 创建 `Dialog_SpawnSettlements` 类。
- [ ] 实现随机生成参数配置界面（数量、距离）。
- [ ] 实现手动选点模式的入口。
- [ ] 优化 `BeginTargetingForSettlement` 的反馈体验。

### 阶段 3：存档安全与验证 (Validation)
- [ ] 验证 Def 注入流程，确保重启游戏后自定义派系不丢失。
- [ ] 测试生成大量据点时的性能。
- [ ] 测试距离限制是否生效。
- [ ] 验证新生成派系与玩家的初始关系。

## 5. 验证计划
*   **功能测试**：在开发者模式下生成 10 个据点，检查是否避开了海洋和玩家基地附近。
*   **存档测试**：生成派系和据点后保存，重启游戏加载，检查据点是否依然存在且归属正确。
*   **交互测试**：手动选点时，尝试点击非法区域（如海面），检查是否有正确提示。
