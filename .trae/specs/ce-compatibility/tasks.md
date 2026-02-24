# Tasks

- [ ] Task 1: 实现 CE 模组检测逻辑
  - [ ] SubTask 1.1: 创建 `CECompatibility` 静态类，提供 `IsActive` 属性或方法，通过检查 Mod 列表或特定类型是否存在来判断 CE 是否加载。
- [ ] Task 2: 创建 CE 兼容性模块架构
  - [ ] SubTask 2.1: 定义 `ICECompatibilityModule` 接口或基类（可选，视复杂度而定），或直接创建 `CEPatch` 类。
  - [ ] SubTask 2.2: 实现具体的 CE 兼容逻辑（如针对 CE 的属性调整、方法替换等）。目前主要是框架搭建，具体逻辑需根据实际冲突点填充。
- [ ] Task 3: 集成 CE 兼容性初始化
  - [ ] SubTask 3.1: 在模组的主初始化入口（`StaticConstructorOnStartup` 或 `Mod` 类）中调用检测逻辑。
  - [ ] SubTask 3.2: 如果检测到 CE，则执行 CE 兼容模块的初始化/Patch 方法。
- [ ] Task 4: 验证与测试
  - [ ] SubTask 4.1: 在启用 CE 的环境下启动游戏，确认无红字报错。
  - [ ] SubTask 4.2: 在未启用 CE 的环境下启动游戏，确认功能正常且无 CE 相关报错。

# Task Dependencies
- Task 3 depends on Task 1 and Task 2.
- Task 4 depends on Task 3.
