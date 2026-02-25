
# 修复完整计划报告

**日期**: 2026-02-25  
**项目**: FactionGearModification (派系编辑器)  
**状态**: 计划就绪，待实施

---

## 概述

本报告详细说明了7个问题的具体修复方案，包括实施步骤、代码修改位置和测试计划。

---

## 修复计划总览

| 优先级 | 问题编号 | 问题描述 | 预估工作量 | 风险等级 |
|--------|----------|----------|------------|----------|
| P0 | 4 | 新存档数据残留 | 高 | 中 |
| P1 | 1 | 衣着生物编码缺失 | 低 | 低 |
| P1 | 2 | 单独派系导入导出 | 中 | 低 |
| P2 | 5 | 允许非人类族群 | 中 | 低 |
| P2 | 7 | 派系信息不显示 | 低 | 低 |
| P3 | 3 | Ctrl+S快捷键 | 极低 | 极低 |
| P3 | 6 | 种族筛选器缺失 | 中 | 低 |

---

## 详细修复方案

### P0: 问题4 - 新存档数据残留 ⭐

**严重程度**: 高  
**预估工作量**: 4-6小时  
**风险等级**: 中

#### 问题根因
1. `Patch_InitNewGame.ResetGlobalSettingsForNewSave()` 只在 `!gameComponent.useCustomSettings` 时重置，边界情况未覆盖
2. `FactionDefManager` 原始数据缓存可能被污染
3. 编辑器会话状态未完全重置
4. 新存档初始化时 FactionDef 恢复不完整

#### 修复方案

**步骤1: 增强新存档初始化逻辑**

修改文件: `FactionGearModification/Patches/Patch_GameInit.cs`

在 `ResetGlobalSettingsForNewSave()` 方法中：

```csharp
private static void ResetGlobalSettingsForNewSave()
{
    var gameComponent = FactionGearGameComponent.Instance;
    if (gameComponent == null) return;

    // 1. 始终为新存档生成唯一标识符
    if (string.IsNullOrEmpty(gameComponent.saveUniqueIdentifier))
    {
        gameComponent.GenerateNewSaveIdentifier();
        Log.Message($"[FactionGearCustomizer] Generated new save identifier: {gameComponent.saveUniqueIdentifier}");
    }

    // 2. 无论如何都完全重置，确保新存档干净
    // 清理原始数据缓存，确保使用真正的原版数据
    FactionDefManager.ClearOriginalDataCache();

    // 3. 先恢复所有 FactionDef 到原始状态
    RestoreAllFactionDefsToOriginal();

    // 4. 重置全局设置
    FactionGearCustomizerMod.Settings.factionGearData.Clear();
    if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
    {
        FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
    }
    FactionGearCustomizerMod.Settings.currentPresetName = null;

    // 5. 重置会话状态
    EditorSession.ResetSession();
    UndoManager.Clear();

    // 6. 重新保存原始数据（此时 FactionDef 已是干净的）
    FactionDefManager.SaveAllOriginalData();

    // 7. 刷新缓存
    FactionGearEditor.RefreshAllCaches();

    // 8. 确保存档组件也是干净的
    gameComponent.activePresetName = null;
    gameComponent.useCustomSettings = false;
    gameComponent.savedFactionGearData.Clear();

    Log.Message("[FactionGearCustomizer] Global settings COMPLETELY reset for new save.");
}
```

**步骤2: 增强存档切换逻辑**

在 `HandleSaveSwitch()` 方法中，确保更彻底的清理：

```csharp
private static void ResetCurrentModifications()
{
    // 1. 清理原始数据缓存
    FactionDefManager.ClearOriginalDataCache();

    // 2. 先恢复所有 FactionDef
    RestoreAllFactionDefsToOriginal();

    // 3. 清除全局设置
    FactionGearCustomizerMod.Settings.factionGearData.Clear();
    if (FactionGearCustomizerMod.Settings.factionGearDataDict != null)
    {
        FactionGearCustomizerMod.Settings.factionGearDataDict.Clear();
    }
    FactionGearCustomizerMod.Settings.currentPresetName = null;

    // 4. 重置编辑器会话
    EditorSession.ResetSession();
    UndoManager.Clear();

    // 5. 重新保存原始数据
    FactionDefManager.SaveAllOriginalData();

    // 6. 刷新缓存
    FactionGearEditor.RefreshAllCaches();

    Log.Message("[FactionGearCustomizer] Current modifications COMPLETELY reset due to save switch.");
}
```

**步骤3: 检查 EditorSession.ResetSession()**

确保 `EditorSession` 中的所有静态字段都被正确重置。

**步骤4: 添加调试日志**

在关键位置添加详细日志，便于追踪问题。

#### 测试计划
1. 创建存档A，修改多个派系的数据
2. 保存存档A，退出游戏
3. 创建新存档B，不做任何修改
4. 检查新存档B中是否有存档A的数据残留
5. 验证派系图标、简介、兵种配置等是否都是原版状态

