# P1 问题完整修复方案

> 文档版本：1.0  
> 创建日期：2026-04-10  
> 适用范围：FactionGearModification v1.5.3+

---

## 概述

本文档提供 OPTIMIZATION_REPORT.md 中识别的 **P1 重要问题** 的完整修复方案。P1 问题包括架构债务、跨层依赖、上帝类文件等结构性问题，需要分阶段实施重构。

---

## P1 问题清单

| # | 问题 | 严重程度 | 预计工作量 |
|---|------|---------|-----------|
| P1-1 | GearApplier.cs 2608 行 | 极严重 | 3 天 |
| P1-2 | GearEditPanel.cs 1760 行 | 极严重 | 2 天 |
| P1-3 | EditorSession 40+ 公开可变静态字段 | 极高 | 2 天 |
| P1-4 | Patch_GeneratePawn 包含大量业务逻辑 | 高 | 1 天 |
| P1-5 | Patch_GameInit 包含生命周期管理逻辑 | 高 | 1 天 |
| P1-6 | UI 直接执行文件 IO | 中 | 0.5 天 |
| P1-7 | UI 直接设置业务层状态 | 中 | 0.5 天 |
| P1-8 | Patch 反向依赖 UI 层 | 中 | 0.5 天 |
| P1-9 | Prefix 修改共享 FactionDef 状态未恢复 | 高 | 0.5 天 |
| P1-10~14 | 5 个文件超过 800 行 | 严重 | 见 P1-1/2 |
| P1-15 | FactionDefManager 读取操作无锁 | 中 | 0.5 天 |
| P1-16 | General 键重复且含义冲突 | 低 | 0.5 天 |

---

## 第一阶段：拆分上帝类（预计 5 天）

### 1.1 拆分 GearApplier.cs（2608 行 → 5 个文件）

**目标结构**：
```
Managers/
├── GearApplier.cs (保留为门面接口，约 100 行)
├── WeaponApplier.cs (约 400 行)
├── ApparelApplier.cs (约 600 行)
├── InventoryApplier.cs (约 300 行)
├── HediffApplier.cs (约 400 行)
└── ApparelBudgetEngine.cs (约 500 行，服装预算核心逻辑)
```

**拆分步骤**：

#### 步骤 1: 创建新文件并提取方法

**WeaponApplier.cs** - 提取以下方法：
- `ApplyWeapons(Pawn, KindGearData)`
- `ApplyMeleeWeapons(Pawn, KindGearData)`
- `GenerateSimpleWeapon(Pawn, GearItem)`
- `GenerateWeaponWithSpec(Pawn, SpecRequirementEdit)`
- `GetRandomHediffFromPool(HediffPoolType)`
- `GetWeaponDamage(ThingDef)`
- `GetWeaponDPS(ThingDef)`
- `GetWeaponRange(ThingDef)`
- `IsTechLevelAllowed(ThingDef, TechLevel?)`

**ApparelApplier.cs** - 提取以下方法：
- `ApplyApparel(Pawn, KindGearData)`
- `EquipApparelList(Pawn, List<GearItem>, bool)`
- `TrySelectBestCoreCombinationByBudget(...)` (含回溯搜索)
- `SelectCoreOutfitBasedOnEquipped(...)`
- `GenerateSimpleItem(Pawn, GearItem)`
- `GenerateItemWithSpec(Pawn, SpecRequirementEdit)`
- `CanWearApparel(Pawn, ThingDef)`
- `HasLayer(ThingDef, ApparelLayerDef)`
- `CoversBodyPart(ThingDef, BodyPartGroupDef)`
- `IsArmor(ThingDef)`
- `GetCandidateStuffForBudgetUpgrade(...)`
- `UpgradeStuffWithinBudget(...)`

**InventoryApplier.cs** - 提取以下方法：
- `ApplyInventory(Pawn, KindGearData)`
- `ApplyInventoryItems(Pawn, List<GearItem>)`
- `ScheduleDelayedInventoryApply(Pawn, List<GearItem>, int)`

