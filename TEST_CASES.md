# FactionGearModification 项目测试用例设计

## 一、项目概述

**项目名称**: FactionGearCustomizer (阵营装备自定义器)  
**项目类型**: RimWorld 游戏模组 (MOD)  
**测试框架建议**: 由于项目依赖 RimWorld 游戏环境，建议采用混合测试策略：
1. **单元测试** - 针对纯逻辑类（数据模型、工具类）
2. **游戏内集成测试** - 通过游戏日志和调试功能验证运行时行为

---

## 二、测试用例设计

### 2.1 数据模型层测试 (Data)

#### 2.1.1 GearItem 测试

| 测试ID | 测试名称 | 测试目标 | 测试方法 | 预期结果 |
|--------|----------|----------|----------|----------|
| DATA-GI-001 | 构造函数初始化 | GearItem(string, float) | 创建 GearItem 实例，验证属性赋值 | thingDefName 和 weight 正确赋值，cachedThingDef 为 null |
| DATA-GI-002 | 空构造函数 | GearItem() | 创建空实例 | 默认 weight = 1f |
| DATA-GI-003 | ThingDef 缓存 | 访问 ThingDef 属性 | 设置 thingDefName 后访问 ThingDef 属性 | cachedThingDef 正确缓存 ThingDef |
| DATA-GI-004 | 无效 DefName 处理 | thingDefName 为空或无效 | 设置空字符串或无效名称后访问 ThingDef | 返回 null，无异常 |
| DATA-GI-005 | ExposeData 序列化 | Scribe 序列化/反序列化 | 序列化后反序列化 | 数据完整性保持 |
| DATA-GI-006 | 默认权重 | weight 默认值 | 创建不带 weight 参数的实例 | weight = 1f |

#### 2.1.2 KindGearData 测试

| 测试ID | 测试名称 | 测试目标 | 测试方法 | 预期结果 |
|--------|----------|----------|----------|----------|
| DATA-KGD-001 | 构造函数 | 初始化 kindDefName | 创建实例 | kindDefName 正确赋值，各列表为空 |
| DATA-KGD-002 | 重置功能 | ResetToDefault() | 调用重置方法 | 所有列表清空，isModified = false，高级字段重置为 null |
| DATA-KGD-003 | 深拷贝功能 | DeepCopy() | 拷贝实例后修改原实例 | 拷贝实例独立于原实例 |
| DATA-KGD-004 | 拷贝复制功能 | CopyFrom() | 复制数据到已有实例 | 目标实例数据与源一致 |
| DATA-KGD-005 | ExposeData 序列化 | 复杂对象序列化 | 序列化包含 SpecificApparel/ForcedHediffs 的实例 | 序列化后数据完整 |
| DATA-KGD-006 | 空列表处理 | 空列表序列化 | 处理 null 列表 | 反序列化后正确初始化为空列表 |

#### 2.1.3 FactionGearData 测试

| 测试ID | 测试名称 | 测试目标 | 测试方法 | 预期结果 |
|--------|----------|----------|----------|----------|
| DATA-FGD-001 | 构造函数 | factionDefName 初始化 | 创建实例 | 正确初始化，字典为空 |
| DATA-FGD-002 | 获取/创建兵种数据 | GetOrCreateKindData() | 首次调用和重复调用 | 首次创建，重复返回同一实例 |
| DATA-FGD-003 | 字典索引 | 字典查询效率 | 多次调用 GetKindData | 快速返回对应数据 |
| DATA-FGD-004 | 添加/更新数据 | AddOrUpdateKindData() | 更新已存在和新增数据 | 正确更新字典和列表 |
| DATA-FGD-005 | 重置功能 | ResetToDefault() | 清空阵营数据 | 列表和字典清空 |
| DATA-FGD-006 | 深拷贝 | DeepCopy() | 拷贝阵营数据 | 独立副本 |

---

### 2.2 业务逻辑层测试 (Managers)

#### 2.2.1 FactionGearManager 测试