---

### P1: 问题1 - 高级-衣着：缺失生物编码概率调整选项

**严重程度**: 中  
**预估工作量**: 1-2小时  
**风险等级**: 低

#### 问题分析
需要先确认：衣着是否真的需要生物编码功能？在RimWorld原版中，生物编码主要用于武器。

**假设用户希望衣着也有生物编码功能**，修复方案如下：

#### 修复方案

**步骤1: 扩展数据结构**

修改文件: `FactionGearModification/Data/KindGearData.cs`

添加字段：
```csharp
public float? BiocodeWeaponChance = null;
public float? BiocodeApparelChance = null;  // 新增
```

在 `ExposeData()`, `ResetToDefault()`, `DeepCopy()`, `CopyFrom()` 方法中添加相应处理。

**步骤2: 修改 SpecRequirementEdit**

修改文件: `FactionGearModification/Data/SpecRequirementEdit.cs`

确保 `Biocode` 字段对衣着也可用（实际上已经存在）。

**步骤3: 添加UI**

修改文件: `FactionGearModification/UI/Panels/GearEditPanel.cs`

在 `DrawAdvancedApparel()` 方法中，参考武器栏的实现添加生物编码概率滑块：

```csharp
private static void DrawAdvancedApparel(Listing_Standard ui, KindGearData kindData)
{
    DrawEmbeddedGearList(ui, kindData, GearCategory.Apparel, GearCategory.Armors);
    
    ui.GapLine();
    
    // 添加生物编码概率调整（类似武器）
    float biocodeChance = kindData.BiocodeApparelChance ?? 0f;
    float oldBiocode = biocodeChance;
    WidgetsUtils.Label(ui, $"{LanguageManager.Get("BiocodeChance")}: {biocodeChance:P0}");
    biocodeChance = ui.Slider(biocodeChance, 0f, 1f);
    if (Math.Abs(biocodeChance - oldBiocode) &gt; 0.001f)
    {
        kindData.BiocodeApparelChance = biocodeChance;
        FactionGearEditor.MarkDirty();
    }
    
    ui.GapLine();
    WidgetsUtils.Label(ui, $"&lt;b&gt;{LanguageManager.Get("SpecificApparelAdvanced")}&lt;/b&gt;");
    // ... 其余代码
}
```

**步骤4: 修改 GearApplier**

修改文件: `FactionGearModification/Managers/GearApplier.cs`

在 `ApplyApparel()` 方法中添加生物编码应用逻辑，参考武器的实现。

**步骤5: 添加语言字符串**

在语言文件中添加相关翻译。

#### 测试计划
1. 进入高级模式，选择衣着标签页
2. 验证生物编码概率滑块是否显示
3. 测试调整概率是否生效
4. 验证生成的衣着是否正确应用生物编码

---

### P1: 问题2 - 单独派系导入导出功能缺失

**严重程度**: 中  
**预估工作量**: 3-4小时  
**风险等级**: 低

#### 修复方案

**步骤1: 扩展 PresetIOManager**

修改文件: `FactionGearModification/IO/PresetIOManager.cs`

添加单个派系导入导出方法：

```csharp
// 导出单个派系为 Base64 字符串
public static string ExportFactionToBase64(FactionGearData factionData)
{
    if (factionData == null)
    {
        Log.Warning("[FactionGearCustomizer] ExportFactionToBase64 called with null data.");
        return null;
    }

    // 创建一个只包含单个派系的临时预设
    var tempPreset = new FactionGearPreset
    {
        name = $"Faction_{factionData.factionDefName}",
        factionGearData = new List&lt;FactionGearData&gt; { factionData.DeepCopy() }
    };

    return ExportToBase64(tempPreset);
}

// 从 Base64 字符串导入单个派系
public static FactionGearData ImportFactionFromBase64(string base64)
{
    var preset = ImportFromBase64(base64);
    if (preset == null || preset.factionGearData.NullOrEmpty())
    {
        return null;
    }
    return preset.factionGearData.FirstOrDefault();
}
```

**步骤2: 添加UI按钮**

在派系列表面板中添加"导出派系"和"导入派系"按钮。

**步骤3: 创建导入对话框**

创建 `Dialog_ImportFaction` 对话框，用于粘贴 Base64 字符串并导入。

**步骤4: 集成到现有界面**

修改 `FactionListPanel.cs`，在派系条目上添加上下文菜单或按钮。

#### 测试计划
1. 选择一个派系，点击导出
2. 验证导出的 Base64 字符串
3. 在另一个预设中导入该派系
4. 验证导入的数据是否完整

---

### P2: 问题7 - 派系群组成员列表非本派系成员的派系信息不显示

**严重程度**: 低  
**预估工作量**: 1-2小时  
**风险等级**: 低

#### 修复方案

