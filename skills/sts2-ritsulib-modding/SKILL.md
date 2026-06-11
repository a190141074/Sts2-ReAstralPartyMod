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
- `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode`
  - 游戏内测试、作弊、脚本执行与 mod 调试工具箱。
  - 当任务涉及“如何更快复现/验证内容”“如何在游戏里直接测卡牌/遗物/事件”“如何做脚本化调试”时，优先把它作为实机验证参考。
- `D:\MOD\杀戮尖塔2mod制作\Slay-the-Spire-2-gdsdecomp`
  - 当前反编译源码主入口，优先查该目录下 `src\Core` 的具体系统源码。

默认按照这条优先级取证并落地方案：

1. `RitsuLib-doc`
2. `RitsuLib-code`
3. 当前工作仓库
4. `STS2-DevMode`
5. `STS2_WineFox-main`
6. 游戏反编译代码

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
- 如果本轮排查/修复得到的是一种后续很可能反复出现、且对这个仓库长期制作有帮助的注意事项、稳定性规则、实现约束或排错经验，默认要主动询问用户是否需要把它补进 skill，避免之后重复遗漏。

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

## Current Repo Notes

当前仓库 `B:\Documents\re-astral-party-mod` 已经切到 RitsuLib 前置：

- `ReAstralPartyMod.csproj` 引用了 `STS2-RitsuLib.dll`
- `ReAstralPartyMod.json` 的 `dependencies` 已包含 `STS2-RitsuLib`
- 入口在 `Scripts/MainFile.cs`
- 入口中先调用 `RitsuLibFramework.EnsureGodotScriptsRegistered(...)`
- 然后手动注册关键词，再调用 `ModTypeDiscoveryHub.RegisterModAssembly(...)`
- Harmony 仅保留给 RitsuLib 没覆盖的运行时 patch

如果任务是继续开发这个仓库，优先复用它自己的组织方式，而不是强行改成 WineFox 的目录结构。

## Repo Localization Stability Note

当任务落在 `B:\Documents\re-astral-party-mod`，且症状是“UI 显示英文 / 显示原始 ID / 查看时报本地化错 / SmartDescription 格式炸裂”时，先按下面顺序取证，不要把所有现象都笼统归类成“本地化没写好”：

1. 先看 `B:\Documents\re-astral-party-mod\logs\saves\mod_data\DevMode\instances\*\session.log`
2. 再看 `B:\Documents\re-astral-party-mod\logs\godot*.log`
3. 先分类，再修：
   - `GetRawText: Key '...title/description/...' not found`
     - 这是缺 key、ID 不匹配、写进了错误 table、或 locale 回退问题。
     - 常见表现是 UI 显示英文、原始 ID、或直接回退到别的语言。
   - `Localization formatting error`
     - 这是文案 key 能找到，但格式字符串里的变量没有在当前调用链被注入。
     - 常见表现是 hover、查看 UI、loadout overlay、战斗内详情页打开就报格式化异常。

这两个问题在这个仓库里经常同时出现。先确定是“缺 key 回退”、还是“格式化变量炸裂”、还是两者并存，再决定修 key 还是修文案占位符。

对 `PowerModel` 额外加一条硬规则：

- `PowerModel.SmartDescription` 并不会像某些 `Description`/选择提示链那样自动补 `Amount` 或 `DynamicVars`。
- 反编译代码的实际行为是：
  - 没有 `smartDescription` 时，直接回退 `Description`
  - 有 `smartDescription` 时，只执行 `new LocString("powers", SmartDescriptionLocKey)`
- 因此 `smartDescription` 里凡是写 `{Amount}`、`{Threshold}`、`{Energy}`、`{OwnerName}` 或其他占位符，必须先确认该模型在这条 UI 调用链里显式注值；否则查看 UI 非常容易触发 `Localization formatting error`。
- 不要把“`Description` 可正常显示动态值”直接类比成“`SmartDescription` 也安全”。

对 `B:\Documents\re-astral-party-mod` 里的“新异格 / 新人格遗物”再加一条硬规则：

- 不能只参考抽象基类如 `AstralPartyRelicModel`、`PersonaRelicBase`、`CooldownPersonaRelicBase` 就直接开写。
- 必须先找仓库里至少 `1` 个已经正常工作的同类成品对照，优先看：
  - 另一个 `VariantPerson*` 遗物
  - 同样带技能卡/能力 hover 的人格遗物
  - 已经进过起始人格选择链的遗物
- 新增内容时，必须显式核对这 `4` 个面是否一致，而不是默认“基类会自动兜底”：
  - `RegisterRelic(...)` 的 public entry / `StableEntryStem`
  - 代码里的 `RelicId`
  - 三语 `relics.json` 的实际 key
  - 资源命名与 `IconBasePath`
- 只要发现“代码 id 是一种写法，而现有同类运行时 public entry 是另一种写法”，必须优先锁 `StableEntryStem` 或补兼容 key，不能继续赌 RitsuLib 自动推导结果。
- 尤其对手写分词 id（例如 `ling_yu_lin` 这类），不要凭肉眼假设运行时 public entry 一定保留同样分段；先对照一个现成 `VariantPerson*` 实例，或直接查运行日志 / 存档里的真实 entry。