**HediffApplier.cs** - 提取以下方法：
- `ApplyHediffs(Pawn, KindGearData)`
- `ApplyHediffFromPool(Pawn, ForcedHediff)`
- `ApplyHediffWithoutPart(Pawn, HediffDef, float)`
- `ApplyHediffWithPart(Pawn, HediffDef, float, List<BodyPartDef>)`
- `GetValidBodyPartsForHediff(HediffDef, Pawn)`
- `WillHediffIncapacitatePawn(Pawn, Hediff)`
- `GetRandomHediffFromPool(HediffPoolType)`
- `GetSocialDrugs()` / `GetHardDrugs()` / `GetAnyAddictions()` / `GetAnyVegetables()`

**ApparelBudgetEngine.cs** - 提取以下方法：
- 所有预算相关私有方法
- 核心套装选择算法
- 材质升级逻辑

#### 步骤 2: 定义接口和依赖

```csharp
// IGearApplier.cs
public interface IWeaponApplier { void ApplyWeapons(Pawn pawn, KindGearData kindData); }
public interface IApparelApplier { void ApplyApparel(Pawn pawn, KindGearData kindData); }
public interface IInventoryApplier { void ApplyInventory(Pawn pawn, KindGearData kindData); }
public interface IHediffApplier { void ApplyHediffs(Pawn pawn, KindGearData kindData); }
```

#### 步骤 3: 重构 GearApplier 为门面

```csharp
public static class GearApplier
{
    private static IWeaponApplier _weaponApplier;
    private static IApparelApplier _apparelApplier;
    private static IInventoryApplier _inventoryApplier;
    private static IHediffApplier _hediffApplier;
    
    public static void Initialize()
    {
        _weaponApplier = new WeaponApplier();
        _apparelApplier = new ApparelApplier();
        _inventoryApplier = new InventoryApplier();
        _hediffApplier = new HediffApplier();
    }
    
    public static void ApplyCustomGear(Pawn pawn, Faction faction)
    {
        // 保留原有逻辑，但委托给子组件
        if (ShouldApplyKindData(kindData))
        {
            try { _inventoryApplier.ApplyInventory(pawn, kindData); }
            catch (Exception ex) { Log.Error(...); }
            
            try { _weaponApplier.ApplyWeapons(pawn, kindData); }
            catch (Exception ex) { Log.Error(...); }
            
            try { _apparelApplier.ApplyApparel(pawn, kindData); }
            catch (Exception ex) { Log.Error(...); }
            
            try { _hediffApplier.ApplyHediffs(pawn, kindData); }
            catch (Exception ex) { Log.Error(...); }
        }
    }
}
```

**验收标准**：
- [ ] 每个文件 < 800 行
- [ ] 单元测试覆盖各子组件
- [ ] 运行时行为与重构前一致
- [ ] 日志输出格式保持不变

---

### 1.2 拆分 GearEditPanel.cs（1760 行 → 3 个文件）

**目标结构**：
```
UI/Panels/
├── GearEditPanel.cs (协调器，约 300 行)
├── SimpleModePanel.cs (约 600 行)
├── AdvancedModePanel.cs (约 700 行)
└── GearTabControl.cs (约 200 行)
```

**拆分方案**：

#### SimpleModePanel.cs
- `DrawSimpleMode(Rect, KindGearData)`
- `DrawWeaponSection(Rect, KindGearData)`
- `DrawApparelSection(Rect, KindGearData)`
- `DrawHediffSection(Rect, KindGearData)`
- 简单模式特有的 UI 绘制逻辑

#### AdvancedModePanel.cs
- `DrawAdvancedMode(Rect, KindGearData)`
- `DrawWeaponSpecList(Rect, List<SpecRequirementEdit>)`
- `DrawApparelSpecList(Rect, List<SpecRequirementEdit>)`
- `DrawHediffList(Rect, List<ForcedHediff>)`
- 高级模式特有的 UI 绘制逻辑

#### GearTabControl.cs
- 标签页切换逻辑
- 类别过滤 UI
- 撤销/重做按钮

