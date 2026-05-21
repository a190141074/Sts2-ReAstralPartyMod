---
name: sts2-ritsulib-modding
description: Build or modify Slay the Spire 2 mods that use RitsuLib as a required dependency. Use when Codex needs a Chinese-first workflow for RitsuLib project setup, `ModInitializer` entrypoints, `RegisterCard` or `RegisterRelic` auto-registration, `ModCardTemplate` or `ModRelicTemplate` content authoring, BaseLib-to-RitsuLib migration, or debugging why a RitsuLib-based mod is not loading or registering content.
---

# STS2 RitsuLib Modding

## Overview

把这个 skill 当成“RitsuLib 前置 STS2 Mod 开发工作流”。它不替代 BaseLib skill；当仓库已经依赖 `STS2-RitsuLib`，或任务明确要求 `RegisterCard`、`ModTypeDiscoveryHub.RegisterModAssembly`、`ModCardTemplate`、`ModRelicTemplate`、`RitsuLibFramework` 时，优先使用这个 skill。

## Top Priority Project Rules

如果当前仓库存在以下文件，必须最先读取并优先执行其中规则：

1. `B:\Documents\re-astral-party-mod\doc\AGENT.zh.md`
2. `B:\Documents\re-astral-party-mod\doc\AGENT.md`

执行顺序以中文文件优先，英文文件为补充。它们的内容高于本 skill 的其它一般性建议；若与本 skill 冲突，以这两个文件为准。

## Updated Reference Roots

- `D:\MOD\杀戮尖塔2mod制作\RitsuLib-doc\RitsuLib`
  - 新版教程主入口，优先看这里的 `01 - 添加基础内容`、`02 - 玩法基底`、`03 - 模组工具`。
- `D:\MOD\杀戮尖塔2mod制作\RitsuLib-code\Docs\zh`
  - 当前 RitsuLib 中文 API 与注册器文档主入口。
- `D:\MOD\杀戮尖塔2mod制作\Slay-the-Spire-2-gdsdecomp\src\Core`
  - 当前反编译源码主入口，优先查这里的具体系统源码。

默认按照这条优先级取证并落地方案：

1. `RitsuLib-doc`
2. `RitsuLib-code`
3. 当前工作仓库
4. `STS2_WineFox-main`
5. 游戏反编译代码

## Quick Start

1. 先判断任务类型：项目初始化、卡牌、遗物、能力、药水、事件、角色、时间线/附魔、迁移、排错。
2. 打开 [references/workflows.md](references/workflows.md)，选对应工作流。
3. 打开 [references/source-map.md](references/source-map.md)，确认本机可用的权威路径。
4. 如果任务涉及 BaseLib 写法迁移，先看 [references/migration-map.md](references/migration-map.md)。
5. 如果任务需要项目组织参考或目录落点，先看 [references/project-patterns.md](references/project-patterns.md)。
6. 需要找真实符号、特性、注册器、示例时，运行 `scripts/find-ritsulib-symbol.ps1`，不要手写长搜索命令。

## Working Rules

- 优先使用 RitsuLib 已公开的注册/模板/内容包能力，不要先上 Harmony。
- 只有在 RitsuLib 没覆盖目标行为，或者任务显式要求改原生流程时，才看 Harmony 或反编译代码。
- 先确认“入口是否注册正确”，再写内容代码。很多“卡牌没出现”“遗物没加载”本质上是初始化或依赖问题。
- 先做最小可运行切片：入口、依赖、1 个内容类型、本地化、图标或场景路径，然后再扩展复杂联动。
- 当前项目已经是 RitsuLib 前置项目；如果用户在这个仓库里提问，优先对齐它的入口、命名、资源路径和本地化约定。
- `STS2_WineFox-main` 是高价值 RitsuLib 实战案例，但不要无脑照抄。先抽取结构模式，再贴合当前仓库实现。

## Task Routing

### 项目初始化与依赖

处理这些请求时优先看：

- `RitsuLib-doc\README.md`
- `RitsuLib-code\Docs\zh\GettingStarted.md`
- 当前项目 `ReAstralPartyMod.csproj` 与 `ReAstralPartyMod.json`
- 当前项目 `Scripts/MainFile.cs`

重点检查：

- `STS2-RitsuLib` DLL 或 NuGet 引用是否存在
- mod manifest `dependencies` 是否包含 `STS2-RitsuLib`
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
3. 用 `ModCardTemplate` 或项目自定义卡牌基类，而不是 `CustomCardModel`。
4. 补齐图片、本地化、必要的 starter 或 token 约定。

### 添加遗物

优先看：

- `RitsuLib-doc\03 - 添加新遗物\README.md`
- `RitsuLib-code` 中 `RegisterRelic` 和 `ModRelicTemplate`
- 当前仓库 `ReAstralPartyCardCode\Relics`
- WineFox 的遗物目录与基类模式

默认检查点：

- 是否使用 `[RegisterRelic(typeof(...Pool))]`
- 是否继承 `ModRelicTemplate` 或项目自定义遗物模板
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

先看 RitsuLib 文档章节，再看 RitsuLib code 里的对应模板或注册属性，最后对照 WineFox 或当前仓库已有实现。

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
2. 再检查 manifest：`dependencies`
3. 再检查 `csproj`：RitsuLib、`sts2.dll`、`0Harmony.dll` 引用路径
4. 再检查本地化、图片、场景路径
5. 最后才看 Harmony、反编译代码、运行时分支行为

如果怀疑符号或注册器名称写错，立刻运行：

```powershell
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "RegisterCard"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "ModRelicTemplate"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "ModTypeDiscoveryHub.RegisterModAssembly"
```

## Current Repo Notes

当前仓库 `B:\Documents\re-astral-party-mod` 已经切到 RitsuLib 前置：

- `ReAstralPartyMod.csproj` 引用了 `STS2-RitsuLib.dll`
- `ReAstralPartyMod.json` 的 `dependencies` 已包含 `STS2-RitsuLib`
- 入口在 `Scripts/MainFile.cs`
- 入口中先调用 `RitsuLibFramework.EnsureGodotScriptsRegistered(...)`
- 然后手动注册关键词，再调用 `ModTypeDiscoveryHub.RegisterModAssembly(...)`
- Harmony 仅保留给 RitsuLib 没覆盖的运行时 patch

如果任务是继续开发这个仓库，优先复用它自己的组织方式，而不是强行改成 WineFox 的目录结构。

## Resources

### references/

- [references/source-map.md](references/source-map.md): 本机所有关键路径与权威用途
- [references/workflows.md](references/workflows.md): 各任务类型的“先看什么、再看什么”
- [references/migration-map.md](references/migration-map.md): BaseLib 到 RitsuLib 对照表
- [references/project-patterns.md](references/project-patterns.md): WineFox 与当前仓库的目录/入口/组织模式

### scripts/

- `scripts/find-ritsulib-symbol.ps1`: 跨 `RitsuLib-doc`、`RitsuLib-code`、WineFox、反编译代码和当前仓库搜索符号与案例

回答时保持结论落地：说明应看哪个文档、哪个目录、哪个注册点，以及为什么。
