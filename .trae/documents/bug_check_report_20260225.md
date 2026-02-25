
# Bug 检查报告

**日期**: 2026-02-25  
**项目**: FactionGearModification (派系编辑器)  
**状态**: 分析完成，待修复

---

## 概述

本报告详细分析了7个待解决的问题，包括代码定位、影响范围和严重程度评估。

---

## 问题清单

### 1. 高级-衣着：缺失生物编码概率调整选项（可复用武器栏的）

**严重程度**: 中  
**影响范围**: UI功能完整性  

#### 问题描述
在高级模式的"衣着"（Apparel）标签页中，缺少与"武器"标签页类似的生物编码（Biocode）概率调整选项。

#### 代码定位
- 相关文件: `FactionGearModification/UI/Panels/GearEditPanel.cs`
- 武器栏生物编码实现位置: [GearEditPanel.cs:780-788](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/Panels/GearEditPanel.cs#L780-L788)
- 衣着标签页绘制位置: [GearEditPanel.cs:747-773](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/Panels/GearEditPanel.cs#L747-L773)

#### 数据结构检查
- `KindGearData.cs` 中已有 `BiocodeWeaponChance` 字段 (第41行)
- `SpecRequirementEdit.cs` 中已有 `Biocode` 字段 (第23行)
- **注意**: 目前只有武器有生物编码功能，衣着没有。需要确认是否需要为衣着也添加生物编码支持。

---

### 2. 单独派系导入导出功能缺失

**严重程度**: 中  
**影响范围**: 预设管理功能  

#### 问题描述
当前只有完整预设的导入导出功能，缺少针对单个派系数据的导入导出功能。

#### 代码定位
- 预设管理器: `FactionGearModification/IO/PresetIOManager.cs`
- 预设选择对话框: `FactionGearModification/UI/Dialogs/Dialog_PresetFactionSelector.cs`
- 已存在的部分实现: `FactionGearGameComponent.ApplyFactionsFromPreset()` 方法 ([FactionGearGameComponent.cs:209-261](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Core/FactionGearGameComponent.cs#L209-L261))

#### 现状分析
- `PresetIOManager` 目前只支持完整预设的 Base64 导入导出
- 已有选择多个派系从预设应用的功能
- **缺少**: 将单个派系导出为独立文件/字符串的功能
- **缺少**: 从外部导入单个派系数据的功能

---

### 3. Ctrl+S 快捷键保存

**严重程度**: 低  
**影响范围**: 用户体验  

#### 问题描述
编辑器中缺少 Ctrl+S 快捷键来快速保存更改。

#### 代码定位
- 已有快捷键处理: [GearEditPanel.cs:152-176](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/Panels/GearEditPanel.cs#L152-L176)
- 保存功能: `FactionGearEditor.SaveChanges()` ([FactionGearEditor.cs:169-188](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/FactionGearEditor.cs#L169-L188))
- 主窗口: `FactionGearMainTabWindow.cs`

#### 现状分析
- 已实现 Ctrl+Z（撤销）和 Ctrl+Y（重做）
- Ctrl+S 功能尚未实现
- 保存逻辑已存在，只需添加快捷键绑定

---

### 4. 创建新存档并使用此mod创建新预设时出现上个存档残留的faction数据 ⭐ (最高优先级)

**严重程度**: 高 - 严重  
**影响范围**: 数据完整性、存档安全  

#### 问题描述
当创建新存档并使用此mod创建新预设时（即在新存档中完全没有修改/添加任何mod/faction数据），会出现上个存档残留的faction数据。除了Faction name有重置外，其他所有数据都残留下来，包括但不限于：
- 派系图标
- 简介
- 兵种配置
- 群组
- 异种人概率
- 等等...

#### 代码定位与分析

**相关文件**:
1. `FactionGearModification/Core/FactionGearGameComponent.cs` - 存档组件
2. `FactionGearModification/Patches/Patch_GameInit.cs` - 游戏初始化补丁
3. `FactionGearModification/Core/FactionGearCustomizerSettings.cs` - 全局设置
4. `FactionGearModification/UI/FactionGearEditor.cs` - 编辑器核心

**关键代码区域**:
- 新游戏初始化: [Patch_GameInit.cs:126-178](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_GameInit.cs#L126-L178)
- 存档切换处理: [Patch_GameInit.cs:184-240](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_GameInit.cs#L184-L240)
- 全局设置重置: [Patch_GameInit.cs:138-177](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_GameInit.cs#L138-L177)

#### 问题根因分析

**发现的潜在问题**:

1. **全局设置没有完全重置**
   - `Patch_InitNewGame` 中的 `ResetGlobalSettingsForNewSave()` 方法在 `!gameComponent.useCustomSettings` 时才重置
   - 但可能存在边界情况没有被覆盖

2. **FactionDefManager 原始数据缓存问题**
   - `FactionDefManager.ClearOriginalDataCache()` 被调用
   - 但随后又调用了 `FactionDefManager.SaveAllOriginalData()`
   - **风险**: 如果在保存原始数据时，全局设置中仍有残留数据，可能会污染原始数据缓存

3. **编辑器会话状态问题**
   - `EditorSession.ResetSession()` 被调用
   - 但某些静态字段可能没有被正确重置

4. **FactionDef 恢复不完整**
   - `RestoreAllFactionDefsToOriginal()` 方法只在存档切换时调用
   - 新存档初始化时可能没有完整恢复所有 FactionDef

---

### 5. 允许添加kinddef非人类族群

**严重程度**: 中  
**影响范围**: 功能扩展性  

#### 问题描述
当前派系群组添加成员时，可能只允许添加人类族群，缺少对非人类族群（如机械族、动物等）的支持。

#### 代码定位
- 兵种选择器: `FactionGearModification/UI/Dialog_PawnKindPicker.cs`
- 派系兵种获取: `FactionGearEditor.GetFactionKinds()` ([FactionGearEditor.cs:37-113](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/FactionGearEditor.cs#L37-L113))
- 群组编辑对话框: `Dialog_EditPawnGroup.cs`

#### 现状分析
- `GetFactionKinds()` 从派系的 `pawnGroupMakers` 中获取兵种
- 可能存在过滤逻辑只允许人类
- 需要检查 `Dialog_PawnKindPicker` 是否有过滤条件

---

### 6. 派系群组单独添加成员界面缺少种族筛选器

**严重程度**: 低  
**影响范围**: 用户体验  

#### 问题描述
在派系群组添加成员的界面中，缺少按种族筛选的功能，导致在大量兵种中查找困难。

#### 代码定位
- 相关文件: `FactionGearModification/UI/Dialog_PawnKindPicker.cs`
- 群组编辑: `Dialog_EditPawnGroup.cs` 中的 `DrawPawnListSection()` 方法

#### 现状分析
- 需要查看 `Dialog_PawnKindPicker` 的具体实现
- 目前可能只有简单的列表显示，没有筛选功能

---

### 7. 派系群组成员列表非本派系成员的派系信息不显示

**严重程度**: 低  
**影响范围**: UI信息完整性  

#### 问题描述
在派系群组成员列表中，对于非本派系的成员，其派系信息显示为"-"而不是实际的派系名称。

#### 代码定位
- 相关代码: [Dialog_EditPawnGroup.cs:216-237](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/Dialog_EditPawnGroup.cs#L216-L237)

#### 现状分析
```csharp
// 当前逻辑
string factionLabel = "-";
if (kind != null)
{
    // 尝试从defaultFactionType字段获取派系信息
    if (defaultFactionTypeField != null)
    {
        var faction = defaultFactionTypeField.GetValue(kind) as FactionDef;
        if (faction != null) factionLabel = faction.LabelCap;
    }
    
    // 如果没有获取到派系信息，尝试从当前编辑的派系获取
    if (factionLabel == "-" &amp;&amp; factionDef != null)
    {
        // 检查该兵种是否属于当前派系
        var factionKinds = FactionGearEditor.GetFactionKinds(factionDef);
        if (factionKinds != null &amp;&amp; factionKinds.Any(k =&gt; k.defName == kind.defName))
        {
            factionLabel = factionDef.LabelCap;
        }
    }
}
```

**问题**:
- 只通过 `defaultFactionType` 字段获取派系
- 许多兵种可能没有设置这个字段
- 缺少后备方案来查找兵种实际所属的派系

---

## 优先级排序

| 优先级 | 问题编号 | 问题描述 | 理由 |
|--------|----------|----------|------|
| P0 | 4 | 新存档数据残留 | 严重影响数据完整性，可能导致存档污染 |
| P1 | 1 | 衣着生物编码缺失 | 功能完整性 |
| P1 | 2 | 单独派系导入导出 | 功能完整性 |
| P2 | 5 | 允许非人类族群 | 功能扩展性 |
| P2 | 7 | 派系信息不显示 | UI信息完整性 |
| P3 | 3 | Ctrl+S快捷键 | 用户体验优化 |
| P3 | 6 | 种族筛选器缺失 | 用户体验优化 |

---

## 总结

本次检查共发现7个问题，其中：
- **1个严重问题**（P0）：新存档数据残留，需要优先修复
- **2个中等问题**（P1）：功能完整性问题
- **2个中等问题**（P2）：功能扩展和UI信息问题
- **2个低优先级问题**（P3）：用户体验优化

所有问题的详细修复计划将在单独的修复计划报告中提供。