**验收标准**：
- [ ] 每个文件 < 800 行
- [ ] UI 行为与重构前一致
- [ ] 支持热切换 Simple/Advanced 模式

---

### 1.3 拆分其他超标文件

| 文件 | 目标结构 | 工作量 |
|------|---------|--------|
| FactionEditWindow.cs (994 行) | `FactionBasicInfoPanel` + `FactionKindListPanel` + `FactionGroupListPanel` | 1 天 |
| PresetManagerWindow.cs (932 行) | `PresetListPanel` + `PresetActionsPanel` | 1 天 |
| FactionDefManager.cs (853 行) | 提取缓存逻辑到 `FactionDefCache` | 0.5 天 |
| ItemLibraryPanel.cs (838 行) | 提取筛选器到 `ItemFilterBar` + 排序逻辑到 `ItemSorter` | 0.5 天 |
| Dialog_PawnGroupGenerationPreview.cs (803 行) | 提取预览逻辑到 `GroupPreviewRenderer` | 0.5 天 |

---

## 第二阶段：修复跨层依赖（预计 2 天）

### 2.1 CustomIconManager 迁移到 IO 层

**当前问题**：`CustomIconManager.cs` 位于 `UI/` 命名空间，但包含完整文件 IO 逻辑。

**目标结构**：
```
IO/
├── PresetIOManager.cs (已有)
├── IconIOManager.cs (新建，从 CustomIconManager 迁移)
└── ...

UI/
└── CustomIconManager.cs (保留为适配层，仅调用 IconIOManager)
```

**迁移步骤**：

1. 创建 `IO/IconIOManager.cs`，迁移所有文件操作：
   - `LoadIcon(string path)`
   - `SaveIcon(string path, byte[] data)`
   - `DeleteIcon(string path)`
   - `GetIconPaths(string directory)`

2. 修改 `UI/CustomIconManager` 为适配层：
```csharp
public static class CustomIconManager
{
    public static Texture2D GetIcon(string path)
    {
        if (path.StartsWith("Custom:"))
        {
            var iconPath = path.Substring(7);
            var bytes = IconIOManager.LoadIcon(iconPath);
            return BytesToTexture(bytes);
        }
        return null;
    }
    // ... 其他方法委托给 IconIOManager
}
```

**验收标准**：
- [ ] UI 层不再直接调用 `File.*` 方法
- [ ] 文件 IO 逻辑集中在 IO 层
- [ ] 现有功能正常工作

---

### 2.2 PreviewPreset 封装

**当前问题**：`PresetManagerWindow.cs:805` 直接赋值 `GearApplier.PreviewPreset = ...`

**修复方案**：

```csharp
// GearApplier.cs
public static class GearApplier
{
    private static FactionGearPreset _previewPreset;
    
    public static void SetPreviewPreset(FactionGearPreset preset)
    {
        _previewPreset = preset;
        Log.Message($"[FactionGearCustomizer] Preview preset set to: {preset?.presetName ?? "null"}");
    }
    
    public static void ClearPreviewPreset()
    {
        _previewPreset = null;
        Log.Message("[FactionGearCustomizer] Preview preset cleared");
    }
    
    // 内部使用
    private static FactionGearPreset GetPreviewPreset() => _previewPreset;
}
```

**修改调用点**：
- `PresetManagerWindow.cs` → `GearApplier.SetPreviewPreset(preset)`
- `FactionGearPreviewWindow.cs` → 同样使用封装方法

**验收标准**：
- [ ] UI 层不再直接访问 `GearApplier.PreviewPreset` 字段
- [ ] 添加日志记录
- [ ] 预览功能正常工作

---

### 2.3 Patch 业务逻辑下沉

#### 2.3.1 Patch_GeneratePawn 异种/年龄逻辑下沉

**当前问题**：`Patch_GeneratePawn.cs` 包含 `ApplyXenotypeSettings` (60 行) 和 `ApplyAgeSettings` (64 行) 等业务逻辑。

**目标结构**：
```
Managers/
├── XenotypeManager.cs (新建)
├── AgeSettingsManager.cs (新建)
└── ...

Patches/
└── Patch_GeneratePawn.cs (仅保留调度逻辑)
```

