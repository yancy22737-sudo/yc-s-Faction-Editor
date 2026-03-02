
# 持久化与预设功能交叉检查矩阵

## 一、数据类字段完整清单

### 1. FactionGearPreset.cs
| 字段 | ExposeData() | DeepCopy() | 预设IO | 备注 |
|------|-------------|-----------|--------|------|
| `name` | ✅ | ✅ | ✅ | 预设名称 |
| `description` | ✅ | ✅ | ✅ | 预设描述 |
| `factionGearData` | ✅ (Deep) | ✅ | ✅ | 派系数据列表 |
| `requiredMods` | ✅ | ✅ | ✅ | 所需Mod列表 |

### 2. FactionGearData.cs
| 字段 | ExposeData() | DeepCopy() | ResetToDefault() | 备注 |
|------|-------------|-----------|-----------------|------|
| `factionDefName` | ✅ | ✅ | ❌ | 派系Def名称 |
| `kindGearData` | ✅ (Deep) | ✅ | ✅ | 兵种数据列表 |
| `isModified` | ✅ | ✅ | ✅ | 是否已修改标记 |
| `Label` | ✅ | ✅ | ✅ | 派系标签 |
| `Description` | ✅ | ✅ | ✅ | 派系描述 |
| `IconPath` | ✅ | ✅ | ✅ | 图标路径 |
| `Color` | ✅ | ✅ | ✅ | 派系颜色 |
| `XenotypeChances` | ✅ | ✅ | ✅ | 异种类型概率字典 |
| `groupMakers` | ✅ (Deep) | ✅ | ✅ | 兵种群组生成器 |
| `PlayerRelationOverride` | ✅ | ✅ | ✅ | 玩家关系覆盖 |
| `kindGearDataDict` | ❌ | ❌ | ❌ | 字典索引(缓存，标记为[Unsaved]) |

### 3. KindGearData.cs
| 字段 | ExposeData() | DeepCopy() | CopyFrom() | ResetToDefault() | 备注 |
|------|-------------|-----------|-----------|-----------------|------|
| `kindDefName` | ✅ | ✅ | ✅ | ❌ | 兵种Def名称 |
| `Label` | ✅ | ✅ | ✅ | ✅ | 兵种标签 |
| `weapons` | ✅ (Deep) | ✅ | ✅ | ✅ | 武器列表 |
| `meleeWeapons` | ✅ (Deep) | ✅ | ✅ | ✅ | 近战武器列表 |
| `armors` | ✅ (Deep) | ✅ | ✅ | ✅ | 护甲列表 |
| `apparel` | ✅ (Deep) | ✅ | ✅ | ✅ | 服装列表 |
| `others` | ✅ (Deep) | ✅ | ✅ | ✅ | 其他物品列表 |
| `isModified` | ✅ | ✅ | ✅ | ✅ | 是否已修改 |
| `ForceNaked` | ✅ | ✅ | ✅ | ✅ | 强制裸体 |
| `ForceOnlySelected` | ✅ | ✅ | ✅ | ✅ | 只使用选中物品 |
| `ForceIgnoreRestrictions` | ✅ | ✅ | ✅ | ✅ | 忽略限制 |
| `ItemQuality` | ✅ | ✅ | ✅ | ✅ | 物品品质 |
| `ForcedWeaponQuality` | ✅ | ✅ | ✅ | ✅ | 武器品质 |
| `BiocodeWeaponChance` | ✅ | ✅ | ✅ | ✅ | 武器生物编码概率 |
| `BiocodeApparelChance` | ✅ | ✅ | ✅ | ✅ | 服装生物编码概率 |
| `TechHediffChance` | ✅ | ✅ | ✅ | ✅ | 技术健康问题概率 |
| `TechHediffsMaxAmount` | ✅ | ✅ | ✅ | ✅ | 技术健康问题最大数量 |
| `ApparelMoney` | ✅ | ✅ | ✅ | ✅ | 服装金钱范围 |
| `TechLevelLimit` | ✅ | ✅ | ✅ | ✅ | 科技等级限制 |
| `WeaponMoney` | ✅ | ✅ | ✅ | ✅ | 武器金钱范围 |
| `ApparelColor` | ✅ | ✅ | ✅ | ✅ | 服装颜色 |
| `TechHediffTags` | ✅ | ✅ | ✅ | ✅ | 技术健康问题标签 |
| `TechHediffDisallowedTags` | ✅ | ✅ | ✅ | ✅ | 不允许的技术健康问题标签 |
| `WeaponTags` | ✅ | ✅ | ✅ | ✅ | 武器标签 |
| `ApparelTags` | ✅ | ✅ | ✅ | ✅ | 服装标签 |
| `ApparelDisallowedTags` | ✅ | ✅ | ✅ | ✅ | 不允许的服装标签 |
| `ApparelRequired` | ✅ (特殊处理) | ✅ (浅拷贝?) | ✅ (浅拷贝?) | ✅ | 必需服装 (用defName存储) |
| `TechRequired` | ✅ (特殊处理) | ✅ (浅拷贝?) | ✅ (浅拷贝?) | ✅ | 必需技术物品 (用defName存储) |
| `SpecificApparel` | ✅ (Deep) | ✅ | ✅ | ✅ | 指定服装 |
| `SpecificWeapons` | ✅ (Deep) | ✅ | ✅ | ✅ | 指定武器 |
| `InventoryItems` | ✅ (Deep) | ✅ | ✅ | ✅ | 库存物品 |
| `ForcedHediffs` | ✅ (Deep) | ✅ | ✅ | ✅ | 强制健康问题 |