| 测试ID | 测试名称 | 测试目标 | 测试方法 | 预期结果 |
|--------|----------|----------|----------|----------|
| MGR-FGM-001 | 缓存初始化 | 首次调用触发初始化 | 调用 GetAllWeapons() | cachedAllWeapons 填充且不为空 |
| MGR-FGM-002 | 缓存单例 | 多次调用返回同一缓存 | 多次调用 GetAllWeapons() | 返回同一列表引用 |
| MGR-FGM-003 | 武器分类 | 远程武器筛选 | 调用 GetAllWeapons() | 仅包含 IsRangedWeapon = true 的项 |
| MGR-FGM-004 | 近战武器分类 | 近战武器筛选 | 调用 GetAllMeleeWeapons() | 仅包含 IsMeleeWeapon = true 的项 |
| MGR-FGM-005 | 头盔分类 | 头盔筛选 | 调用 GetAllHelmets() | 仅包含 Overhead 层的装备 |
| MGR-FGM-006 | 护甲分类 | 护甲筛选 | 调用 GetAllArmors() | Shell 层或护甲值 > 0.4 的装备 |
| MGR-FGM-007 | 服装分类 | 服装筛选 | 调用 GetAllApparel() | OnSkin/Middle 层的非护甲装备 |
| MGR-FGM-008 | 腰带的分类 | 腰带筛选 | 调用 GetAllOthers() | Belt 层的装备 |
| MGR-FGM-009 | 武器射程获取 | GetWeaponRange() | 传入远程武器和近战武器 | 远程返回实际射程，近战返回 0 |
| MGR-FGM-010 | 武器伤害获取 | GetWeaponDamage() | 传入不同类型武器 | 返回正确伤害值（子弹或工具） |
| MGR-FGM-011 | 武器精度获取 | GetWeaponAccuracy() | 传入武器 | 返回 AccuracyMedium 值 |
| MGR-FGM-012 | DPS 计算 | CalculateWeaponDPS() | 传入武器计算 DPS | 伤害/冷却时间 正确计算 |
| MGR-FGM-013 | 护甲锐器防护 | GetArmorRatingSharp() | 传入护甲 | 返回锐器防护值 |
| MGR-FGM-014 | 护甲钝器防护 | GetArmorRatingBlunt() | 传入护甲 | 返回钝器防护值 |
| MGR-FGM-015 | Mod 来源获取 | GetModSource() | 传入不同 Mod 的装备 | 返回正确的 modContentPack.Name |
| MGR-FGM-016 | Mod 分组 | GetModGroup() | 传入 CE/Vanilla Expanded/Alpha/Rimsenal 装备 | 返回正确分组名称 |
| MGR-FGM-017 | 空值处理 | 空参数方法 | 传入 null 调用各方法 | 返回 0 或空列表，无异常 |
| MGR-FGM-018 | 默认预设加载 | LoadDefaultPresets() | 调用方法 | 加载所有阵营的默认数据 |

#### 2.2.2 UndoManager 测试

| 测试ID | 测试名称 | 测试目标 | 测试方法 | 预期结果 |
|--------|----------|----------|----------|----------|
| MGR-UM-001 | 记录状态 | RecordState() | 记录状态后检查 | undoList 包含记录，CanUndo = true |
| MGR-UM-002 | 撤销操作 | Undo() | 记录→修改→撤销 | 数据恢复到记录时状态，redoStack 有记录 |
| MGR-UM-003 | 重做操作 | Redo() | 记录→修改→撤销→重做 | 数据恢复到最后修改状态 |
| MGR-UM-004 | 撤销限制 | 超过30步撤销 | 记录31次后撤销 | 保持30步记录，最旧记录被移除 |
| MGR-UM-005 | 上下文切换 | 切换兵种后撤销 | 切换 kindDefName 后撤销 | 撤销栈清空 |
| MGR-UM-006 | 空操作处理 | 空参数调用 | 调用各方法传入 null | 无异常，正确处理 |
| MGR-UM-007 | 清除历史 | Clear() | 清除后检查 | 两个栈都清空，CanUndo/CanRedo = false |

#### 2.2.3 GearApplier 测试

| 测试ID | 测试名称 | 测试目标 | 测试方法 | 预期结果 |
|--------|----------|----------|----------|----------|
| MGR-GA-001 | 空参数处理 | ApplyCustomGear() | 传入 null Pawn/Faction | 无异常，直接返回 |
| MGR-GA-002 | 强制裸体 | ForceNaked = true | 应用装备 | 移除所有服装 |
| MGR-GA-003 | 强制只选 | ForceOnlySelected = true | 应用装备 | 移除未选中装备 |
| MGR-GA-004 | 随机装备选择 | 权重随机选择 | 根据 weight 选择装备 | 按权重概率选择 |
| MGR-GA-005 | 强制健康状态 | 施加 Hediff | 应用带 ForcedHediffs 的数据 | 正确施加健康状态 |
| MGR-GA-006 | 装备质量 | 强制质量设置 | 设置 ForcedWeaponQuality/ItemQuality | 生成物品具有正确质量 |
| MGR-GA-007 | 装备颜色 | 颜色应用 | 设置 ApparelColor | 物品具有正确颜色 |
| MGR-GA-008 | 生物编码 | Biocode 应用 | 设置 BiocodeWeaponChance | 按概率应用生物编码 |

---

### 2.3 IO 层测试

#### 2.3.1 PresetIOManager 测试

| 测试ID | 测试名称 | 测试目标 | 测试方法 | 预期结果 |
|--------|----------|----------|----------|----------|
| IO-PIM-001 | 导出为 Base64 | ExportToBase64() | 导出预设 | 返回有效的 Base64 字符串 |
| IO-PIM-002 | 导入为 Base64 | ImportFromBase64() | 导入导出的字符串 | 返回与原始数据一致的预设 |
| IO-PIM-003 | 空数据处理 | 导出/导入 null | 传入 null | 导出返回 null，导入返回 null |
| IO-PIM-004 | 无效 Base64 | 导入无效字符串 | 传入损坏的 Base64 | 返回 null |
| IO-PIM-005 | 预设完整性 | 复杂预设序列化 | 包含所有字段的预设 | 数据完整不丢失 |

