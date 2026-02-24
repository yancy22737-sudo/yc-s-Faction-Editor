---
name: rim-searcher
description: "RimWorld 源码检索与分析助手。用于查询 RimWorld C# 类型、XML Def、继承关系、交叉引用。涉及边缘世界底层机制时/问题难以解决时优先调用。"
---

# RimSearcher - RimWorld 源码检索助手

本 Skill 指导 AI 使用 RimSearcher MCP 工具进行 RimWorld 源码的精准检索与分析。

## 何时调用

- 需要理解 RimWorld 原生机制、类继承关系
- 查询特定 Def 的定义或关联 C# 类型
- 追踪某个类/方法的引用位置
- 阅读 RimWorld 源代码片段

## 六大工具速查

### 1. 🔎 rimworld-searcher__locate - 全局模糊定位
支持 C# 类型、成员、XML Def、文件名搜索。

**过滤语法：**
- `type:CompShield` - 查找类型
- `method:CompTick` - 查找方法
- `field:energy` - 查找字段
- `def:Apparel_ShieldBelt` - 查找 Def

**示例：**
```
def:Apparel_ShieldBelt
type:CompShield
JDW (CamelCase 缩写匹配 JobDriver_Wait)
```

### 2. 🔬 rimworld-searcher__inspect - 深度分析
分析单个 Def 或 C# 类型，返回继承关系图和成员大纲。

**示例：**
```
name: Apparel_ShieldBelt
name: RimWorld.CompShield
```

### 3. 🔗 rimworld-searcher__trace - 交叉引用追踪
- `mode: inheritors` - 列出子类
- `mode: usages` - 查找引用位置

**示例：**
```
symbol: ThingComp, mode: inheritors
symbol: CompShield, mode: usages
```

### 4. 📖 rimworld-searcher__read_code - 精确读取代码
读取指定方法、属性、类或行区间。

**示例：**
```
path: CompShield.cs, methodName: CompTick
path: CompShield.cs, extractClass: CompShield
path: CompShield.cs, startLine: 50, lineCount: 30
```

### 5. 🔤 rimworld-searcher__search_regex - 正则检索
全局正则搜索 C# 和 XML。

**示例：**
```
pattern: class.*:.*ThingComp
fileFilter: .cs
```

### 6. 📁 rimworld-searcher__list_directory - 目录浏览
列出目录内容，支持分页。

**示例：**
```
path: /RimWorld/Source/Core/Defs
limit: 50
```

## 使用流程

1. **定位** → 使用 `locate` 找到目标
2. **分析** → 使用 `inspect` 了解结构
3. **追踪** → 使用 `trace` 查看引用/继承
4. **阅读** → 使用 `read_code` 查看具体实现

## 典型工作流
- 场景：分析护盾腰带如何生效
locate(def:Apparel_ShieldBelt)：定位 Def
inspect(Apparel_ShieldBelt)：看合并后 XML 与关联 C# 类型
inspect(RimWorld.CompShield)：看继承链和类大纲
read_code(path=CompShield.cs, methodName=CompTick)：读取核心逻辑
trace(symbol=CompShield, mode=usages)：追踪相关引用

## 注意事项

- 优先使用 `locate` 而非 `search_regex`，性能更好
- CamelCase 缩写支持（如 JDW → JobDriver_Wait）
- Def 查询会自动解析 ParentName 继承链
- C# 与 XML 之间有语义桥接（thingClass/compClass 自动关联）