### 4. GearItem.cs
| 字段 | ExposeData() | DeepCopy() | 备注 |
|------|-------------|-----------|------|
| `thingDefName` | ✅ | ✅ | 物品Def名称 |
| `weight` | ✅ | ✅ | 权重 |
| `cachedThingDef` | ❌ | ❌ | 缓存(标记为[Unsaved]) |

### 5. ForcedHediff.cs
| 字段 | ExposeData() | DeepCopy() | ResolveReferences() | 备注 |
|------|-------------|-----------|-------------------|------|
| `hediffDefName` | ✅ | ✅ | ✅ | 健康问题Def名称 |
| `partsDefNames` | ✅ | ✅ | ✅ | 身体部位Def名称列表 |
| `cachedHediffDef` | ❌ | ❌ | ✅ | 缓存(标记为[Unsaved]) |
| `cachedParts` | ❌ | ❌ | ✅ | 缓存(标记为[Unsaved]) |
| `PoolType` | ✅ | ✅ | ❌ | 池类型 |
| `maxParts` | ✅ | ✅ | ❌ | 最大部位数 |
| `maxPartsRange` | ✅ | ✅ | ❌ | 最大部位数范围 |
| `chance` | ✅ | ✅ | ❌ | 概率 |
| `severityRange` | ✅ | ✅ | ❌ | 严重程度范围 |

### 6. SpecRequirementEdit.cs
| 字段 | ExposeData() | DeepCopy() | ResolveReferences() | 备注 |
|------|-------------|-----------|-------------------|------|
| `thingDefName` | ✅ | ✅ | ✅ | 物品Def名称 |
| `materialDefName` | ✅ | ✅ | ✅ | 材料Def名称 |
| `styleDefName` | ✅ | ✅ | ✅ | 风格Def名称 |
| `cachedThing` | ❌ | ❌ | ✅ | 缓存(标记为[Unsaved]) |
| `cachedMaterial` | ❌ | ❌ | ✅ | 缓存(标记为[Unsaved]) |
| `cachedStyle` | ❌ | ❌ | ✅ | 缓存(标记为[Unsaved]) |
| `Quality` | ✅ | ✅ | ❌ | 品质 |
| `Biocode` | ✅ | ✅ | ❌ | 生物编码 |
| `Color` | ✅ | ✅ | ❌ | 颜色 |
| `SelectionMode` | ✅ | ✅ | ❌ | 选择模式 |
| `SelectionChance` | ✅ | ✅ | ❌ | 选择概率 |
| `weight` | ✅ | ✅ | ❌ | 权重 |
| `CountRange` | ✅ | ✅ | ❌ | 数量范围 |
| `PoolType` | ✅ | ✅ | ❌ | 池类型 |