---

### 2.4 UI 层测试

#### 2.4.1 FactionGearEditor 测试

| 测试ID | 测试名称 | 测试目标 | 测试方法 | 预期结果 |
|--------|----------|----------|----------|----------|
| UI-FGE-001 | 获取阵营兵种 | GetFactionKinds() | 传入 FactionDef | 返回排序后的兵种列表，无重复 |
| UI-FGE-002 | 标记脏状态 | MarkDirty() | 修改数据后调用 | IsDirty = true，生成备份 |
| UI-FGE-003 | 放弃更改 | DiscardChanges() | 修改后放弃 | 设置恢复到备份状态，IsDirty = false |
| UI-FGE-004 | 保存更改 | SaveChanges() | 保存设置 | 设置写入文件，IsDirty = false |
| UI-FGE-005 | 边界计算 | CalculateBounds() | 计算过滤边界 | 正确计算 Min/Max 值 |
| UI-FGE-006 | Mod 源获取 | GetUniqueModSources() | 获取 Mod 源列表 | 返回去重排序的列表 |
| UI-FGE-007 | 图标缓存 | GetIconWithLazyLoading() | 多次请求同一图标 | 缓存正确工作 |
| UI-FGE-008 | 详细提示 | GetDetailedItemTooltip() | 获取物品详细信息 | 返回格式化的详细信息 |
| UI-FGE-009 | 复制兵种装备 | CopyKindDefGear() | 复制当前兵种数据 | CopiedKindGearData 正确设置 |
| UI-FGE-010 | 批量应用 | ApplyToAllKindsInFaction() | 应用复制的装备到全阵营 | 所有兵种数据被更新 |

#### 2.4.2 UI State (EditorSession) 测试

| 测试ID | 测试名称 | 测试目标 | 测试方法 | 预期结果 |
|--------|----------|----------|----------|----------|
| UI-ES-001 | 状态重置 | ResetFilters() | 重置过滤条件 | 所有过滤条件恢复默认值 |
| UI-ES-002 | 分类切换 | SelectedCategory 设置 | 切换分类 | 触发相关缓存刷新 |

---

## 三、集成测试方案

### 3.1 游戏内调试功能

由于项目依赖 RimWorld 游戏环境，建议在游戏中实现调试功能：

```csharp
// 建议在 FactionGearEditor 中添加测试模式
public static void RunSelfTest()
{
    // 1. 测试数据模型
    TestGearItem();
    TestKindGearData();
    TestFactionGearData();
    
    // 2. 测试管理器
    TestFactionGearManager();
    TestUndoManager();
    
    // 3. 测试 IO
    TestPresetIOManager();
    
    Log.Message("[FactionGearCustomizer] Self-test completed.");
}
```

### 3.2 测试场景

| 场景ID | 场景名称 | 测试内容 | 预期结果 |
|--------|----------|----------|----------|
| INT-001 | 新游戏生成 | 创建新殖民地，生成 NPC | 自定义装备正确应用到 NPC |
| INT-002 | 阵营装备验证 | 验证特定阵营的装备配置 | 符合预设配置 |
| INT-003 | 预设导入导出 | 导出配置→新建存档→导入 | 配置完全一致 |
| INT-004 | 撤销重做 | 多次修改后撤销重做 | 数据正确恢复 |
| INT-005 | 大规模应用 | 应用到多个阵营多个兵种 | 性能可接受，无崩溃 |

---

## 四、测试执行建议

### 4.1 手动测试清单

1. **功能验证**:
   - [ ] 创建新游戏，验证装备应用到pawn
   - [ ] 测试预设保存/加载
   - [ ] 测试撤销/重做功能
   - [ ] 测试过滤器功能

2. **边界情况**:
   - [ ] 空阵营数据处理
   - [ ] 无效 ThingDef 处理
   - [ ] Mod 卸载后的数据处理

3. **性能测试**:
   - [ ] 大量阵营数据加载时间
   - [ ] UI 响应时间
   - [ ] 游戏生成性能影响

### 4.2 自动化测试建议

1. 创建独立的测试项目引用游戏 DLL
2. 使用 Moq/NSubstitute 模拟 RimWorld 依赖
3. 实现核心逻辑的单元测试

---

## 五、测试覆盖优先级

| 优先级 | 测试类别 | 覆盖类 |
|--------|----------|--------|
| 高 | 数据模型层 | GearItem, KindGearData, FactionGearData |
| 高 | 业务逻辑核心 | FactionGearManager, UndoManager |
| 中 | 业务逻辑应用 | GearApplier |
| 中 | IO 层 | PresetIOManager |
| 中 | UI 层核心 | FactionGearEditor |
| 低 | UI 面板 | 各 Panel 类 |

---

*文档版本: 1.0*  
*创建日期: 2026-02-21*