**XenotypeManager.cs**：
```csharp
public static class XenotypeManager
{
    public static void ApplyXenotypeSettings(Faction faction, PawnKindDef kindDef, ref PawnGenerationRequest request)
    {
        // 迁移自 Patch_GeneratePawn.ApplyXenotypeSettings
    }
    
    public static void RestoreOriginalXenotypeChances(Faction faction, PawnKindDef kindDef)
    {
        // 恢复逻辑
    }
}
```

**Patch_GeneratePawn.cs 重构后**：
```csharp
public static void Prefix(ref PawnGenerationRequest request)
{
    if (ModsConfig.BiotechActive && request.KindDef != null && request.Faction != null)
    {
        XenotypeManager.ApplyXenotypeSettings(request.Faction, request.KindDef, ref request);
    }
    
    if (request.KindDef != null && request.Faction != null)
    {
        AgeSettingsManager.ApplyAgeSettings(ref request);
    }
}

public static void Postfix(Pawn __result, PawnGenerationRequest request)
{
    // ... 现有逻辑
    
    // 新增：恢复异种设置
    if (ModsConfig.BiotechActive && request.KindDef != null && request.Faction != null)
    {
        XenotypeManager.RestoreOriginalXenotypeChances(request.Faction, request.KindDef);
    }
}
```

**验收标准**：
- [ ] Patch 文件仅包含调度逻辑
- [ ] 业务逻辑移至 Manager 层
- [ ] 添加 Postfix 恢复异种设置

---

#### 2.3.2 Patch_GameInit 生命周期管理下沉

**当前问题**：`Patch_GameInit.cs` 包含存档同步、重置、清理等核心业务逻辑（约 300 行）。

**目标结构**：
```
Core/
└── SaveLifecycleManager.cs (新建)
```

**SaveLifecycleManager.cs**：
```csharp
public static class SaveLifecycleManager
{
    public static void OnGameInit() { /* 游戏初始化 */ }
    public static void OnLoadGame() { /* 加载存档 */ }
    public static void OnSaveGame() { /* 保存存档 */ }
    public static void OnReturnToMainMenu() { /* 返回主菜单清理 */ }
    public static void OnNewSave() { /* 新存档重置 */ }
}
```

**Patch_GameInit.cs 重构后**：
```csharp
[HarmonyPatch(typeof(GameInit), "StartPlay")]
public static class Patch_GameInit_StartPlay
{
    public static void Postfix()
    {
        SaveLifecycleManager.OnGameInit();
    }
}

[HarmonyPatch(typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow")]
public static class Patch_GameInit_LoadGame
{
    public static void Postfix()
    {
        SaveLifecycleManager.OnLoadGame();
    }
}
```

**验收标准**：
- [ ] Patch 文件仅包含 Harmony 钩子
- [ ] 生命周期逻辑集中在 SaveLifecycleManager
- [ ] 存档切换行为与重构前一致

---

### 2.4 Patch 反向依赖 UI 层

**当前问题**：`Patch_GameInit.cs` 直接调用 `EditorSession.ResetSession()`、`FactionGearEditor.RefreshAllCaches()`。

**修复方案**：

1. 定义事件接口：
```csharp
// Core/IEditorLifecycle.cs
public interface IEditorLifecycle
{
    void OnProgramStateChanged(ProgramState oldState, ProgramState newState);
    void OnCachesRefreshNeeded();
}
```

2. 实现接口（由 UI 层注册）：
```csharp
// UI/FactionGearEditor.cs
public class FactionGearEditor : IEditorLifecycle
{
    public void OnProgramStateChanged(ProgramState oldState, ProgramState newState)
    {
        if (newState == ProgramState.Playing)
        {
            EditorSession.ResetSession();
        }
    }
    
    public void OnCachesRefreshNeeded()
    {
        RefreshAllCaches();
    }
}
```