### 7. PawnGroupMakerData.cs
| 字段 | ExposeData() | DeepCopy() | CopyFrom() | 备注 |
|------|-------------|-----------|-----------|------|
| `kindDefName` | ✅ | ✅ | ✅ | 兵种群组Def名称 |
| `customLabel` | ✅ | ✅ | ✅ | 自定义标签 |
| `commonality` | ✅ | ✅ | ✅ | 常见度 |
| `maxTotalPoints` | ✅ | ✅ | ✅ | 最大总点数 |
| `options` | ✅ (Deep) | ✅ | ✅ | 选项列表 |
| `traders` | ✅ (Deep) | ✅ | ✅ | 商队列表 |
| `carriers` | ✅ (Deep) | ✅ | ✅ | 搬运工列表 |
| `guards` | ✅ (Deep) | ✅ | ✅ | 护卫列表 |

### 8. PawnGenOptionData.cs
| 字段 | ExposeData() | DeepCopy() | 备注 |
|------|-------------|-----------|------|
| `kindDefName` | ✅ | ✅ | 兵种Def名称 |
| `selectionWeight` | ✅ | ✅ | 选择权重 |

### 9. FactionGearGameComponent.cs (存档级别)
| 字段 | ExposeData() | 备注 |
|------|-------------|------|
| `activePresetName` | ✅ | 当前存档激活的预设名称 |
| `hasShownFirstTimePrompt` | ✅ | 是否已显示首次进入提示 |
| `savedFactionGearData` | ✅ (Deep) | 当前存档的派系装备数据 |
| `useCustomSettings` | ✅ | 是否使用自定义设置 |
| `saveUniqueIdentifier` | ✅ | 存档唯一标识符 |

### 10. FactionGearCustomizerSettings.cs (全局级别)
| 字段 | ExposeData() | 备注 |
|------|-------------|------|
| `version` | ✅ | 版本号 |
| `factionGearData` | ✅ (Deep) | 全局派系装备数据 |
| `presets` | ✅ (Deep) | 全局预设列表 |
| `forceIgnoreRestrictions` | ✅ | 强制忽略原版限制 |
| `ShowInMainTab` | ✅ | 在主标签中显示 |
| `ShowHiddenFactions` | ✅ | 显示隐藏派系 |
| `suppressDeleteGroupConfirmation` | ✅ | 抑制删除群组确认 |
| `autoSaveBeforePreview` | ✅ | 预览前自动保存 |
| `currentPresetName` | ✅ | 当前预设名称 |
| `previousSaveIdentifier` | ✅ | 上一个存档的唯一标识符 |
| `dismissedDialogs` | ✅ | 已关闭的对话框 |

---

## 二、发现的问题清单

### 🔴 高优先级问题

