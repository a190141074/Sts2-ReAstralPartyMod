---
name: sts2-ritsulib-modding
description: Build or modify Slay the Spire 2 mods that use RitsuLib as a required dependency. Use when Codex needs a Chinese-first workflow for RitsuLib project setup, `ModInitializer` entrypoints, `RegisterCard` or `RegisterRelic` auto-registration, `ModCardTemplate` or `ModRelicTemplate` content authoring, BaseLib-to-RitsuLib migration, or debugging why a RitsuLib-based mod is not loading or registering content.
---

# STS2 RitsuLib Modding

## Overview

把这个 skill 当成“RitsuLib 前置 STS2 Mod 开发工作流”。它不替代 BaseLib skill；当仓库已经依赖 `STS2-RitsuLib`，或任务明确要求 `RegisterCard`、`ModTypeDiscoveryHub.RegisterModAssembly`、`ModCardTemplate`、`ModRelicTemplate`、`RitsuLibFramework` 时，优先使用这个 skill。

这个 skill 只负责 RitsuLib 共性工作流，不承载单一仓库的日志路径、命名链约束或 feature-family 经验。若当前仓库还有自己的 agent 文档或 repo overlay skill，先读这个通用层，再继续读仓库层。

## Updated Reference Roots

- `D:\MOD\杀戮尖塔2mod制作\RitsuLib-doc\RitsuLib`
  - 新版教程主入口，优先看这里的 `01 - 添加基础内容`、`02 - 玩法基底`、`03 - 模组工具`。
- `D:\MOD\杀戮尖塔2mod制作\RitsuLib-code`
  - 当前 RitsuLib 代码与 lifecycle patch 权威。
- `D:\Steam\steamapps\workshop\content\2868840\3747602295\lib\0.107.1`
  - 当前本机运行时 RitsuLib 实际程序集目录。
  - Workshop 根目录的 `STS2-RitsuLib.dll` 是 loader；日常编译、API 查询和签名核对优先用这里的 `STS2-RitsuLib.dll` / `STS2-RitsuLib.xml`。
- `D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64`
  - 当前本机运行时游戏程序集目录。
  - 其中 `sts2.xml` 是游戏本体程序集 XML 文档索引；新版已为不少原生类型和方法补了注释，排查原生 Hook、参数名和 public API 时优先一起看。
- `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode`
  - 游戏内测试、作弊、脚本执行与 mod 调试工具箱。
- `D:\MOD\杀戮尖塔2mod制作\Slay-the-Spire-2-gdsdecomp`
  - 当前反编译源码主入口，优先查该目录下 `src\Core` 的具体系统源码。

默认按照这条优先级取证并落地方案：

1. `RitsuLib-doc`
2. 运行时 `STS2-RitsuLib.xml`
3. `RitsuLib-code`
4. 当前工作仓库
5. `STS2-DevMode`
6. `STS2_WineFox-main`
7. 游戏反编译代码

## Quick Start

1. 先判断任务类型：项目初始化、卡牌、遗物、能力、药水、事件、角色、时间线/附魔、迁移、排错。
2. 打开 [references/workflows.md](references/workflows.md)，选对应工作流。
3. 如果问题是“回合开始该挂哪一种 / 哪个 hook 更窄更安全 / 是模型 override 还是 lifecycle event”，先读 [references/timing-map.md](references/timing-map.md)。
4. 打开 [references/source-map.md](references/source-map.md)，确认本机可用的权威路径。
5. 如果任务涉及 BaseLib 写法迁移，先看 [references/migration-map.md](references/migration-map.md)。
6. 如果任务需要项目组织参考或目录落点，先看 [references/project-patterns.md](references/project-patterns.md)。
7. 如果当前仓库还有自己的 agent 文档或 overlay skill，继续加载仓库层。
8. 需要找真实符号、特性、注册器、示例时，运行 `scripts/find-ritsulib-symbol.ps1`，不要手写长搜索命令。

## Working Rules