3. Patch 通过事件通知：
```csharp
// Patch_GameInit.cs
public static class Patch_GameInit
{
    private static IEditorLifecycle _editorLifecycle;
    
    public static void RegisterEditorLifecycle(IEditorLifecycle lifecycle)
    {
        _editorLifecycle = lifecycle;
    }
    
    public static void Postfix()
    {
        _editorLifecycle?.OnProgramStateChanged(oldState, Current.ProgramState);
    }
}
```

**验收标准**：
- [ ] Patch 不再直接引用 UI 层类型
- [ ] 通过接口/事件解耦
- [ ] 状态切换时 UI 正确刷新

---

## 第三阶段：EditorSession 重构（预计 2 天）

### 3.1 当前结构

```csharp
public static class EditorSession
{
    // 40+ 公开可变静态字段
    public static string SelectedFactionDefName = "";
    public static Faction SelectedFactionInstance = null;
    public static Vector2 FactionListScrollPos = Vector2.zero;
    public static string SearchText = "";
    // ...
}
```

### 3.2 目标结构

```csharp
public static class EditorSession
{
    private static SelectionState _selection = new();
    private static FilterState _filters = new();
    private static ScrollState _scroll = new();
    private static CacheState _cache = new();
    private static ClipboardState _clipboard = new();
    
    // 公开属性（带访问控制）
    public static SelectionState Selection => _selection;
    public static FilterState Filters => _filters;
    public static ScrollState Scroll => _scroll;
    public static CacheState Cache => _cache;
    public static ClipboardState Clipboard => _clipboard;
    
    public static void ResetSession()
    {
        _selection.Reset();
        _filters.Reset();
        _scroll.Reset();
        _cache.Invalidate();
        _clipboard.Clear();
    }
}

public class SelectionState
{
    public string SelectedFactionDefName { get; set; } = "";
    public Faction SelectedFactionInstance { get; set; } = null;
    public string SelectedKindDefName { get; set; } = "";
    public GearCategory SelectedCategory { get; set; }
    
    public void Reset() { ... }
}

public class FilterState
{
    public string SearchText { get; set; } = "";
    public HashSet<string> SelectedModSources { get; set; } = new();
    public TechLevel? SelectedTechLevel { get; set; }
    
    public void Reset() { ... }
}

// ... 其他 State 类
```

### 3.3 迁移步骤

#### 步骤 1: 创建 State 类

创建 `SelectionState`、`FilterState`、`ScrollState`、`CacheState`、`ClipboardState` 五个类。

#### 步骤 2: 修改 EditorSession 为容器

将原有字段移动到对应 State 类中，EditorSession 仅保留属性访问器。

#### 步骤 3: 更新调用点

全局搜索替换：
- `EditorSession.SelectedFactionDefName` → `EditorSession.Selection.SelectedFactionDefName`
- `EditorSession.SearchText` → `EditorSession.Filters.SearchText`
- `EditorSession.FactionListScrollPos` → `EditorSession.Scroll.FactionListScrollPos`

#### 步骤 4: 添加变更通知（可选增强）

```csharp
public class SelectionState : INotifyPropertyChanged
{
    private string _selectedFactionDefName = "";
    
    public string SelectedFactionDefName
    {
        get => _selectedFactionDefName;
        set
        {
            if (_selectedFactionDefName != value)
            {
                _selectedFactionDefName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFactionDefName)));
                OnSelectionChanged?.Invoke();
            }
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    public event Action OnSelectionChanged;
}
```

**验收标准**：
- [ ] 所有字段封装到 State 类中
- [ ] 外部通过属性访问
- [ ] 支持 Reset 操作
- [ ] 可选：支持变更通知

---

## 第四阶段：其他 P1 问题（预计 1 天）

### 4.1 FactionDefManager 读取操作加锁

**当前问题**：`GetXenotypeChances` (第 51 行) 和 `GetOriginalXenotypeChances` (第 90 行) 读取 `originalFactionData` 时无锁。

**修复方案**：

```csharp
public static Dictionary<XenotypeDef, float> GetXenotypeChances(FactionDef factionDef, PawnKindDef kindDef)
{
    lock (originalDataLock)
    {
        if (!originalFactionData.TryGetValue(factionDef, out var data))
            return FactionDefManager.GetXenotypeChances(factionDef, kindDef);
        
        return data.GetXenotypeChances(kindDef);
    }
}
```