修改文件: `FactionGearModification/UI/Dialog_EditPawnGroup.cs`

改进派系信息查找逻辑：

```csharp
private static string GetFactionLabelForKind(PawnKindDef kind)
{
    if (kind == null) return "-";

    // 1. 先尝试从 defaultFactionType 字段获取
    if (defaultFactionTypeField != null)
    {
        var faction = defaultFactionTypeField.GetValue(kind) as FactionDef;
        if (faction != null) return faction.LabelCap;
    }

    // 2. 遍历所有派系，查找该兵种是否在某个派系的 pawnGroupMakers 中
    foreach (var factionDef in DefDatabase&lt;FactionDef&gt;.AllDefs)
    {
        if (factionDef.pawnGroupMakers == null) continue;

        foreach (var pgm in factionDef.pawnGroupMakers)
        {
            var hasKind = (pgm.options?.Any(opt =&gt; opt.kind == kind) ?? false) ||
                         (pgm.traders?.Any(opt =&gt; opt.kind == kind) ?? false) ||
                         (pgm.carriers?.Any(opt =&gt; opt.kind == kind) ?? false) ||
                         (pgm.guards?.Any(opt =&gt; opt.kind == kind) ?? false);

            if (hasKind)
            {
                return factionDef.LabelCap;
            }
        }
    }

    // 3. 检查 race 的默认派系
    if (kind.race?.defaultFactionType != null)
    {
        return kind.race.defaultFactionType.LabelCap;
    }

    return "-";
}
```

然后在 `DrawPawnListSection()` 中使用这个新方法。

#### 测试计划
1. 添加一个非本派系的兵种到群组
2. 验证该兵种的派系信息是否正确显示

---

### P3: 问题3 - Ctrl+S 快捷键保存

**严重程度**: 低  
**预估工作量**: 30分钟  
**风险等级**: 极低

#### 修复方案

修改文件: `FactionGearModification/UI/Panels/GearEditPanel.cs`

在快捷键处理区域添加：

```csharp
// Handle Keyboard Shortcuts - only if has active preset
if (hasActivePreset &amp;&amp; Event.current.type == EventType.KeyDown &amp;&amp; Event.current.control)
{
    // ... 现有 Ctrl+Z 和 Ctrl+Y 代码 ...

    else if (Event.current.keyCode == KeyCode.S)
    {
        FactionGearEditor.SaveChanges();
        Event.current.Use();
    }
}
```

同时也在 `FactionGearMainTabWindow` 中添加快捷键处理，确保在整个窗口范围内都有效。

#### 测试计划
1. 做一些修改
2. 按 Ctrl+S
3. 验证修改是否被保存

---

### P2: 问题5 - 允许添加kinddef非人类族群

**严重程度**: 中  
**预估工作量**: 2-3小时  
**风险等级**: 低

#### 修复方案

需要先检查 `Dialog_PawnKindPicker.cs` 的实现，看看是否有过滤人类的逻辑。

假设当前有过滤逻辑，修复方案：

**步骤1: 修改兵种选择器**

修改文件: `FactionGearModification/UI/Dialog_PawnKindPicker.cs`

移除或添加选项来控制是否显示非人类兵种。

**步骤2: 添加过滤选项**

在选择器界面添加"显示非人类兵种"的复选框。

**步骤3: 验证 GearApplier**

确保 `GearApplier` 能正确处理非人类兵种的装备应用。

#### 测试计划
1. 打开群组编辑
2. 尝试添加机械族或动物
3. 验证是否能成功添加
4. 验证生成时是否正常工作

---

### P3: 问题6 - 派系群组单独添加成员界面缺少种族筛选器

**严重程度**: 低  
**预估工作量**: 2-3小时  
**风险等级**: 低

#### 修复方案

**步骤1: 添加筛选状态**

在 `Dialog_PawnKindPicker` 中添加种族筛选相关的字段。

**步骤2: 添加筛选UI**

在对话框顶部添加种族筛选下拉框或搜索框。

**步骤3: 实现筛选逻辑**

根据选择的种族过滤显示的兵种列表。

#### 测试计划
1. 打开兵种选择器
2. 使用种族筛选功能
3. 验证筛选结果是否正确

---

## 实施顺序

建议按以下顺序实施修复：

1. **Phase 1 (P0)**: 问题4 - 新存档数据残留
2. **Phase 2 (P1)**: 问题1 - 衣着生物编码、问题2 - 单独派系导入导出
3. **Phase 3 (P2)**: 问题7 - 派系信息显示、问题5 - 非人类族群
4. **Phase 4 (P3)**: 问题3 - Ctrl+S、问题6 - 种族筛选器

## 总体测试策略

每个问题修复后都需要：
1. 单元测试（如果适用）
2. 手动功能测试
3. 回归测试（确保不破坏现有功能）
4. 存档兼容性测试

所有修复完成后进行完整的集成测试。
