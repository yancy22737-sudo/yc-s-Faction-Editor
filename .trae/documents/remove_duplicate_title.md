# 移除重复标题计划

用户反馈界面上 "Faction Gear Customizer" 标题重复，希望移除左上角的标题。
经分析，截图展示的是 Mod 设置界面（`Dialog_ModSettings`），左上角的标题是 RimWorld 系统强制显示的窗口标题。中间带图标的标题是 Mod 自定义绘制的。

为了解决重复问题并保持界面整洁，我们计划：

1.  **移除中间的自定义标题文本**：
    *   修改 `FactionGearCustomizerMod.cs` 中的 `DoSettingsWindowContents` 方法。
    *   移除绘制 "Faction Gear Customizer" 文本的代码。
    *   保留 Logo 绘制（可选，或者也移除，视布局而定），并调整布局使其居中。

2.  **尝试隐藏主窗口标题（如果存在）**：
    *   虽然截图看起来是 Mod 设置界面，但为了保险起见，我们在 `FactionGearMainTabWindow.cs` 中显式设置 `optionalTitle` 为空，以防在主窗口模式下也出现标题。

这样可以消除文本重复，同时保留系统标准的窗口标题。

## 涉及文件

*   `FactionGearCustomizerMod.cs`: 移除中间标题文本绘制。
*   `FactionGearMainTabWindow.cs`: 确保主窗口没有标题（防御性编程）。

## 验证

*   编译并运行游戏。
*   打开 Faction Gear Customizer 的 Mod 设置界面。
*   确认界面上不再出现重复的 "Faction Gear Customizer" 文本，只保留左上角的系统标题（和中间的 Logo）。