对 `B:\Documents\re-astral-party-mod` 里的“成套新增内容”（例如 `VariantPerson* + 人格技能牌 + 配套 Power`，或一组月球 relic / helper / power）再加一条硬规则：

- 不能只检查其中一个主件，尤其不能只检查 relic 本体或只检查 `relics.json`。
- 只要仓库里已经有同类成品，必须按“整套比对”执行，至少同时核对：
  - relic
  - 配套 card
  - 配套 power
  - 三语本地化
  - `csproj` 资源项
  - `.import` / 贴图命名
- 任何一个子件只要走的是仓库公共默认命名链，就优先保持默认，不要手写覆盖。
- 对这个仓库，下面这些显式覆盖默认都必须视为“高风险例外”，写之前先证明默认链真的不适用：
  - `RelicId`
  - `IconBasePath`
  - `CardId`
  - `PortraitBasePath`
  - `FrameBasePath`
  - `PowerId`
  - `ResolveIconPath()`
- 默认判断标准：
  - `AstralPartyRelicModel` 已能按类名推导 relic id 与图标路径
  - `AstralPartyCardModel` 已能按类名推导 card id 与 portrait 路径
  - `AstralPartyPowerModel` 已能按 id / 类名推导 power 图标路径
- 如果没有“共享旧资源”“运行时 public entry 不一致”“分阶段换图”“原生特殊图标链”这类明确理由，就不要新增这些 override。
- 新增这类内容前，先搜索一次相关文件里的 `override string .*Path`、`RelicId`、`CardId`、`PowerId`；把每一条显式指定都当成需要解释的例外，而不是默认写法。

对 inspect / hover 再补一条仓库级规则：

- 只要某个 power 会被 `HoverTipFactory.FromPower<T>()` 引到 relic/card 查看链里，就必须把它当成“canonical 模型也会被访问”的对象处理。
- `Description`、`SmartDescriptionLocKey`、`GetDescriptionLocKey()`、`GetSmartDescriptionLocKey()` 这些查看链函数里，默认禁止直接读取 `Owner`、`Owner.Player`、`CombatState` 等 mutable 状态，除非先做 canonical-safe 防护。
- 若描述分支确实依赖持有者状态，优先：
  - 使用不触发 `AssertMutable()` 的显式状态来源
  - 或在描述选择函数里做 canonical-safe 兜底
- 不要先假设“战斗里能读到 Owner，所以 hover 里也能读到”。

这条规则的目标很明确：

- 当仓库里已经有整套同类参照物时，后续新增同类内容默认先“照着已验证链路比对”，而不是只凭基类和命名习惯推断。
- 如果不先比对同类成品，再去补本地化，极容易出现“json 文案都写了，但运行时实际拿的是另一条 public entry”这种重复错误。

## Repo Stability Note

当前仓库已经有一条需要长期坚持的数值边界规则，处理 `decimal` 时必须优先遵守：

- `decimal` 只用于运行时计算和公式推导。
- 一旦跨到存档、联机同步、奖励状态、计数器显示、HP 快照、阈值里程碑这类边界，必须改成稳定标量形态。
- 默认优先使用 `ReAstralPartyCardCode\Utils\StableNumericStateHelper.cs`：
  - `SerializeDecimal` / `DeserializeDecimal`
  - `SerializeDecimalSequence` / `DeserializeDecimalSequence`
  - `FloorToNonNegativeInt`
  - `RoundToNonNegativeInt`
  - `FloorDivisionToNonNegativeInt`
  - `ClampCeilingToInt`

明确要求：

- 不要给 `[SavedProperty]` 直接挂 `decimal`。
- 不要把 `decimal` 直接作为需要持久化或奖励同步的字段形态。
- 不要在多个文件里重复手写 `Math.Floor`、`Math.Round`、`Math.Ceiling` 到 `int` 的边界转换逻辑；优先收口到 `StableNumericStateHelper`。
- 如果原版或现有逻辑允许百分比/倍率先产生小数，保留小数计算；真正应用到游戏整数资源时，再统一按固定规则落整。

默认策略：

1. 公式阶段保留 `decimal`
2. 状态保存阶段转为 `string`/`int`/其他 BaseLib 支持类型
3. 显示与计数阶段走统一 helper
4. 需要 deterministic 阈值结算时，统一复用 helper，不再各自实现

## Resources

### references/

- [references/source-map.md](references/source-map.md): 本机所有关键路径与权威用途
- [references/workflows.md](references/workflows.md): 各任务类型的“先看什么、再看什么”
- [references/migration-map.md](references/migration-map.md): BaseLib 到 RitsuLib 对照表
- [references/project-patterns.md](references/project-patterns.md): WineFox 与当前仓库的目录/入口/组织模式

### scripts/

- `scripts/find-ritsulib-symbol.ps1`: 跨 `RitsuLib-doc`、`RitsuLib-code`、WineFox、反编译代码和当前仓库搜索符号与案例

回答时保持结论落地：说明应看哪个文档、哪个目录、哪个注册点，以及为什么。
