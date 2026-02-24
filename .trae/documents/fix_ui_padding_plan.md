# 修复主界面上方留白与标题残留问题计划

用户反馈主界面上方存在约两行高度的留白，且“派系装备修改器”标题依然显示。这表明虽然我们在 `FactionGearMainTabWindow` 中试图接管绘制，但仍有代码在绘制标题，或者我们的接管未能完全覆盖原版逻辑。

考虑到用户提到“项目刚搭建时没有这个问题”，我们将重点排查后期的 UI 重构（如 `TopBarPanel` 的引入）以及可能被忽视的绘制调用。

## 1. 现状分析
- **现象**: 顶部有空白 + 标题依然显示。
- **矛盾点**: `FactionGearMainTabWindow.WindowOnGUI` 已重写且未调用 `base.WindowOnGUI`，且 `TopBarPanel` 中的标题绘制代码已被注释。理论上不应出现标题。
- **推测原因**:
    1.  **残留的绘制代码**: 项目中可能存在另一个地方（非 `TopBarPanel`）在绘制窗口标题。
    2.  **XML 配置问题**: `MainButtonDef` 可能指向了旧的或错误的 Window 类。
    3.  **Patch 冲突**: 可能有 Harmony Patch 强制恢复了标题绘制。
    4.  **类继承问题**: `MainTabWindow` 的某些底层机制被触发。

## 2. 排查步骤

### 2.1 全局搜索标题文本引用
搜索翻译键 `FactionGearCustomizer` (对应“派系装备修改器”) 在所有 `.cs` 文件中的引用，找出是谁在绘制它。
- **重点关注**: `FactionGearEditor.cs`, `FactionGearMainTabWindow.cs` 以及所有 `UI/Panels/*.cs`。

### 2.2 检查 XML 定义
确认 `Defs/MainButtonDefs/MainButton.xml` 中 `<tabWindowClass>` 指向的确实是我们修改的 `FactionGearCustomizer.UI.FactionGearMainTabWindow`。

### 2.3 检查 Harmony Patch
审查 `Patches/` 目录下的所有补丁，特别是 `Patch_FactionGearMainTabWindow.cs`，确认是否有补丁干扰了 `WindowOnGUI` 的执行流程。

### 2.4 审查 UI 布局计算
一旦找到标题绘制源头并移除后，重新检查 `FactionGearEditor.DrawEditor` 中的 `Rect` 计算，确保 `(0,0)` 起点确实对应屏幕顶部，没有被父级 Group 偏移。

## 3. 修复方案
根据排查结果执行修复：
- **如果是代码残留**: 直接删除或注释相关的 `Widgets.Label` 调用。
- **如果是机制问题**: 在 `FactionGearMainTabWindow` 中进一步加固，例如在 `DoWindowContents` 开头显式绘制一个覆盖全屏的 Debug 框（开发阶段）或调整 `GUI.BeginGroup` 的坐标。

## 4. 验证
- 编译并运行。
- 确认顶部标题消失。
- 确认 UI 内容紧贴顶部（或保留适当美观的 Padding，但由我们控制）。