#### 问题1: FactionGearPreset.SaveFullFactionData() 有bug
**位置**: [FactionGearPreset.cs:99-125](file:///c:\Users\Administrator\source\repos\FactionGearModification\FactionGearModification\Data\FactionGearPreset.cs#L99-L125)

**问题描述**:
```csharp
// 第115行和123行有bug：
newFactionData.kindGearData.Add(newKindData); // ❌ 错误：添加到newFactionData自己的kindGearData里了！
```

**正确做法应该是**:
```csharp
newFactionData.kindGearData.Add(newKindData); // ❌ 错误
// 应该改为：
newFactionData.kindGearData.Add(newKindData); // 等等... 看代码：
// 第100行: FactionGearData newFactionData = new FactionGearData(faction.factionDefName);
// 然后在循环里：
// newKindData.kindGearData.Add(newKindData); ❌ 这是添加到newKindData自己的列表里！！
```

看第114-115行和121-123行：
```csharp
KindGearData newKindData = kind.DeepCopy();
newKindData.kindGearData.Add(newKindData);  // ❌ 致命bug！newKindData没有kindGearData字段！
```

**影响**: 保存预设时完全不保存兵种数据！预设是空的！

---

#### 问题2: KindGearData.DeepCopy() 和 CopyFrom() 中的 ApparelRequired/TechRequired 浅拷贝问题
**位置**: [KindGearData.cs:263-264](file:///c:\Users\Administrator\source\repos\FactionGearModification\FactionGearModification\Data\KindGearData.cs#L263-L264)

**问题描述**:
```csharp
if (this.ApparelRequired != null) copy.ApparelRequired = new List&lt;ThingDef&gt;(this.ApparelRequired); // ❌ 浅拷贝列表，但ThingDef引用没问题
if (this.TechRequired != null) copy.TechRequired = new List&lt;ThingDef&gt;(this.TechRequired); // ❌ 浅拷贝列表，但ThingDef引用没问题
```

虽然ExposeData中用defName正确处理，但DeepCopy()和CopyFrom()直接用ThingDef列表，而没有ResolveReferences()。

---

### 🟡 中优先级问题

#### 问题3: 预设导出/导入后没有调用 ResolveReferences()
**影响**: 
- PresetIOManager.ImportFromBase64() 导入预设后，SpecRequirementEdit、ForcedHediff 等类的引用没有重新解析
- 应该在导入后调用所有数据的 ResolveReferences()

---

#### 问题4: FactionGearGameComponent.ApplyFactionsFromPreset() 中没有清理 requiredMods
**影响**: 选择性应用派系时，不会验证或更新所需Mod列表

---

### 🟢 低优先级问题

#### 问题5: FactionGearData.ResetToDefault() 没有重置 factionDefName
**位置**: [FactionGearData.cs:97-113](file:///c:\Users\Administrator\source\repos\FactionGearModification\FactionGearModification\Data\FactionGearData.cs#L97-L113)

**影响**: 理论上重置后应该保持factionDefName，这个设计是合理的

---

## 三、预设功能与持久化功能交叉检查矩阵

| 数据流转路径 | 涉及类 | ExposeData()覆盖 | DeepCopy()覆盖 | ResolveReferences()调用 | 风险评估 |
|------------|-------|-----------------|---------------|-----------------------|---------|
| 全局设置 → 存档保存 | `FactionGearCustomizerSettings` → `FactionGearGameComponent` | ✅ | ✅ | ⚠️ 可能缺失 | 中等 |
| 预设保存 (当前设置 → 预设) | `FactionGearPreset.SaveFromCurrentSettings()` | ✅ | ❌ (有bug) | ❌ | 极高 |
| 预设加载 (预设 → 当前设置) | `FactionGearGameComponent.ApplyPresetToSave()` | ✅ | ✅ | ⚠️ 可能缺失 | 中等 |
| 预设导出 (预设 → Base64) | `PresetIOManager.ExportToBase64()` | ✅ | - | ❌ | 低 |
| 预设导入 (Base64 → 预设) | `PresetIOManager.ImportFromBase64()` | ✅ | - | ❌ | 高 |
| 派系选择性应用 | `FactionGearGameComponent.ApplyFactionsFromPreset()` | ✅ | ✅ | ❌ | 中 |

---

## 四、详细修复建议

### 修复1: 修复 FactionGearPreset.SaveFullFactionData() 的致命bug
**文件**: `FactionGearPreset.cs`

**问题代码 (第110-125行)**:
```csharp
if (saveAll)
{
    foreach (var kind in faction.kindGearData)
    {
        KindGearData newKindData = kind.DeepCopy();
        newKindData.kindGearData.Add(newKindData);  // ❌ 错误！
    }
}
else
{
    foreach (var kind in modifiedKinds)
    {
        KindGearData newKindData = kind.DeepCopy();
        newKindData.kindGearData.Add(newKindData);  // ❌ 错误！
    }
}
```

**修复后代码**:
```csharp
if (saveAll)
{
    foreach (var kind in faction.kindGearData)
    {
        KindGearData newKindData = kind.DeepCopy();
        newFactionData.kindGearData.Add(newKindData);  // ✅ 正确：添加到newFactionData的kindGearData
    }
}
else
{
    foreach (var kind in modifiedKinds)
    {
        KindGearData newKindData = kind.DeepCopy();
        newFactionData.kindGearData.Add(newKindData);  // ✅ 正确
    }
}
```

---

### 修复2: 在预设导入后调用 ResolveReferences()
**文件**: `PresetIOManager.cs`

**建议添加**:
在 `ImportFromBase64()` 返回预设前，遍历所有数据并调用 ResolveReferences()

---

### 修复3: 在 KindGearData.DeepCopy() 和 CopyFrom() 后考虑调用 ResolveReferences()
或者确保在数据同步流程中统一调用

---

## 五、总结

| 问题类别 | 数量 | 严重程度 |
|---------|------|---------|
| 致命bug (预设无法保存兵种数据) | 1 | 🔴 极高 |
| 引用解析缺失 | 2 | 🟡 中 |
| 浅拷贝潜在问题 | 2 | 🟡 中 |

**最紧急修复**: 问题1 (FactionGearPreset.SaveFullFactionData 的bug) - 这会导致预设完全无法保存兵种数据！
