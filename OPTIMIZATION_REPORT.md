# FactionGearModification 全面优化审查报告

> 审查日期: 2026-04-10
> 项目版本: v1.5.3
> 审查范围: 全部 96 个 C# 源文件、3 套语言 XML、7 个 Patch 文件

---

## 一、总结

### 总体评价: **可改进**

项目功能完整、版本迭代活跃，架构分层意图明确。但存在以下核心风险：

- **最大风险**: `GearApplier.cs`（2608行）和 `GearEditPanel.cs`（1760行）远超 800 行硬阈值，属于典型的"上帝类"，修改一处容易引发连锁问题
- **次要风险**: `EditorSession` 作为 40+ 公开可变静态字段的全局状态容器，任何代码都可以直接读写，无封装无保护
- **Patch 风险**: `Patch_FactionGearMainTabWindow` 对基类 `Window` 的 Patch 既无效又有害

---

## 二、发现列表（按优先级）

### P0 阻断 — 会崩溃/刷红字/数据损坏/严重性能问题

| # | 问题 | 文件 | 行号 | 说明 |
|---|------|------|------|------|
| P0-1 | 空 catch 块静默吞异常 | [GearApplier.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Managers/GearApplier.cs#L635) | 635-637 | `GetValidBodyPartsForHediff` 中 `catch {}` 完全吞掉异常，导致 Hediff 无法应用且无法排查 |
| P0-2 | ApplyCustomGear 顶层 catch 不回滚 | [GearApplier.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Managers/GearApplier.cs#L120) | 120-123 | 异常后 Pawn 处于半修改状态（武器已清除但新武器未装备），应回滚或保留原装备 |
| P0-3 | Patch 基类 Window.WindowOnGUI | [Patch_FactionGearMainTabWindow.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_FactionGearMainTabWindow.cs#L7) | 7 | 对所有 Mod 的所有窗口每帧执行 `is` 类型检查，且因派生类已重写方法，Patch 实际无效 |
| P0-4 | 回溯搜索无预算剪枝 | [GearApplier.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Managers/GearApplier.cs#L1703) | 1703-1722 | `TrySelectBestCoreCombinationByBudget` 最坏 20^4=160K 次迭代，每次 Pawn 生成执行 |
| P0-5 | 英文语言文件混入中文 | [FactionGearCustomizer.xml](file:///c:/Users/Administrator/source/repos/FactionGearModification/1.6/Languages/English/Keyed/FactionGearCustomizer.xml#L316) | 316 | `RangeFilterTooltip` 中 `Range上限取自当前候选列表` 未翻译 |

### P1 重要 — 逻辑隐患/耦合过高/难维护/易回归

| # | 问题 | 文件 | 行号 | 说明 |
|---|------|------|------|------|
| P1-1 | GearApplier.cs 2608行 | [GearApplier.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Managers/GearApplier.cs) | 全文件 | 超出 800 行阈值 3.3 倍，职责极度集中 |
| P1-2 | GearEditPanel.cs 1760行 | [GearEditPanel.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/Panels/GearEditPanel.cs) | 全文件 | 超出 800 行阈值 2.2 倍 |
| P1-3 | EditorSession 40+ 公开可变静态字段 | [EditorSession.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/State/EditorSession.cs) | 全文件 | 任何代码可直接读写，无封装无保护 |
| P1-4 | Patch_GeneratePawn 包含大量业务逻辑 | [Patch_GeneratePawn.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_GeneratePawn.cs) | 68-341 | 异种/年龄设置逻辑应委托给 Manager 层 |
| P1-5 | Patch_GameInit 包含生命周期管理逻辑 | [Patch_GameInit.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_GameInit.cs) | 79-457 | 存档同步/重置/清理等核心业务不应写在 Patch 中 |
| P1-6 | UI 直接执行文件 IO | [CustomIconManager.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/CustomIconManager.cs) | 21-87 | 位于 UI 命名空间却承担完整文件 IO 职责 |
| P1-7 | UI 直接设置业务层状态 | [PresetManagerWindow.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/PresetManagerWindow.cs#L805) | 805 | `GearApplier.PreviewPreset = ...` UI 直接修改 Manager 层静态字段 |
| P1-8 | Patch 反向依赖 UI 层 | [Patch_GameInit.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_GameInit.cs#L177) | 177-404 | 直接操控 EditorSession/FactionGearEditor |
| P1-9 | Prefix 修改共享 FactionDef 状态 | [Patch_GeneratePawn.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_GeneratePawn.cs#L68) | 68-128 | 修改 `xenotypeSet` 后未在 Postfix 恢复，可能污染共享数据 |
| P1-10 | FactionEditWindow.cs 994行 | [FactionEditWindow.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/FactionEditWindow.cs) | 全文件 | 超出 800 行阈值 |
| P1-11 | PresetManagerWindow.cs 932行 | [PresetManagerWindow.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/PresetManagerWindow.cs) | 全文件 | 超出 800 行阈值 |
| P1-12 | FactionDefManager.cs 853行 | [FactionDefManager.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Managers/FactionDefManager.cs) | 全文件 | 超出 800 行阈值 |
| P1-13 | ItemLibraryPanel.cs 838行 | [ItemLibraryPanel.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/Panels/ItemLibraryPanel.cs) | 全文件 | 超出 800 行阈值 |
| P1-14 | Dialog_PawnGroupGenerationPreview.cs 803行 | [Dialog_PawnGroupGenerationPreview.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/Dialogs/Dialog_PawnGroupGenerationPreview.cs) | 全文件 | 超出 800 行阈值 |
| P1-15 | FactionDefManager 读取操作无锁 | [FactionDefManager.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Managers/FactionDefManager.cs#L51) | 51,90 | 写入有锁但读取无锁，可能读到不一致数据 |
| P1-16 | General 键重复且中文含义不一致 | [FactionGearCustomizer.xml](file:///c:/Users/Administrator/source/repos/FactionGearModification/1.6/Languages/ChineseSimplified/Keyed/FactionGearCustomizer.xml) | 98/1051 | 第一次="常规"，第二次="高级设置"，后定义覆盖前定义 |

### P2 优化 — 可读性/命名/重复代码/体验改进

| # | 问题 | 文件 | 行号 | 说明 |
|---|------|------|------|------|
| P2-1 | 10处硬编码 UI 文本 | KindListPanel, Dialog_PawnKindPicker 等 | 多处 | 未使用 Translate() 获取语言键 |
| P2-2 | 大量魔法数字 | GearApplier, FactionDefManager 等 | 多处 | 999f, 75f, -75f, 0.5f, 0.95f, 0.4f, 0.1f 等应提取为常量 |
| P2-3 | 俄文缺少 TargetFaction 键 | [Russian XML](file:///c:/Users/Administrator/source/repos/FactionGearModification/1.6/Languages/Russian/Keyed/FactionGearCustomizer.xml) | - | 删除派系区域缺少翻译 |
| P2-4 | RimTalkUnderDev/SeverityRange 仅中文有 | English/Russian XML | - | 英文和俄文缺失这两个键 |
| P2-5 | Patch_GameInit 死代码 | [Patch_GameInit.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_GameInit.cs#L265) | 265-292 | `RestoreAllFactionDefsToOriginal` 无任何调用者 |
| P2-6 | 文件名与类名不匹配 | [Patch_Page_SelectScenario.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_Page_SelectScenario.cs) | 全文件 | 文件名暗示"选择剧本"，实际 Patch `Page_CreateWorldParams` |
| P2-7 | 同一方法被两个嵌套类 Patch | [Patch_GameInit.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/Patches/Patch_GameInit.cs) | 39/344 | 缺少显式优先级控制 |
| P2-8 | Debug 文件未排除 | Debug_HediffApplicationTest.cs, Debug_FactionLeaderTest.cs | 全文件 | 调试代码不应包含在发布版本中 |
| P2-9 | 根目录散落临时文件 | tmp_log_cleaned.txt, ersAdministratorsourcerepos... | 根目录 | 应清理或加入 .gitignore |
| P2-10 | 语言文件重复键 | 全部3套语言 XML | 多处 | Weight×3, Yes×3, No×2, ClearAll×2 等，后定义覆盖前定义 |
| P2-11 | CustomIconManager 缓存 Texture2D 可能被 Unity 销毁 | [CustomIconManager.cs](file:///c:/Users/Administrator/source/repos/FactionGearModification/FactionGearModification/UI/CustomIconManager.cs#L50) | 50 | 缓存命中后未验证 Unity 对象有效性 |
| P2-12 | "Custom:" 前缀长度硬编码为 7 | Patch_FactionDef_FactionIcon.cs:21 等 | 多处 | 应提取为常量 |

---

## 三、各维度详细分析

### 3.1 代码架构与分层

#### 超标文件统计（7个文件超过 800 行硬阈值）

| 文件 | 行数 | 超出倍数 | 严重程度 |
|------|------|----------|----------|
| GearApplier.cs | 2608 | 3.3x | 极严重 |
| GearEditPanel.cs | 1760 | 2.2x | 极严重 |
| FactionEditWindow.cs | 994 | 1.2x | 严重 |
| PresetManagerWindow.cs | 932 | 1.2x | 严重 |
| FactionDefManager.cs | 853 | 1.1x | 中等 |
| ItemLibraryPanel.cs | 838 | 1.0x | 轻微 |
| Dialog_PawnGroupGenerationPreview.cs | 803 | 1.0x | 轻微 |

#### 跨层依赖问题

```
发现的不合规依赖方向：

UI ──→ IO      CustomIconManager 直接读写文件
UI ──→ IO      VersionLogWindow 直接读取文件
UI ──→ IO      Dialog_ImportIcon 直接读取文件
UI ──→ Manager  PresetManagerWindow 直接设置 GearApplier.PreviewPreset
UI ──→ Data     KindListPanel 直接 LINQ 查询 Settings.factionGearData
Patch ──→ UI    Patch_GameInit 直接操控 EditorSession/FactionGearEditor
Patch ──→ Logic Patch_GeneratePawn 包含异种/年龄业务逻辑
```

#### EditorSession 全局状态容器

`EditorSession` 包含约 40+ 个公开可变静态字段，涵盖：
- 选择状态（阵营/兵种/分类/Mod来源等）
- UI 滚动位置（5个 Vector2）
- 过滤器状态（搜索文本/范围/伤害/价值等）
- 缓存数据（Mod来源/弹药集/过滤物品等）
- 缓存失效检查变量（6个 Last* 字段）
- 剪贴板（CopiedKindGearData）

**风险**: 任何代码都可以直接读写，无封装无保护；多窗口/多面板同时操作时容易产生竞态条件。

---

### 3.2 逻辑正确性与边界

#### 异常处理问题

| 位置 | 问题 | 建议 |
|------|------|------|
| GearApplier.cs:635-637 | `catch {}` 完全吞掉异常 | 至少记录 `Log.Warning` |
| GearApplier.cs:120-123 | 顶层 catch 只记录不回滚 | 考虑在 catch 中恢复原始装备 |
| ItemLibraryPanel.cs:581 | `catch { return 0f; }` 掩盖 API 变更 | 记录一次警告日志 |
| GearApplier.cs:451-477 | 嵌套 catch 只记录 Message 丢失堆栈 | 使用 `ex.ToString()` 或 `ex.StackTrace` |
| FactionDefManager.cs:473-476 | catch 只记录 Message | 使用完整异常信息 |

#### 魔法数字清单

| 值 | 含义 | 出现位置 | 建议常量名 |
|----|------|----------|-----------|
| 999f | 默认最大年龄 | Patch_GeneratePawn.cs:346, Dialog_PawnGroupGenerationPreview.cs:711 | `DefaultMaxAge` |
| 75f / -75f | 盟友/敌对好感度阈值 | FactionDefManager.cs:408-411 | `AllyGoodwillThreshold` / `HostileGoodwillThreshold` |
| -0.5f | 昏迷/失能能力偏移阈值 | GearApplier.cs:189 | `IncapacitatingCapOffsetThreshold` |
| 0.5f | 负面 Hediff 危险阈值 | GearApplier.cs:237 | `DangerousHediffSeverityThreshold` |
| 0.95f | 致命严重程度安全系数 | GearApplier.cs:733, HediffApplicationService.cs:331 | `LethalSeveritySafetyFactor` |
| 0.4f | 护甲评级阈值 | GearApplier.cs:1606 | `ArmorRatingThreshold` |
| 0.1f | 药物成瘾性分界 | GearApplier.cs:2462,2474 | `SocialDrugAddictivenessThreshold` |
| 7 | "Custom:" 前缀长度 | Patch_FactionDef_FactionIcon.cs:21 等 | `CustomIconPrefixLength` |
| 100 | 物品数量上限 | ItemCardUI.cs:357,487 | `MaxItemCount` |
| 500 | 图标缓存大小 | FactionGearEditor.cs:22 | `MaxIconCacheSize` |

---

### 3.3 Harmony Patch 问题

| 严重度 | Patch 文件 | 问题 | 建议 |
|--------|-----------|------|------|
| **严重** | Patch_FactionGearMainTabWindow | 对基类 Window.WindowOnGUI Patch，影响所有窗口；且因派生类已重写方法，Patch 实际无效 | **直接删除此文件** |
| 中等 | Patch_FactionDef_FactionIcon | 属性 Getter 在热路径频繁调用，缓存 Texture2D 可能被 Unity 销毁 | 增加缓存有效性检查 |
| 中等 | Patch_GeneratePawn | Prefix 修改共享 FactionDef.xenotypeSet，存在竞态风险 | 在 Postfix 中恢复原始状态 |
| 中等 | Patch_GameInit | MainMenuDrawer.DoMainMenuControls 每帧调用 | 考虑事件驱动方式替代轮询 |
| 低 | Patch_GameInit | 同一方法被两个嵌套类 Patch，缺少显式优先级 | 合并或添加 HarmonyPriority |
| 低 | Patch_GameInit | RestoreAllFactionDefsToOriginal 是死代码 | 删除 |
| 低 | Patch_Page_SelectScenario | 文件名与类名/Patch 目标不匹配 | 重命名为 Patch_Page_CreateWorldParams |

---

### 3.4 多语言适配

#### 硬编码 UI 文本（10处严重）

| 文件 | 行号 | 硬编码文本 | 语言 |
|------|------|-----------|------|
| Dialog_PawnGroupGenerationPreview.cs | 342 | `"原："` | 中文 |
| Dialog_KindXenotypeEditor.cs | 360 | `"(超过100%)"` | 中文 |
| KindListPanel.cs | 230 | `"Copied gear from "` | 英文 |
| KindListPanel.cs | 254 | `"Pasted gear to "` | 英文 |
| KindListPanel.cs | 233 | `"Copy this KindDef's gear"` | 英文 |
| KindListPanel.cs | 258 | `"Paste gear to this KindDef"` | 英文 |
| Dialog_PawnKindPicker.cs | 241 | `"Mod: "` | 英文 |
| Dialog_TextureBrowser.cs | 142 | `"Delete"` | 英文 |
| VersionLogWindow.cs | 66-67 | `"VersionLog.txt not found."` | 英文 |
| VersionLogWindow.cs | 73 | `"Error loading VersionLog: "` | 英文 |

#### 语言文件键缺失

| 键名 | 中文 | 英文 | 俄文 | 建议 |
|------|------|------|------|------|
| RimTalkUnderDev | ✅ | ❌ | ❌ | 英文添加 "Under Development"，俄文添加翻译 |
| SeverityRange | ✅ | ❌ | ❌ | 英文添加 "Severity Range"，俄文添加翻译 |
| TargetFaction(删除派系区域) | ✅ | ✅ | ❌ | 俄文添加翻译 |

#### 语言文件翻译问题

| 问题 | 文件 | 行号 | 详情 |
|------|------|------|------|
| 英文混入中文 | English XML | 316 | `RangeFilterTooltip` 中 "Range上限取自当前候选列表" 未翻译 |
| General 键重复含义冲突 | ChineseSimplified XML | 98/1051 | 第一次="常规"，第二次="高级设置"，后者覆盖前者 |
| CategoryAll 重复措辞不同 | ChineseSimplified XML | 77/879 | "类别：全部" vs "全部类别" |

---

### 3.5 数据结构与持久化

#### 关键风险点

| 问题 | 文件 | 说明 |
|------|------|------|
| SpecRequirementEdit/ForcedHediff 保存 defName 而非 Def 引用 | Data/ | v1.3.1 已修复，但需持续关注新字段是否遵循同样模式 |
| KindGearData.DeepCopy 深拷贝完整性 | KindGearData.cs | 593行，需确保新增字段同步更新 DeepCopy |
| FactionGearPreset 序列化版本兼容 | FactionGearPreset.cs | 需要迁移策略确保旧预设可加载 |

---

### 3.6 项目卫生

| 问题 | 位置 | 建议 |
|------|------|------|
| Debug 文件未排除 | Debug_HediffApplicationTest.cs, Debug_FactionLeaderTest.cs | 使用 `#if DEBUG` 包裹或从发布构建排除 |
| 根目录散落临时文件 | tmp_log_cleaned.txt, ersAdministratorsourcerepos... | 清理并加入 .gitignore |
| AMMO_QUICKSTART.md / AMMO_SYSTEM_README.md | 项目源码目录 | 开发文档应移至 .trae/documents 或 docs 目录 |
| Mod-mainTab-UI-issue.md | 项目源码目录 | 已解决的问题记录，应归档 |

---

## 四、建议与落地

### 4.1 最小改动修复（P0 级别）

1. **删除 Patch_FactionGearMainTabWindow.cs** — 此 Patch 既无效又有害，`FactionGearMainTabWindow.PreOpen()` 已设置 `doWindowBackground = true`
2. **修复空 catch 块** — `GearApplier.cs:635` 的 `catch {}` 改为 `catch (Exception ex) { Log.Warning($"...{ex.Message}"); }`
3. **修复英文语言文件混入中文** — `RangeFilterTooltip` 中的中文翻译为英文
4. **为回溯搜索添加预算剪枝** — 在 `Search` 方法中当 `totalCost > budget` 时提前返回

### 4.2 结构性重构（P1 级别，按优先级排序）

#### 第一阶段: 拆分上帝类

| 目标文件 | 当前行数 | 拆分方案 |
|----------|---------|---------|
| GearApplier.cs | 2608 | 拆为: `WeaponApplier` / `ApparelApplier` / `InventoryApplier` / `HediffApplier` / `ApparelBudgetEngine` |
| GearEditPanel.cs | 1760 | 拆为: `SimpleModePanel` / `AdvancedModePanel` / `GearTabControl` |
| FactionEditWindow.cs | 994 | 拆为: `FactionBasicInfoPanel` / `FactionKindListPanel` / `FactionGroupListPanel` |
| PresetManagerWindow.cs | 932 | 拆为: `PresetListPanel` / `PresetActionsPanel` |

#### 第二阶段: 修复跨层依赖

1. **CustomIconManager 迁移到 IO 层** — 将文件读写逻辑移至 `IO/IconIOManager.cs`，UI 层仅调用接口
2. **Patch 业务逻辑下沉** — 将 `Patch_GeneratePawn` 中的异种/年龄逻辑移至 `Managers/XenotypeManager.cs` 和 `Managers/AgeSettingsManager.cs`
3. **Patch_GameInit 生命周期管理下沉** — 将存档同步/重置/清理逻辑移至 `Core/SaveLifecycleManager.cs`
4. **PreviewPreset 封装** — 通过 `GearApplier.SetPreviewPreset()` / `ClearPreviewPreset()` 方法替代直接赋值

#### 第三阶段: EditorSession 重构

将 `EditorSession` 从 40+ 公开静态字段重构为：
```
EditorSession (封装类)
├── SelectionState    (阵营/兵种/分类选择)
├── FilterState       (搜索/范围/排序过滤)
├── ScrollState       (各面板滚动位置)
├── CacheState        (缓存数据与失效标记)
└── ClipboardState    (复制/粘贴数据)
```
各子类通过属性暴露读写，内部可添加校验和变更通知。

### 4.3 回归清单

完成上述修改后，需验证以下用户路径：

1. **基础装备流程**: 选择阵营 → 选择兵种 → 添加武器 → 保存 → 预览 → 验证装备正确
2. **批量操作**: 复制兵种配置 → 批量粘贴 → 撤销 → 验证数据一致
3. **预设管理**: 新建预设 → 导出 → 删除 → 导入 → 验证数据完整
4. **存档切换**: 存档A修改 → 保存 → 返回主菜单 → 加载存档B → 验证数据隔离
5. **Hediff 应用**: 添加 Hediff → 指定部位 → 预览 → 验证部位正确且不倒地
6. **群组生成**: 修改袭击群组 → 触发袭击 → 验证兵种组成
7. **多语言切换**: 英文 → 中文 → 俄文 → 验证无硬编码文本暴露
8. **CE 兼容**: 安装 CE → 配置武器 → 生成 Pawn → 验证弹药自动添加

---

## 五、项目健康度评分

| 维度 | 评分 (1-10) | 说明 |
|------|------------|------|
| 功能完整性 | 9 | 功能丰富，覆盖装备/群组/派系/Hediff全链路 |
| 代码架构 | 5 | 分层意图明确但执行不彻底，7个文件超标 |
| 逻辑正确性 | 6 | 核心路径有保护，但空catch/半修改状态存在隐患 |
| 可维护性 | 4 | GearApplier 2608行是最大痛点，修改成本极高 |
| 多语言适配 | 7 | 主链路已适配，但10处硬编码和3处键缺失需修复 |
| Patch 安全性 | 5 | 存在无效Patch和共享状态污染风险 |
| 项目卫生 | 5 | Debug文件/临时文件未清理，文档散落 |
| **综合** | **5.9** | **可改进** — 核心功能扎实，但架构债务需要系统性偿还 |