- 优先使用 RitsuLib 已公开的注册/模板/内容包能力，不要先上 Harmony。
- 只有在 RitsuLib 没覆盖目标行为，或者任务显式要求改原生流程时，才看 Harmony 或反编译代码。
- 先确认“入口是否注册正确”，再写内容代码。很多“卡牌没出现”“遗物没加载”本质上是初始化或依赖问题。
- 先做最小可运行切片：入口、依赖、1 个内容类型、本地化、图标或场景路径，然后再扩展复杂联动。
- 当概念已明确，但 public 接口名、override 面、参数名、返回值语义还不确定时，先查运行时 `STS2-RitsuLib.xml`，再决定是否下钻 `RitsuLib-code`。
- 当前游戏 API `0.105+` 分支默认把 manifest 当成新版口径处理：`min_game_version` 必填；依赖优先写对象形态并补 `min_version`，不要继续沿用旧字符串依赖数组。
- 对 Harmony / patcher 直连原生方法时，不要只按旧代码记忆写 patch 形参；参数名也要对照运行时 `sts2.xml` 或反编译确认，例如 `choiceContext` 不能随手写成 `context`。
- 遇到 hook/时机选择问题，不要凭感觉选最宽的入口；先看 `timing-map.md`，再决定是模型 override、RitsuLib lifecycle event，还是更窄的仓库 base/helper。
- `STS2_WineFox-main` 是高价值 RitsuLib 实战案例，但不要无脑照抄。先抽取结构模式，再贴合当前仓库实现。
- 如果仓库已经有自己的 working example、agent 文档、技能 overlay 或命名/资源约定，优先在仓库内找同类参照，不要强推通用模板。

## Task Routing

### 项目初始化与依赖

处理这些请求时优先看：

- `RitsuLib-doc\README.md`
- `RitsuLib-code\Docs\zh\GettingStarted.md`
- 当前项目的 `*.csproj`
- 当前项目的 manifest / mod json
- 当前项目的入口文件

重点检查：

- `STS2-RitsuLib` DLL 或 NuGet 引用是否存在
- 运行时 `STS2-RitsuLib.dll` 与 `STS2-RitsuLib.xml` 是否存在且版本匹配
- 运行时 `sts2.dll` 与 `sts2.xml` 是否存在，且和当前参考的 API 分支一致
- mod manifest 是否声明 `min_game_version`
- mod manifest `dependencies` 是否包含 `STS2-RitsuLib`，以及当前分支是否应改为对象写法并补 `min_version`
- 入口是否包含 `RitsuLibFramework.EnsureGodotScriptsRegistered(...)`
- 入口是否包含 `ModTypeDiscoveryHub.RegisterModAssembly(modId, assembly)`

### 添加卡牌

优先看：