**验收标准**：
- [ ] 所有读取操作加锁
- [ ] 无死锁风险
- [ ] 性能影响可接受

---

### 4.2 General 键重复问题

**当前问题**：`General` 键在语言文件中出现两次，第一次="常规"，第二次="高级设置"。

**修复方案**：

1. 拆分键名：
```xml
<General>常规</General>
<AdvancedSettings>高级设置</AdvancedSettings>
```

2. 更新代码引用：
- 搜索所有 `LanguageManager.Get("General")`
- 根据上下文替换为 `LanguageManager.Get("General")` 或 `LanguageManager.Get("AdvancedSettings")`

**验收标准**：
- [ ] 无重复键
- [ ] UI 显示正确
- [ ] 三套语言文件同步更新

---

### 4.3 Patch_GeneratePawn 恢复异种设置

**当前问题**：Prefix 修改 `xenotypeSet` 后未在 Postfix 恢复。

**修复方案**：

```csharp
// Patch_GeneratePawn.cs
public static void Postfix(Pawn __result, PawnGenerationRequest request)
{
    // ... 现有逻辑
    
    // 新增：恢复异种设置
    if (ModsConfig.BiotechActive && request.KindDef != null && request.Faction != null)
    {
        XenotypeManager.RestoreOriginalXenotypeChances(request.Faction, request.KindDef);
    }
}
```

**XenotypeManager.RestoreOriginalXenotypeChances**：
```csharp
public static void RestoreOriginalXenotypeChances(Faction faction, PawnKindDef kindDef)
{
    var originalChances = FactionDefManager.GetOriginalXenotypeChances(faction.def, kindDef);
    if (originalChances != null)
    {
        var xenotypeSet = GetXenotypeSet(faction, kindDef);
        xenotypeSet.chances.Clear();
        foreach (var kvp in originalChances)
        {
            xenotypeSet.chances[kvp.Key] = kvp.Value;
        }
    }
}
```

**验收标准**：
- [ ] Postfix 恢复原始异种概率
- [ ] 无共享状态污染
- [ ] 大规模袭击测试通过

---

## 回归测试清单

完成上述重构后，需验证以下用户路径：

1. **基础装备流程**: 选择阵营 → 选择兵种 → 添加武器 → 保存 → 预览 → 验证装备正确
2. **批量操作**: 复制兵种配置 → 批量粘贴 → 撤销 → 验证数据一致
3. **预设管理**: 新建预设 → 导出 → 删除 → 导入 → 验证数据完整
4. **存档切换**: 存档 A 修改 → 保存 → 返回主菜单 → 加载存档 B → 验证数据隔离
5. **Hediff 应用**: 添加 Hediff → 指定部位 → 预览 → 验证部位正确且不倒地
6. **群组生成**: 修改袭击群组 → 触发袭击 → 验证兵种组成
7. **多语言切换**: 英文 → 中文 → 俄文 → 验证无硬编码文本暴露
8. **CE 兼容**: 安装 CE → 配置武器 → 生成 Pawn → 验证弹药自动添加

---

## 风险评估

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|---------|
| 重构引入回归 | 中 | 高 | 充分测试 + 版本控制 |
| 性能下降 | 低 | 中 | 性能测试 + 缓存优化 |
| 接口设计不当 | 中 | 中 | 代码审查 + 迭代优化 |
| 工作量超估 | 高 | 低 | 分阶段实施 + 优先级调整 |

---

## 总结

本修复方案预计总工作量 **10 天**，分为 4 个阶段：
1. **阶段 1**（5 天）：拆分上帝类，解决最严重的文件超标问题
2. **阶段 2**（2 天）：修复跨层依赖，恢复分层架构
3. **阶段 3**（2 天）：重构 EditorSession，封装全局状态
4. **阶段 4**（1 天）：修复其他 P1 问题

建议按优先级依次实施，每阶段完成后进行回归测试，确保功能正常。
