### 核心开发规范
1. **代码架构**
   - 模块化拆分UI为独立组件，分离UI表现与业务逻辑，降低单文件复杂度。
   - **禁止“上帝类”**：单文件超150KB、职责不清（混合UI/业务/数据/状态）、高耦合、大量静态字段存UI状态均不允许。
   - **硬阈值**：单文件<800行，单函数<30行，嵌套<3层，分支<3个。

2. **工具与参考**
   - 涉及边缘世界底层机制/原生代码，优先用rim-search（精准理解Def和Class关系）。
   - 以下文件夹内容仅作参考，不属于本项目。
   - Ref-TotalControl
   - CombatExtended-1.6.7.1.0
   - rimworld-worldedit-2.0-master
   - RimTalk-main

3. **文档维护**
   - **README_ARCH.md**：检索优先权重，需清晰描述项目模块划分，生成全局地图、各模块成员清单与接口说明，每个文件头部声明依赖和职责；每次代码变更后强制回环检查文件头依 赖、更新模块文档。
   - **VersionLog.txt**：检索优先权重，每次版本更新时添加更新内容。

4. **版本与适配**
   - 版本号格式为x.y.z（主/次/修订），每次build前必须升级。禁止删减旧版本号和内容。
   - 新功能涉及UI显示需做语言适配。
   - 修改已有Def必须用PatchOperation。
   - Harmony Patch 需谨慎：在对基类（如 `Window`）进行 Patch 时，目标过滤逻辑必须极其严密。

5. **文件操作**
   - 直接删除→根目录新建TrashCan文件夹，移入并改后缀为_original。
   - 直接覆盖→根目录新建Legacy文件夹，移入并改后缀为_legacy。

6. **测试与环境**
   - 项目实际运行测试地址：E:\SteamLibrary\steamapps\common\RimWorld\Mods\FactionGearCustomizer
   - build流程为运行build.ps1,修复报错,然后运行deploy_test.ps1
   - Mods\FactionGearCustomizer\Language是唯一正确语言文件路径
   - 项目环境为Rimworld 1.6，注意Harmony Patch相关问题。

7. **开发决策与限制**
   - 模糊/难实现的开发方向，优先向用户提问。
   - 难修复的bug，禁止阉割功能，优先向用户提问。
   - **禁止修改游戏本体**：包括直接修改源文件、资源文件、配置文件。
   - 没有build指令禁止build。
   - 存档安全,mod兼容性高,修改无需重启游戏，不会导致游戏崩溃或数据丢失是底线。
   - xml语言文件注意转义字符问题!
