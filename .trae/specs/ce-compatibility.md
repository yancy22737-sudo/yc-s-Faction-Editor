# Combat Extended (CE) 兼容性开发计划

## 1. 背景与目标
**背景**：目前 Faction Gear Modification (FGM) 仅支持配置服装 (Apparel) 和武器 (Weapons)。Combat Extended (CE) 引入了弹药系统 (Ammo) 和负载系统 (Loadout/Bulk)，由于 FGM 无法生成弹药，导致生成的 Pawn 手持武器却无法射击。此外，FGM 的“强制清理”功能可能会意外移除 CE 生成的弹药。

**目标**：
1.  **核心目标**：在 FGM 中引入 **Inventory (物品清单)** 支持，允许用户配置任意 `ThingDef`（如弹药、药品、背包），并支持设置堆叠数量。
2.  **CE 增强**：提供针对 CE 的辅助功能，如自动匹配弹药、显示 Bulk/Mass 属性。
3.  **兼容性**：确保 FGM 的生成逻辑不被 CE 的机制覆盖或冲突。

## 2. 数据模型扩展 (Data Model)

### 2.1 SpecRequirementEdit 扩展
修改 `FactionGearCustomizer.SpecRequirementEdit` 类，支持堆叠数量。
*   **新增字段**：
    *   `public IntRange CountRange = new IntRange(1, 1);` (支持随机数量，如 20-50 发子弹)
*   **ExposeData**：更新 `Scribe` 逻辑以保存/加载 `countRange`。

### 2.2 KindGearData 扩展
修改 `FactionGearCustomizer.KindGearData` 类，添加 Inventory 数据容器。
*   **新增字段**：
    *   `public List<SpecRequirementEdit> InventoryItems = new List<SpecRequirementEdit>();`
*   **ExposeData**：更新 `Scribe` 逻辑。

## 3. UI 界面扩展 (UI Implementation)

### 3.1 新增 "Items" 标签页
在 `FactionGearMainTabWindow` 中新增一个 Tab (Items/Inventory)，位于 Weapons 和 Apparel 之间。
*   **功能**：
    *   列表显示当前配置的 Inventory Items。
    *   "Add Item" 按钮：打开物品选择器。
    *   "Edit" 按钮：配置数量范围 (`CountRange`)、材质、品质等。
*   **过滤器**：
    *   在物品选择器中，过滤掉 `Building`, `Filth`, `Mote` 等不可携带的 `ThingDef`。
    *   优先展示 `Category = Items` 或 `Category = Ammo` (如果存在)。

### 3.2 物品配置弹窗
扩展或复用现有的配置弹窗，增加 `IntRange` (Min/Max) 的输入控件，用于设置堆叠数量。

### 3.3 CE 属性显示 (条件性)
如果检测到 CE 加载：
*   在物品列表和选择器中，显示 Bulk (体积) 和 Mass (重量)。
*   在 Tooltip 中显示弹药的 Caliber (口径) 信息。

## 4. 核心逻辑实现 (Core Logic)

### 4.1 GearApplier 扩展
在 `GearApplier.ApplyCustomGear` 中添加 `ApplyInventory` 调用。

```csharp
private static void ApplyInventory(Pawn pawn, KindGearData kindData)
{
    // 1. 清理逻辑 (如果 ForceNaked/ForceOnlySelected)
    //    注意：Inventory 的清理需要小心，避免移除任务物品或关键道具。
    //    建议：仅当 ForceOnlySelected 时，清理 Inventory 中非 FGM 生成的物品。

    // 2. 生成逻辑
    foreach (var itemSpec in kindData.InventoryItems)
    {
        // 生成 Thing (使用 ThingMaker)
        // 设置 StackCount (基于 CountRange.RandomInRange)
        // 添加到 pawn.inventory.innerContainer
    }
}
```

### 4.2 CE 辅助工具类 (CE_CompatUtility)
创建一个独立的工具类 (或使用反射) 来处理 CE 相关逻辑，避免硬依赖。
*   `TryGetAmmoForWeapon(ThingDef weaponDef)`: 返回推荐的 `AmmoDef`。
*   `GetBulk(ThingDef def)`: 返回体积。

## 5. 开发步骤与任务分解

### Phase 1: 基础 Inventory 支持 (无 CE 依赖)
1.  [ ] **Data**: 修改 `SpecRequirementEdit` 和 `KindGearData`，添加字段和 Scribe 支持。
2.  [ ] **UI**: 在编辑器中添加 "Items" Tab 和基本的增删改查 UI。
3.  [ ] **Logic**: 实现 `GearApplier.ApplyInventory`。
4.  [ ] **Test**: 验证添加普通物品 (如 Medicine, Meals) 是否成功。

### Phase 2: 数量与堆叠控制
1.  [ ] **UI**: 在配置弹窗中添加 `CountRange` 滑块/输入框。
2.  [ ] **Logic**: 在生成时应用随机数量，并处理堆叠限制 (如果数量 > MaxStack，分多堆存放)。
3.  [ ] **Test**: 验证大量物品 (如 100 个 Gold) 是否正确分堆。

### Phase 3: CE 兼容性增强
1.  [ ] **Reflection/Integration**: 建立与 CE 的软连接 (检测 Mod ID)。
2.  [ ] **Ammo Helper**: 实现“添加武器时提示添加弹药”的功能 (可选：自动添加或弹窗询问)。
3.  [ ] **Stats**: 在 UI 中显示 CE 的 Bulk/Mass。
4.  [ ] **Test**: 启用 CE，验证生成的 Pawn 是否携带弹药并能射击。

## 6. 风险与注意事项
*   **Loadout 冲突**：CE 的 Loadout 系统可能会在 FGM 生成后再次检查并移除“多余”的物品。
    *   *缓解*：对于非玩家派系，通常不受 Loadout 严格限制。对于玩家派系，建议用户手动管理 Loadout。
*   **性能**：反射调用 CE 方法可能会有轻微性能损耗，应缓存结果。
*   **更新兼容性**：数据结构的改变需要确保旧存档/旧预设的兼容性 (Look with default value)。

## 7. 交付物
*   更新后的 DLL。
*   支持 Inventory 配置的新 UI。
*   (可选) 针对常见派系的 CE 预设文件。
