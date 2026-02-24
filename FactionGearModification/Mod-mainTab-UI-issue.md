# MainTabWindow UI 问题分析与修复记录

## 问题描述

打开"派系装备修改器"主界面时，窗口顶部会出现一条顽固的、高度约 35px 的留白区域，且显示了 "Faction Gear Customizer"（或中文 "派系装备修改器"）的标题文本。这导致了 UI 顶部空间的浪费，且无法通过常规的 XML 配置或简单的代码修改移除。

## 根本原因分析 (2026-02-23 最终定论)

经过漫长的排查，发现问题的根源并非 `FactionGearMainTabWindow` 本身的实现问题，而是一个**目标判定过于宽泛的 Harmony Patch** 导致的。

### 1. 罪魁祸首：`Patch_ModSettingsTitleIcon.cs`

项目中存在一个名为 `Patch_ModSettingsTitleIcon` 的补丁，其原本目的是为了给 **Mod 设置选项卡**（`Dialog_ModSettings`）添加图标和标题。

然而，该补丁的 `IsTarget` 方法在判断目标窗口时存在逻辑漏洞：
- 它虽然尝试排除了 `FactionGearMainTabWindow`。
- 但在某些情况下（可能是基类匹配或类型判断顺序问题），它**错误地将主界面窗口识别为了目标窗口**。
- 结果：该补丁的 `Postfix` 方法被执行，强制在窗口顶部绘制了标题栏和图标，覆盖了我们所有的自定义绘制逻辑。

### 2. 次要原因：`MainTabWindow` 的默认行为

RimWorld 的 `MainTabWindow`（继承自 `Window`）在初始化时，如果 `optionalTitle` 属性不为空，或者对应的 `MainButtonDef` 有 `label`，系统底层的 `Window.WindowOnGUI` 可能会预留标题栏空间。

## 最终修复方案 (v1.1.43)

为了彻底解决此问题，我们采取了“釜底抽薪”与“严防死守”相结合的策略。

### 1. 移除问题 Patch（核心修复）

**操作**：直接删除了 `Patches/Patch_ModSettingsTitleIcon.cs` 文件。
**理由**：该补丁功能非核心（仅美化设置界面），但副作用巨大。移除它消除了强制绘制标题的外部干扰源。

### 2. 强制接管 WindowOnGUI（防止留白）

在 `UI/FactionGearMainTabWindow.cs` 中，我们重写了 `WindowOnGUI` 方法，并强制设置绘制区域坐标。

```csharp
public override void WindowOnGUI()
{
    // 1. 强制将 rect 设置为从 (0,0) 开始，彻底消除任何顶部偏移
    //    UI.screenWidth 和 UI.screenHeight 确保覆盖全屏（减去底部标签栏）
    Rect rect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight - 35f);
    this.windowRect = rect; // 同步 rect 以确保事件响应正确
    
    // 2. 绘制背景
    if (this.doWindowBackground)
    {
        Widgets.DrawWindowBackground(rect);
    }
    
    // 3. 使用 GUI.BeginGroup 接管绘制，绕过原生 Window 的标题栏处理
    GUI.BeginGroup(rect);
    try
    {
        Rect localRect = new Rect(0f, 0f, rect.width, rect.height);
        this.DoWindowContents(localRect);
    }
    finally
    {
        GUI.EndGroup();
    }
}
```

### 3. 强制清空 optionalTitle（双重保险）

为了防止 RimWorld 内部机制在某些角落里仍尝试绘制标题或预留空间，我们在窗口打开前强制清空标题属性。

```csharp
public override void PreOpen()
{
    base.PreOpen();
    this.optionalTitle = ""; // 核心：告诉系统“我没有标题”，禁止绘制原生标题栏
    this.doWindowBackground = true;
    FactionGearEditor.InitializeWorkingSettings(true);
}
```

## 经验总结

1.  **Harmony Patch 需谨慎**：在对基类（如 `Window`）进行 Patch 时，目标过滤逻辑必须极其严密。一旦误伤其他窗口，排查难度极大，因为问题表现得像是窗口自身的行为。
2.  **UI 调试方向**：当 UI 出现“无法解释”的元素时，不要只盯着当前的 UI 类看，要检查是否有全局的 Patch 或 Hook 在干扰绘制。
3.  **MainTabWindow 特性**：RimWorld 的主标签页窗口有其特殊的生命周期和属性（如 `MainButtonDef` 的关联），修改其默认外观通常需要重写 `WindowOnGUI` 才能彻底摆脱原版限制。
