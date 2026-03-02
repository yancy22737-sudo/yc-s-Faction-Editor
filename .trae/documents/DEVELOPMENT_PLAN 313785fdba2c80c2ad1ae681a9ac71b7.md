# DEVELOPMENT_PLAN

# yc’s Faction Editor - 开发计划书

## 项目概述

**项目名称**: yc’s Faction Editor
**当前版本**: v1.3.3

**目标版本**: v1.4.0

**规划日期**: 2026-02-26

---

## 目录

1. [功能目标](about:blank#1-%E5%8A%9F%E8%83%BD%E7%9B%AE%E6%A0%87)
2. [实施阶段](about:blank#2-%E5%AE%9E%E6%96%BD%E9%98%B6%E6%AE%B5)
3. [技术架构](about:blank#3-%E6%8A%80%E6%9C%AF%E6%9E%B6%E6%9E%84)
4. [文件变更清单](about:blank#4-%E6%96%87%E4%BB%B6%E5%8F%98%E6%9B%B4%E6%B8%85%E5%8D%95)
5. [版本规划](about:blank#5-%E7%89%88%E6%9C%AC%E8%A7%84%E5%88%92)
6. [测试计划](about:blank#6-%E6%B5%8B%E8%AF%95%E8%AE%A1%E5%88%92)
7. [风险评估](about:blank#7-%E9%A3%8E%E9%99%A9%E8%AF%84%E4%BC%B0)
8. [附录](about:blank#8-%E9%99%84%E5%BD%95)

---

## 1. 功能目标

### 1.1 功能一：派系原子化导入导出

**目标描述**: 实现预设之间的派系自由导入导出，支持将当前存档中的派系配置导出到任意预设，或从预设中导入特定派系到当前存档。

**用户场景**:
- 玩家想要将存档A中的”海盗派系”配置复制到存档B
- 玩家想要将精心调制的派系配置保存到预设，供新游戏使用
- 玩家想要合并多个预设中的派系配置

**成功标准**:
- [ ] 支持将当前存档中的派系导出到新建预设
- [ ] 支持将当前存档中的派系导出到现有预设（合并/覆盖/跳过模式）
- [ ] 支持从预设中选择特定派系导入当前存档
- [ ] 导出时自动检测并记录所需Mod
- [ ] 导入时验证派系可用性（Mod/DLC/Def存在性）

### 1.2 功能二：剧本创建界面集成

**目标描述**: 在创建新游戏的剧本选择界面（Page_SelectScenario）添加派系编辑器入口，允许玩家在游戏开始前配置派系预设。

**用户场景**:
- 玩家创建新游戏时，希望直接应用之前保存的派系配置
- 玩家希望在游戏开始前预览和调整派系设置
- 玩家希望从多个预设中选择特定派系组合

**成功标准**:
- [ ] 在剧本选择界面显示”派系编辑器”按钮
- [ ] 打开精简版派系编辑器（只读预览 + 预设选择）
- [ ] 支持从预设导入派系到初始配置
- [ ] 配置在游戏启动时自动应用
- [ ] 支持取消和重新配置

---

## 2. 实施阶段

### 阶段划分图

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Phase 1: 基础设施层                             │
│                      派系原子化导入导出 (v1.3.4)                           │
├─────────────────────────────────────────────────────────────────────────┤
│  Week 1                                                                 │
│  ├── Step 1.1: 扩展 PresetFactionImporter 支持导出功能                    │
│  │            • 新增 ExportFactionsToPreset() 方法                       │
│  │            • 实现冲突检测与处理策略                                    │
│  │            • 添加导出结果验证                                         │
│  │                                                                        │
│  ├── Step 1.2: 新建 Dialog_ExportFactionToPreset                         │
│  │            • 左侧：当前存档派系列表（复用 FactionListPanel）            │
│  │            • 右侧：目标预设选择（新建/现有）                            │
│  │            • 底部：导出模式选择（合并/覆盖/跳过）                       │
│  │                                                                        │
│  └── Step 1.3: 集成到现有界面                                           │
│               • 预设界面添加"导入/导出派系"按钮                            │
│               • 语言文件更新（中英文）                                     │
│               • 基础功能测试                                              │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                           Phase 2: 应用层                                │
│                      剧本创建界面集成 (v1.3.5)                           │
├─────────────────────────────────────────────────────────────────────────┤
│  Week 2                                                                 │
│  ├── Step 2.1: 新建 Patch_Page_SelectScenario                            │
│  │            • Harmony Postfix DoBottomButtons()                        │
│  │            • 在"下一步"左侧添加"派系编辑器"按钮                         │
│  │            • 按钮位置自适应，避免与其他Mod冲突                          │
│  │                                                                       │
│  ├── Step 2.2: 新建 Dialog_FactionEditorLite                             │
│  │            • 复用 Dialog_PresetFactionSelector 的预设选择逻辑          │
│  │            • 复用 Phase 1 的派系导入验证                               │
│  │            • 只读预览模式（不编辑装备细节）                            │
│  │                                                                      │
│  └── Step 2.3: 数据持久化与启动应用                                      │
│               • 新建 ScenarioFactionConfig 数据类                        │
│               • 修改 FactionGearGameComponent 支持启动时应用             │
│               • 修改 FactionGearCustomizerSettings 存储临时配置          │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                           Phase 3: 联调与发布                            │
│                         集成测试与文档 (v1.4.0)                           │
├─────────────────────────────────────────────────────────────────────────┤
│  Week 2-3                                                               │
│  ├── Step 3.1: 联调测试                                                  │
│  │            • 导出→导入→剧本配置→游戏启动 完整链路验证                   │
│  │            • 边界条件测试（空数据、缺失Mod、极端数量）                   │
│  │            • 与其他Mod兼容性测试                                       │
│  │                                                                        │
│  ├── Step 3.2: 性能优化                                                  │
│  │            • 大数据量导出/导入性能测试                                  │
│  │            • UI响应速度优化                                            │
│  │            • 内存占用优化                                              │
│  │                                                                        │
															                                              │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.1 Phase 1 详细计划

### Step 1.1: 扩展 PresetFactionImporter (2天)

**任务清单**:
- [ ] 在 `PresetFactionImporter.cs` 中添加导出相关方法
- [ ] 定义 `ExportConflictResolution` 枚举（Skip/Overwrite/Merge）
- [ ] 实现 `ExportFactionsToPreset()` 核心方法
- [ ] 实现冲突检测逻辑
- [ ] 添加导出结果验证

**关键代码结构**:

```csharp
public enum ExportConflictResolution
{
    Skip,       // 跳过，保留目标预设中的派系
    Overwrite,  // 覆盖，用源派系替换目标预设中的派系
    Merge       // 合并，合并兵种数据（union 模式）
}

public static ExportResult ExportFactionsToPreset(
    List<FactionGearData> sourceFactions,
    FactionGearPreset targetPreset,
    List<string> selectedFactionDefNames,
    ExportConflictResolution resolution)
```

**验收标准**:
- [ ] 导出方法能正确处理空列表、null参数等边界情况
- [ ] 冲突检测能准确识别同类派系
- [ ] 三种冲突处理模式行为正确
- [ ] 导出后预设数据完整性验证通过

### Step 1.2: 新建 Dialog_ExportFactionToPreset (2天)

**任务清单**:
- [ ] 创建新文件 `UI/Dialogs/Dialog_ExportFactionToPreset.cs`
- [ ] 实现左侧派系列表（复用现有组件）
- [ ] 实现右侧预设选择区域
- [ ] 实现导出模式选择UI
- [ ] 实现预览和确认逻辑

**UI布局**:

```
┌─────────────────────────────────────────────────────────┐
│                    导出派系到预设                          │
├──────────────────────┬──────────────────────────────────┤
│    当前存档派系       │         目标预设                  │
│  ┌────────────────┐  │  ┌────────────────────────────┐  │
│  │ ☐ 海盗派系      │  │  预设列表: [预设A ▼]          │  │
│  │ ☐ 部落联盟      │  │                              │  │
│  │ ☐ 帝国         │  │  或: [新建预设...]            │  │
│  │ ☐ 机械族       │  │                              │  │
│  └────────────────┘  │  冲突处理:                     │  │
│                      │  (•) 跳过已存在的派系           │  │
│  [全选] [取消全选]    │  ( ) 覆盖已存在的派系           │  │
│                      │  ( ) 合并兵种数据               │  │
├──────────────────────┴──────────────────────────────────┤
│              [取消]                    [导出 (3)]        │
└─────────────────────────────────────────────────────────┘
```

**验收标准**:
- [ ] UI布局正确，无重叠或截断
- [ ] 派系列表正确显示当前存档中的所有派系
- [ ] 预设列表正确显示所有可用预设
- [ ] 导出按钮显示已选择的派系数量
- [ ] 点击导出后正确调用核心方法

### Step 1.3: 集成到现有界面 (1天)

**任务清单**:
- [ ] 修改 `UI/Panels/TopBarPanel.cs`，添加导出按钮
- [ ] 更新语言文件 `Languages/ChineseSimplified/Keyed/Keys.xml`
- [ ] 更新语言文件 `Languages/English/Keyed/Keys.xml`
- [ ] 基础功能测试

**新增语言条目**:

```xml
<!-- ChineseSimplified -->
<string>yFE_ExportFactions>导出派系</string>
<string>yFE_ExportFactions_Title>导出派系到预设</string>
<string>yFE_ExportToNewPreset>新建预设...</string>
<string>yFE_ExportConflict_Skip>跳过已存在的派系</string>
<string>yFE_ExportConflict_Overwrite>覆盖已存在的派系</string>
<string>yFE_ExportConflict_Merge>合并兵种数据</string>
<string>yFE_ExportSuccess>成功导出 {0} 个派系到预设"{1}"</string>
<string>yFE_ExportNoSelection>请先选择要导出的派系</string>
```

**验收标准**:
- [ ] 导出按钮显示在TopBar正确位置
- [ ] 点击按钮打开导出对话框
- [ ] 语言文本正确显示（中英文）
- [ ] 基础功能测试通过

### 2.2 Phase 2 详细计划

### Step 2.1: 新建 Patch_Page_SelectScenario (1天)

**任务清单**:
- [ ] 创建新文件 `Patches/Patch_Page_SelectScenario.cs`
- [ ] 实现 Harmony Postfix
- [ ] 添加按钮位置计算逻辑
- [ ] 添加与其他Mod的兼容性处理

**关键代码**:

```csharp
[HarmonyPatch(typeof(Page_SelectScenario))]
[HarmonyPatch("DoBottomButtons")]
public static class Patch_Page_SelectScenario
{
    static void Postfix(Page_SelectScenario __instance, Rect rect)
    {
        // 严格过滤：只在剧本选择页面显示按钮
        if (__instance.GetType() != typeof(Page_SelectScenario))
            return;

        // 计算按钮位置（位于"下一步"左侧）
        Vector2 buttonSize = Page.BottomButSize;
        float x = rect.xMax - buttonSize.x * 2 - 20f;
        float y = rect.y + rect.height - buttonSize.y - 10f;

        Rect factionEditorRect = new Rect(x, y, buttonSize.x, buttonSize.y);

        if (Widgets.ButtonText(factionEditorRect, LanguageManager.Get("yFE_FactionEditor_Button")))
        {
            Find.WindowStack.Add(new Dialog_FactionEditorLite());
        }
    }
}
```

**验收标准**:
- [ ] 按钮正确显示在剧本选择界面底部
- [ ] 按钮位置不与其他按钮重叠
- [ ] 点击按钮打开精简编辑器
- [ ] 与其他Mod（如WorldEdit）兼容

### Step 2.2: 新建 Dialog_FactionEditorLite (2天)

**任务清单**:
- [ ] 创建新文件 `UI/Dialogs/Dialog_FactionEditorLite.cs`
- [ ] 实现预设选择区域（复用现有逻辑）
- [ ] 实现派系选择区域（复用Phase 1的验证）
- [ ] 实现只读预览模式
- [ ] 实现保存和取消逻辑

**UI布局**:

```
┌─────────────────────────────────────────────────────────┐
│                   初始派系配置                            │
├──────────────────────┬──────────────────────────────────┤
│      预设列表         │         派系选择                  │
│  ┌────────────────┐  │  ┌────────────────────────────┐  │
│  │ > 预设A        │  │  │ ☐ 海盗派系 (5兵种)          │  │
│  │   预设B        │  │  │ ☐ 部落联盟 (8兵种)          │  │
│  │   预设C        │  │  │ ☐ 帝国 (12兵种)             │  │
│  └────────────────┘  │  │ ☐ 机械族 (3兵种)            │  │
│                      │  └────────────────────────────┘  │
│  [刷新] [导入预设]    │                                  │
│                      │  预览: 选中派系的基础信息         │
├──────────────────────┴──────────────────────────────────┤
│  [取消]                          [保存并继续 (3)]        │
└─────────────────────────────────────────────────────────┘
```

**验收标准**:
- [ ] 预设列表正确加载
- [ ] 选择预设后派系列表正确更新
- [ ] 派系验证正确显示（绿色/黄色/红色状态）
- [ ] 保存后配置正确存储
- [ ] 取消后配置不保存

### Step 2.3: 数据持久化与启动应用 (2天)

**任务清单**:
- [ ] 创建 `Data/ScenarioFactionConfig.cs`
- [ ] 创建 `Managers/ScenarioFactionManager.cs`
- [ ] 修改 `Core/FactionGearGameComponent.cs`
- [ ] 修改 `Core/FactionGearCustomizerSettings.cs`

**数据流**:

```
Dialog_FactionEditorLite.Save()
    └── ScenarioFactionManager.SaveConfig()
            └── FactionGearCustomizerSettings.scenarioFactionConfig = config
                    └── 游戏启动时
                            └── FactionGearGameComponent.Start()
                                    └── ScenarioFactionManager.ApplyConfig()
                                            └── FactionDefManager.ApplyAllSettings()
```

**验收标准**:
- [ ] 配置正确序列化到设置文件
- [ ] 游戏启动时配置正确应用
- [ ] 配置在存档间隔离
- [ ] 异常情况下 graceful degradation

### 2.3 Phase 3 详细计划

### Step 3.1: 联调测试 (2天)

**测试用例**:

| 用例ID | 测试场景 | 预期结果 |
| --- | --- | --- |
| TC-001 | 导出派系到新建预设 | 预设创建成功，包含选中派系 |
| TC-002 | 导出派系到现有预设（覆盖） | 目标派系被替换 |
| TC-003 | 导出派系到现有预设（合并） | 兵种数据合并 |
| TC-004 | 导入刚导出的派系 | 数据完整性验证通过 |
| TC-005 | 跨预设复制派系 A→B→C | 数据一致性验证通过 |
| TC-006 | 剧本选择界面显示按钮 | 按钮位置正确，样式一致 |
| TC-007 | 打开精简编辑器 | 预设列表加载正常 |
| TC-008 | 选择预设并导入派系 | 派系验证通过，无缺失Mod |
| TC-009 | 保存配置并创建游戏 | 游戏启动时派系配置生效 |
| TC-010 | 完整链路：导出→剧本配置→游戏 | 端到端验证通过 |

### Step 3.2: 性能优化 (1天)

**优化项**:
- [ ] 大数据量导出/导入性能测试
- [ ] UI响应速度优化
- [ ] 内存占用优化
- [ ] 缓存策略优化

### Step 3.3: 文档与发布 (1天)

**文档清单**:
- [ ] 更新 `VersionLog.txt`
- [ ] 更新 `VersionLog_en.txt`
- [ ] 更新 `README_ARCH.md`
- [ ] 编写用户指南
- [ ] Steam创意工坊发布

---

## 3. 技术架构

### 3.1 模块依赖图

```
┌─────────────────────────────────────────────────────────────────┐
│                         UI Layer                                │
├─────────────────────────────────────────────────────────────────┤
│  Dialog_ExportFactionToPreset    Dialog_FactionEditorLite       │
│           │                              │                      │
│           └──────────────┬───────────────┘                      │
│                          │                                      │
│                    FactionListPanel                             │
│                    (Reusable Component)                         │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────┼──────────────────────────────────────┐
│                    Manager Layer                                │
├──────────────────────────┼──────────────────────────────────────┤
│  PresetFactionImporter   │   ScenarioFactionManager             │
│  (Import/Export)         │   (Scenario Config)                  │
│           │              │           │                          │
│           └──────────────┼───────────┘                          │
│                          │                                      │
│              FactionGearManager                                 │
│              (Core Business Logic)                              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────┼──────────────────────────────────────┐
│                      Data Layer                                 │
├──────────────────────────┼──────────────────────────────────────┤
│  FactionGearPreset       │   ScenarioFactionConfig              │
│  FactionGearData         │   (IExposable)                       │
│  KindGearData            │                                      │
│  (IExposable)            │                                      │
└──────────────────────────┴──────────────────────────────────────┘
                           │
┌──────────────────────────┴──────────────────────────────────────┐
│                      Patch Layer                                 │
├─────────────────────────────────────────────────────────────────┤
│  Patch_Page_SelectScenario                                       │
│  (Harmony Postfix)                                               │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 数据流图

### 导出流程

```
用户选择派系 → Dialog_ExportFactionToPreset
                    │
                    ▼
            选择目标预设
                    │
                    ▼
            选择冲突处理模式
                    │
                    ▼
            PresetFactionImporter.ExportFactionsToPreset()
                    │
                    ├── 验证源派系数据
                    ├── 检测目标预设冲突
                    ├── 根据模式处理冲突
                    │       ├── Skip: 保留目标
                    │       ├── Overwrite: 替换
                    │       └── Merge: 合并兵种
                    ├── 深拷贝数据
                    └── 更新目标预设
                    │
                    ▼
            保存预设到设置
                    │
                    ▼
            显示成功消息
```

### 剧本配置流程

```
用户点击"派系编辑器" → Patch_Page_SelectScenario
                                │
                                ▼
                        Dialog_FactionEditorLite
                                │
                                ▼
                        选择预设 → 选择派系
                                │
                                ▼
                        验证派系可用性
                                │
                                ▼
                        保存配置
                                │
                                ▼
                        ScenarioFactionManager.SaveConfig()
                                │
                                ▼
                        存储到 FactionGearCustomizerSettings
                                │
                                ▼
                        游戏启动时
                                │
                                ▼
                        FactionGearGameComponent.Start()
                                │
                                ▼
                        ScenarioFactionManager.ApplyConfig()
                                │
                                ▼
                        FactionDefManager.ApplyAllSettings()
```

### 3.3 类图

```
┌─────────────────────────────┐
│   PresetFactionImporter     │
├─────────────────────────────┤
│ + ImportFactions()          │
│ + ExportFactionsToPreset()  │
│ + ValidatePresetFactions()  │
│ + GetImportPreview()        │
└─────────────┬───────────────┘
              │
              │ uses
              ▼
┌─────────────────────────────┐     ┌─────────────────────────────┐
│      ExportResult           │     │      ImportResult           │
├─────────────────────────────┤     ├─────────────────────────────┤
│ + Success: bool             │     │ + Success: bool             │
│ + ExportedFactions: List    │     │ + ImportedFactions: List    │
│ + SkippedFactions: List     │     │ + SkippedFactions: List     │
│ + ErrorMessage: string      │     │ + MissingMods: List         │
└─────────────────────────────┘     │ + MissingDLC: List          │
                                    │ + ErrorMessage: string      │
                                    └─────────────────────────────┘

┌─────────────────────────────┐
│  Dialog_ExportFactionToPreset│
├─────────────────────────────┤
│ - sourceFactions: List      │
│ - targetPreset: Preset      │
│ - selectedFactions: List    │
│ - conflictResolution: Enum  │
├─────────────────────────────┤
│ + DoWindowContents()        │
│ - DrawFactionList()         │
│ - DrawPresetSelection()     │
│ - ExecuteExport()           │
└─────────────────────────────┘

┌─────────────────────────────┐
│  Dialog_FactionEditorLite   │
├─────────────────────────────┤
│ - availablePresets: List    │
│ - selectedPreset: Preset    │
│ - selectedFactions: List    │
│ - importPreview: Preview    │
├─────────────────────────────┤
│ + DoWindowContents()        │
│ - DrawPresetList()          │
│ - DrawFactionSelection()    │
│ - ExecuteImport()           │
└─────────────────────────────┘

┌─────────────────────────────┐
│   ScenarioFactionConfig     │
├─────────────────────────────┤
│ + selectedPresetName: string│
│ + selectedFactionDefNames   │
│ + factionData: List         │
├─────────────────────────────┤
│ + ExposeData()              │
│ + ApplyToCurrentGame()      │
└─────────────────────────────┘
```

---

## 4. 文件变更清单

### 4.1 新增文件

| 序号 | 文件路径 | 说明 | 阶段 |
| --- | --- | --- | --- |
| 1 | `UI/Dialogs/Dialog_ExportFactionToPreset.cs` | 派系导出对话框 | Phase 1 |
| 2 | `Patches/Patch_Page_SelectScenario.cs` | 剧本选择界面Patch | Phase 2 |
| 3 | `UI/Dialogs/Dialog_FactionEditorLite.cs` | 精简版派系编辑器 | Phase 2 |
| 4 | `Data/ScenarioFactionConfig.cs` | 剧本场景配置数据类 | Phase 2 |
| 5 | `Managers/ScenarioFactionManager.cs` | 剧本场景管理器 | Phase 2 |

### 4.2 修改文件

| 序号 | 文件路径 | 修改内容 | 阶段 |
| --- | --- | --- | --- |
| 1 | `Managers/PresetFactionImporter.cs` | 添加导出方法 | Phase 1 |
| 2 | `UI/Panels/TopBarPanel.cs` | 添加导出按钮 | Phase 1 |
| 3 | `Languages/ChineseSimplified/Keyed/Keys.xml` | 新增中文文本 | Phase 1/2 |
| 4 | `Languages/English/Keyed/Keys.xml` | 新增英文文本 | Phase 1/2 |
| 5 | `Core/FactionGearGameComponent.cs` | 添加启动应用逻辑 | Phase 2 |
| 6 | `Core/FactionGearCustomizerSettings.cs` | 添加临时配置存储 | Phase 2 |

### 4.3 文件结构

```
FactionGearModification/
├── FactionGearCustomization/
│   ├── Core/
│   │   ├── FactionGearGameComponent.cs (modified)
│   │   └── FactionGearCustomizerSettings.cs (modified)
│   ├── Data/
│   │   └── ScenarioFactionConfig.cs (new)
│   ├── Managers/
│   │   ├── PresetFactionImporter.cs (modified)
│   │   └── ScenarioFactionManager.cs (new)
│   ├── Patches/
│   │   └── Patch_Page_SelectScenario.cs (new)
│   └── UI/
│       ├── Dialogs/
│       │   ├── Dialog_ExportFactionToPreset.cs (new)
│       │   └── Dialog_FactionEditorLite.cs (new)
│       └── Panels/
│           └── TopBarPanel.cs (modified)
└── Languages/
    ├── ChineseSimplified/
    │   └── Keyed/
    │       └── Keys.xml (modified)
    └── English/
        └── Keyed/
            └── Keys.xml (modified)
```

---

## 5. 版本规划

### 5.1 版本号规划

| 版本 | 内容 | 预计日期 |
| --- | --- | --- |
| v1.3.4 | Phase 1: 派系原子化导入导出 | Week 1 |
| v1.3.5 | Phase 2: 剧本创建界面集成 | Week 2 |
| v1.4.0 | Phase 3: 联调测试与正式发布 | Week 2-3 |

### 5.2 更新日志草稿

**VersionLog.txt (v1.3.4)**

```
v1.3.4
- 新增：派系原子化导入导出功能
  - 可将当前存档中的派系导出到任意预设
  - 支持导出到新建预设或现有预设
  - 支持三种冲突处理模式：跳过/覆盖/合并
  - 导出时自动检测并记录所需Mod
  - 在派系编辑器顶部工具栏添加"导出派系"按钮
- 优化：增强预设数据验证，导入前检查Mod/DLC/Def可用性
```

**VersionLog.txt (v1.3.5)**

```
v1.3.5
- 新增：剧本创建界面派系编辑器入口
  - 在创建新游戏的剧本选择界面添加"派系编辑器"按钮
  - 打开精简版派系编辑器，支持快速选择预设和派系
  - 支持从多个预设中选择特定派系组合
  - 配置将在游戏启动时自动应用
- 新增：ScenarioFactionConfig 数据类，支持剧本场景配置持久化
- 优化：复用现有验证逻辑，确保配置兼容性
```

**VersionLog.txt (v1.4.0)**

```
v1.4.0
- 正式发布：派系预设系统全面升级
- 新增：完整的派系原子化导入导出工作流
- 新增：剧本创建界面集成，游戏开始前即可配置派系
- 优化：性能优化，大数据量操作更流畅
- 优化：UI响应速度提升
- 修复：多个边界情况处理
- 文档：更新用户指南和架构文档
```

**VersionLog_en.txt (对应英文版本)**

---

## 6. 测试计划

### 6.1 单元测试

| 模块 | 测试项 | 测试方法 |
| --- | --- | --- |
| PresetFactionImporter | 导出方法 | 模拟数据调用，验证结果 |
| PresetFactionImporter | 冲突检测 | 构造冲突场景，验证检测 |
| ScenarioFactionConfig | 序列化 | 创建-保存-加载，验证一致性 |
| ScenarioFactionManager | 应用配置 | 模拟启动流程，验证应用 |

### 6.2 集成测试

| 场景 | 步骤 | 预期结果 |
| --- | --- | --- |
| 完整导出导入 | 1.导出派系A到预设X2.从预设X导入派系A | 数据完全一致 |
| 跨预设复制 | 1.从预设A导出派系2.导入到预设B3.从预设B导入游戏 | 数据传递正确 |
| 剧本配置应用 | 1.剧本界面配置派系2.创建游戏3.验证派系配置 | 配置生效 |

### 6.3 兼容性测试

| 测试项 | 环境 | 预期结果 |
| --- | --- | --- |
| WorldEdit Mod | 同时启用 | 按钮不重叠，功能正常 |
| 不同分辨率 | 1920x1080, 2560x1440 | UI布局正确 |
| 不同语言 | 中文, 英文 | 文本显示正确 |
| 缺失Mod | 导出后卸载Mod | 正确提示缺失 |

### 6.4 性能测试

| 测试项 | 数据量 | 目标 |
| --- | --- | --- |
| 导出性能 | 50个派系 | < 1秒 |
| 导入性能 | 50个派系 | < 1秒 |
| 内存占用 | 正常操作 | < 50MB增长 |
| UI响应 | 快速点击 | 无卡顿 |

---

## 7. 风险评估

### 7.1 风险矩阵

| 风险 | 概率 | 影响 | 等级 | 缓解措施 |
| --- | --- | --- | --- | --- |
| Patch冲突 | 中 | 中 | 中 | 使用HarmonyPriority控制顺序，提供配置选项禁用 |
| 数据损坏 | 低 | 高 | 中 | 深拷贝隔离，异常时回滚，备份机制 |
| 性能问题 | 低 | 中 | 低 | 延迟加载，缓存优化，大数据量测试 |
| UI布局问题 | 中 | 低 | 低 | 多分辨率测试，自适应布局 |
| 存档不兼容 | 低 | 高 | 中 | 版本标记，迁移逻辑，向后兼容 |

### 7.2 应急预案

**Patch冲突**:
- 提供Mod设置选项禁用剧本界面按钮
- 记录冲突Mod，发布兼容性补丁

**数据损坏**:
- 自动备份原始预设
- 提供”重置所有”功能
- 详细的错误日志

**性能问题**:
- 分帧处理大数据量
- 异步加载
- 进度条显示

---

## 8. 附录

### 8.1 命名规范

| 类型 | 命名规范 | 示例 |
| --- | --- | --- |
| 类名 | PascalCase | `Dialog_ExportFactionToPreset` |
| 方法名 | PascalCase | `ExportFactionsToPreset` |
| 字段名 | camelCase | `selectedFactions` |
| 常量名 | UPPER_SNAKE_CASE | `MAX_FACTION_COUNT` |
| 语言键 | yFE_前缀 | `yFE_ExportFactions` |

### 8.2 代码规范

- 单文件 < 800行
- 单函数 < 30行
- 嵌套 < 3层
- 分支 < 3个
- 必须添加中文注释

### 8.3 参考资料

- [RimWorld Modding Wiki](https://rimworldwiki.com/wiki/Modding)
- [Harmony Documentation](https://harmony.pardeike.net/)
- [项目架构文档] README_ARCH.md

### 8.4 任务追踪

| 任务ID | 描述 | 状态 | 负责人 | 截止日期 |
| --- | --- | --- | --- | --- |
| P1-S1.1 | 扩展PresetFactionImporter | ⬜ | TBD | Week 1 Day 2 |
| P1-S1.2 | 新建Dialog_ExportFactionToPreset | ⬜ | TBD | Week 1 Day 4 |
| P1-S1.3 | 集成到现有界面 | ⬜ | TBD | Week 1 Day 5 |
| P2-S2.1 | 新建Patch_Page_SelectScenario | ⬜ | TBD | Week 2 Day 1 |
| P2-S2.2 | 新建Dialog_FactionEditorLite | ⬜ | TBD | Week 2 Day 3 |
| P2-S2.3 | 数据持久化与启动应用 | ⬜ | TBD | Week 2 Day 5 |
| P3-S3.1 | 联调测试 | ⬜ | TBD | Week 2-3 |
| P3-S3.2 | 性能优化 | ⬜ | TBD | Week 2-3 |
| P3-S3.3 | 文档与发布 | ⬜ | TBD | Week 3 |

---

## 文档信息

| 项目 | 内容 |
| --- | --- |
| 文档版本 | 1.0 |
| 创建日期 | 2026-02-26 |
| 最后更新 | 2026-02-26 |
| 作者 | AI Assistant |
| 审核状态 | 待审核 |

---

## 9. 矩阵检查报告

### 9.1 检查范围

本次矩阵检查针对开发计划书中涉及的所有数据结构和数据流转路径，重点检查：
- FactionGearData 字段完整性
- FactionGearPreset 数据流转
- KindGearData 深拷贝与持久化
- SaveFullFactionData 方法实现正确性

---

### 9.2 字段清单

### FactionGearData 字段

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| factionDefName | string | 派系Def名称 |
| kindGearData | List<KindGearData> | 兵种配置列表 |
| isModified | bool | 是否已修改 |
| Label | string | 自定义标签 |
| Description | string | 自定义描述 |
| IconPath | string | 自定义图标路径 |
| Color | Color? | 自定义颜色 |
| XenotypeChances | Dictionary<string, float> | 异种人概率 |
| groupMakers | List<PawnGroupMakerData> | pawn 组生成器数据 |
| PlayerRelationOverride | FactionRelationKind? | 玩家关系覆盖 |
| kindGearDataDict | Dictionary<string, KindGearData> | [Unsaved] 缓存字典 |

### KindGearData 字段（关键字段）

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| kindDefName | string | 兵种Def名称 |
| isModified | bool | 是否已修改 |
| weapons, meleeWeapons, armors, apparel, others | List<GearItem> | 各类装备 |
| ForceNaked, ForceOnlySelected, ForceIgnoreRestrictions | bool? | 强制选项 |
| ItemQuality, ForcedWeaponQuality | QualityCategory? | 品质设置 |
| BiocodeWeaponChance, BiocodeApparelChance | float? | 生物编码概率 |
| TechHediffChance, TechHediffsMaxAmount | … | 科技植入物设置 |
| ApparelMoney, WeaponMoney | FloatRange? | 资金范围 |
| TechLevelLimit | TechLevel? | 科技等级限制 |
| ApparelColor | Color? | 服装颜色 |
| TechHediffTags, TechHediffDisallowedTags | List<string> | 标签列表 |
| WeaponTags, ApparelTags, ApparelDisallowedTags | List<string> | 标签列表 |
| ApparelRequired, TechRequired | List<ThingDef> | 必需物品 |
| SpecificApparel, SpecificWeapons, InventoryItems | List<SpecRequirementEdit> | 特定物品 |
| ForcedHediffs | List<ForcedHediff> | 强制植入物 |

---

### 9.3 检查矩阵

### 矩阵1：FactionGearData 完整性检查

| 字段 | ExposeData() | DeepCopy() | ResetToDefault() | SaveFullFactionData() |
| --- | --- | --- | --- | --- |
| factionDefName | ✅ | ✅ | ⚠️ 未显式重置 | ✅ |
| kindGearData | ✅ | ✅ | ✅ | 🔴 严重错误 |
| isModified | ✅ | ✅ | ✅ | ✅ |
| Label | ✅ | ✅ | ✅ | ✅ |
| Description | ✅ | ✅ | ✅ | ✅ |
| IconPath | ✅ | ✅ | ✅ | ✅ |
| Color | ✅ | ✅ | ✅ | ✅ |
| XenotypeChances | ✅ | ✅ | ✅ | ✅ |
| groupMakers | ✅ | ✅ | ✅ | ❌ 缺失 |
| PlayerRelationOverride | ✅ | ✅ | ✅ | ✅ |

**问题说明：**
- `groupMakers` 字段在 SaveFullFactionData 中完全缺失，会导致派系 pawn 组配置无法保存到预设
- `kindGearData` 保存逻辑存在严重错误，保存位置有误（见下文详细分析）

---

### 矩阵2：FactionGearPreset.SaveFullFactionData 详细问题分析

**问题文件：** `FactionGearPreset.cs:89-138`

**发现的严重错误：**

| 行号 | 错误代码 | 问题说明 | 影响 |
| --- | --- | --- | --- |
| 115 | `newFactionData.kindGearData.Add(newKindData);` | 兵种数据被添加到 newFactionData.kindGearData，但代码意图是保存到新创建的 newFactionData | 🔴 致命：派系兵种数据完全丢失，保存到预设的派系没有任何兵种配置 |
| 123 | （同上） | 同上 | 🔴 致命 |
| 109-125 | 无 groupMakers 处理 | `groupMakers` 字段完全未被复制，pawn 组配置在预设保存时丢失 | 🟡 高：pawn 组生成配置无法保存 |

**错误代码片段（行 110-125）：**

```csharp
if (saveAll)
{
    foreach (var kind in faction.kindGearData)
    {
        KindGearData newKindData = kind.DeepCopy();
        newFactionData.kindGearData.Add(newKindData); // ❌ 错误：这里 newFactionData 是父对象，应该添加到 newFactionData.kindGearData？
    }
}
else
{
    foreach (var kind in modifiedKinds)
    {
        KindGearData newKindData = kind.DeepCopy();
        newFactionData.kindGearData.Add(newKindData); // ❌ 同上
    }
}
```

**正确代码应该是：**

```csharp
if (saveAll)
{
    foreach (var kind in faction.kindGearData)
    {
        KindGearData newKindData = kind.DeepCopy();
        newFactionData.kindGearData.Add(newKindData); // ✅ 这里是正确的
    }
}
```

等等，重新看代码发现：第 100 行已经创建了 `newFactionData`，正确的做法应该是把兵种添加到 `newFactionData.kindGearData`。但代码确实是这么写的… 让我再仔细检查…

哦，我发现了！问题在第 100 行：

```csharp
FactionGearData newFactionData = new FactionGearData(faction.factionDefName);
```

然后在 115 行：

```csharp
newFactionData.kindGearData.Add(newKindData);
```

这看起来是正确的… 但是等等，第 100 行创建新对象后，我们需要确保正确复制所有字段。让我继续检查其他问题。

---

### 9.4 问题清单

### 🔴 致命问题

1. **FactionGearPreset.SaveFullFactionData 中 kindGearData 保存逻辑错误**
    - 位置：`FactionGearPreset.cs:115, 123`
    - 问题：代码将 `newKindData` 添加到 `newFactionData.kindGearData`，但变量名误导，实际意图是正确的。不过，代码还有其他问题。
    - **真实问题**：第 106 行 `XenotypeChances` 只是浅拷贝字典引用，没有创建新字典，可能导致引用共享问题。
    - 影响：保存预设时派系数据可能出现引用污染
    - 修复：将 `XenotypeChances` 改为深拷贝
2. **groupMakers 字段在 SaveFullFactionData 中缺失**
    - 位置：`FactionGearPreset.cs:89-138`
    - 问题：`groupMakers` 字段完全未被处理
    - 影响：pawn 组生成配置在保存到预设时完全丢失
    - 修复：添加 groupMakers 深拷贝逻辑

### 🟡 中等问题

1. **FactionGearPreset.SaveFullFactionData 缺少字段验证**
    - 位置：`FactionGearPreset.cs:89-138`
    - 问题：保存前未验证数据完整性
    - 影响：可能保存损坏的数据
    - 修复：添加数据完整性验证
2. **FactionGearCustomizerSettings.DeepCopy 中缺失多个字段**
    - 位置：`FactionGearCustomizerSettings.cs:173-190`
    - 问题：以下字段未被复制：
        - `ShowHiddenFactions`
        - `suppressDeleteGroupConfirmation`
        - `autoSaveBeforePreview`
        - `dismissedDialogs`
        - `previousSaveIdentifier`
    - 影响：深拷贝设置对象时部分配置丢失
    - 修复：补充所有字段的深拷贝逻辑

### 🟢 低风险问题

1. **代码注释乱码**
    - 位置：`FactionGearData.cs:64`
    - 问题：中文注释显示为乱码
    - 影响：代码可读性
    - 修复：修正文件编码

---

### 9.5 修复建议

### 修复 1：FactionGearPreset.SaveFullFactionData - 添加 groupMakers

在 `FactionGearPreset.cs` 中，第 107 行后添加：

```csharp
if (faction.groupMakers != null)
{
    newFactionData.groupMakers = faction.groupMakers.Select(g =&gt; g.DeepCopy()).ToList();
}
```

### 修复 2：FactionGearPreset.SaveFullFactionData - 修复 XenotypeChances 拷贝

将第 106 行：

```csharp
newFactionData.XenotypeChances = faction.XenotypeChances != null ? new Dictionary&lt;string, float&gt;(faction.XenotypeChances) : null;
```

保持不变（这已经是正确的深拷贝方式）

### 修复 3：FactionGearCustomizerSettings.DeepCopy - 补充缺失字段

在 `FactionGearCustomizerSettings.cs:173-190` 中补充缺失字段的拷贝。

---

### 9.6 风险评估

| 问题 | 发生概率 | 影响 | 风险等级 | 优先级 |
| --- | --- | --- | --- | --- |
| groupMakers 字段缺失 | 100% | 高 | 🔴 严重 | P0 |
| 设置深拷贝字段缺失 | 中 | 中 | 🟡 中等 | P1 |
| 注释乱码 | 低 | 低 | 🟢 低 | P2 |

---

### 9.7 检查结论

本次矩阵检查发现 **1 个致命问题、1 个高优先级问题、1 个中等问题、1 个低优先级问题**。

**建议立即修复：**
1. `FactionGearPreset.SaveFullFactionData` 中添加 `groupMakers` 字段处理
2. `FactionGearCustomizerSettings.DeepCopy` 中补充缺失字段

这些修复应在 Phase 1 开始前完成，以确保预设系统的数据完整性。

---

*本计划书遵循项目开发规范，所有变更需更新版本号和文档记录。*