- `RitsuLib-doc\01 - 添加卡牌\README.md`
- `RitsuLib-code\Interop\AutoRegistration\RegistrationAttributes.cs`
- `RitsuLib-code\Scaffolding\Content\ModCardTemplate.cs`
- `STS2_WineFox-main\Cards\`

默认流程：

1. 先确认卡池类型和基类。
2. 用 `[RegisterCard(typeof(...Pool))]` 而不是 BaseLib 的 `[Pool(...)]`。
3. 如果接口名、virtual 面、构造参数或 helper 返回值不清楚，先查 `STS2-RitsuLib.xml`。
4. 用 `ModCardTemplate` 或项目自定义卡牌基类，而不是 `CustomCardModel`。
5. 补齐图片、本地化、必要的 starter 或 token 约定。

### 添加遗物

优先看：

- `RitsuLib-doc\03 - 添加新遗物\README.md`
- `RitsuLib-code` 中 `RegisterRelic` 和 `ModRelicTemplate`
- 当前仓库遗物目录
- WineFox 的遗物目录与基类模式

默认检查点：

- 是否使用 `[RegisterRelic(typeof(...Pool))]`
- 是否继承 `ModRelicTemplate` 或项目自定义遗物模板
- 如果 override 面或 helper 入口不清楚，先查 `STS2-RitsuLib.xml`
- 是否补齐 `relics.json` 和图标路径
- 是否需要 starter relic 或角色绑定

### 添加能力、药水、事件、角色、时间线、附魔

直接按章节走：

- 能力: `05 - 添加新能力`
- 药水: `06 - 添加新药水`
- 时间线: `09 - 添加时间线`
- 事件: `12 - 添加新事件`
- 附魔: `13 - 添加新附魔`
- 角色: `14 - 添加新人物`

先看 RitsuLib 文档章节；如果 public 接口面不清楚，先补查 `STS2-RitsuLib.xml`；再看 RitsuLib code 里的对应模板或注册属性，最后对照 WineFox 或当前仓库已有实现。

做附魔时默认额外确认一件事，不要省略：

- 先明确 `CanEnchant(CardModel card)` 的目标牌型与边界。
- 默认优先实现 `CanEnchantCardType(CardType)`，先把合法牌型在类型层收死。
- 默认不要只写 `base.CanEnchant(card)` 就结束。
- 推荐写法是双层判断：
  - `CanEnchantCardType(CardType)` 负责限制 `Attack`、`Skill` 或其它明确牌型集合
  - `CanEnchant(CardModel card)` 再补关键词、已有状态、额外限制
- 至少要确认它是否应限制为：
  - `CardType.Attack`
  - `CardType.Skill`
  - `Attack or Skill`
  - 或其它明确牌型集合
- 不要只靠事件、遗物、休息房等调用点的 filter 去兜底错误牌型；牌型约束应优先落在附魔模型自身。
- 如果用户没指定，默认主动确认或从同类成品中找出稳定口径；否则很容易把本不该吃附魔的牌型也放进候选，后续再补兼容会更贵。

### BaseLib -> RitsuLib 迁移

遇到这些请求时，先打开 [references/migration-map.md](references/migration-map.md)：

- “把这个 BaseLib 卡牌/遗物改成 RitsuLib”
- “`[Pool]` 在 RitsuLib 里对应什么”
- “为什么 ID 格式变了”
- “角色池、starter card、starter relic 怎么换”
- “事件、先古之民、升级替换在 RitsuLib 怎么写”

迁移时默认先替换这几类内容：

1. 注册特性
2. 内容基类
3. ID 约定
4. 关键词与动态变量
5. 角色/卡池/起始内容
6. 事件与先古之民
7. 升阶与替换映射

### 排错与定位

当用户说“没注册成功”“mod 没加载”“卡不出现”“Godot 脚本没绑定”“DLL 路径不对”时：

1. 先检查入口：`[ModInitializer]`、`EnsureGodotScriptsRegistered`、`RegisterModAssembly`
2. 再检查 manifest：`min_game_version`、`dependencies` 写法、依赖最低版本
3. 再检查运行时 `STS2-RitsuLib.dll` / `STS2-RitsuLib.xml` 与 `sts2.dll` / `sts2.xml` 是否都在、是否和本地引用链一致
4. 再检查 `csproj`：RitsuLib、`sts2.dll`、`0Harmony.dll` 引用路径
5. 再检查本地化、图片、场景路径
6. 如果是 patch apply 失败，先对照运行时 XML / 反编译确认 target 签名与 patch 形参名，再怀疑逻辑本身
7. 如果核心问题是“该挂哪一种时机”，回到 `timing-map.md`
8. 最后才看更宽的 Harmony 重写或别的分支行为

如果任务已经进入“需要高频实机复现或快速验证”的阶段，再补看：

- `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode`
  - 优先查它的 README、docs、manual、scripts。
  - 用来找游戏内直接生成卡牌/遗物、执行脚本、开调试开关、快速推进房间或战斗的做法。
  - 这是验证与调试参考，不替代 RitsuLib 的注册/API 权威地位。

如果怀疑符号或注册器名称写错，立刻运行：

```powershell
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "RegisterCard"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "ModRelicTemplate"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "ModTypeDiscoveryHub.RegisterModAssembly"
```

## Resources

### references/

- [references/source-map.md](references/source-map.md): 本机所有关键路径与权威用途
- [references/workflows.md](references/workflows.md): 各任务类型的“先看什么、再看什么”
- [references/timing-map.md](references/timing-map.md): 游戏本体 hook 与 RitsuLib lifecycle 事件的时机选择索引
- [references/migration-map.md](references/migration-map.md): BaseLib 到 RitsuLib 对照表
- [references/project-patterns.md](references/project-patterns.md): WineFox 与常见 RitsuLib 项目组织模式

### scripts/

- `scripts/find-ritsulib-symbol.ps1`: 跨 `RitsuLib-doc`、`RitsuLib-code`、WineFox、反编译代码和当前仓库搜索符号与案例
