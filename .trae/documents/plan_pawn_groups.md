# Pawn Groups 界面与逻辑分析及修复计划

## 现状分析
通过对代码的审查，发现 Pawn Groups 模块存在以下问题：

### 1. 功能缺失
*   **无法编辑 `maxTotalPoints`**: `PawnGroupMakerData` 中包含 `maxTotalPoints` 字段，但在 `GroupListPanel` 和 `Dialog_EditPawnGroup` 中均未提供编辑接口。这导致用户无法限制袭击或商队的总点数。
*   **`disallowFood` 字段未生效**: 在 `PawnGroupData.cs` 中，`disallowFood` 的赋值逻辑被注释掉了，导致该设置无法应用到游戏中。

### 2. UI/UX 问题
*   **固定滚动高度**: `Dialog_EditPawnGroup.cs` 中 ScrollView 的高度被硬编码为 `1500f`。
    *   **后果**: 如果内容较少，会出现大量空白滚动区域；如果内容过多（如添加了大量 Pawn 种类），底部内容会被截断无法访问。
*   **Pawn 选择器信息不足**: `Dialog_PawnKindPicker` 仅显示 Pawn 的名称。用户在构建袭击组时，通常需要知道 Pawn 的 `CombatPower` (战斗力) 以便平衡强度，目前缺乏此信息。

### 3. 代码结构
*   `Dialog_PawnKindPicker` 类定义在 `Dialog_EditPawnGroup.cs` 文件内部，建议分离以提高可维护性。

## 修复计划

### 步骤 1: 修复数据模型与逻辑
*   检查 `PawnGroupData.cs`，恢复 `disallowFood` 的逻辑（如果确认为需要的功能）。

### 步骤 2: 改进 `Dialog_EditPawnGroup`
*   **动态高度计算**: 移除 `1500f` 的硬编码高度。改为在绘制过程中记录 `curY`，或者使用 `Listing_Standard.CurHeight` 来动态计算所需的 ViewRect 高度。
*   **添加字段编辑**: 在列表上方添加 `maxTotalPoints` (NumericField) 和 `disallowFood` (Checkbox) 的编辑控件。

### 步骤 3: 增强 `Dialog_PawnKindPicker`
*   将 `Dialog_PawnKindPicker` 提取为独立文件 `UI/Dialog_PawnKindPicker.cs`。
*   在列表中显示 Pawn 的 `CombatPower`，并支持按战斗力排序。
*   显示 Pawn 的 Race (种族) 信息，以便区分不同种族的同名单位。

### 步骤 4: 验证
*   编译并在游戏中测试：
    *   打开 Faction 编辑窗口 -> Groups 标签。
    *   编辑一个 Group，检查是否能修改 Max Points。
    *   添加大量 Pawn，检查滚动条是否正常。
    *   打开 Pawn 选择器，检查是否显示战斗力。
