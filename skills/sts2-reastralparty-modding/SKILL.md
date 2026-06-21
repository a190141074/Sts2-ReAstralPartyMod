---
name: sts2-reastralparty-modding
description: Repo overlay for `B:\Documents\re-astral-party-mod` on top of `sts2-ritsulib-modding`. Use when work targets this repo and needs repo-specific localization, content-family verification, combat and sync debugging, or startup system routing rules.
---

# STS2 ReAstralParty Modding

## Overview

把这个 skill 当成 `B:\Documents\re-astral-party-mod` 的仓库专属 overlay。它不是 RitsuLib 通用替代品，而是叠加在 `sts2-ritsulib-modding` 之上，专门处理这个仓库反复出现的命名链、本地化、日志排错、成套内容校验、启动系统和联机同步问题。

默认加载顺序：

1. `sts2-ritsulib-modding`
2. `B:\Documents\re-astral-party-mod\doc\AGENT.zh.md`
3. `B:\Documents\re-astral-party-mod\doc\AGENT.md`
4. 本 skill
5. 本 skill 的 [references/workflows.md](references/workflows.md)
6. 本 skill 的 [references/timing-map.md](references/timing-map.md)
7. 本 skill 的 [references/bilingual-glossary-template.md](references/bilingual-glossary-template.md)

## Maintenance Rule

- Codex 全局目录是主编辑源：
  - `C:\Users\LuoYe\.codex\skills\sts2-reastralparty-modding`
- 仓库里的镜像必须同步维护：
  - `B:\Documents\re-astral-party-mod\skills\sts2-reastralparty-modding`
- 以后任何对 `sts2-ritsulib-modding` 或本 overlay 的修改，都要同时更新：
  - Codex 全局 skill
  - 仓库 `skills/` 镜像
- 如果只改了一边，视为未完成。
- 这份时机索引不是一次性文档；后续每次通过反编译或真实实现验证出更合适的时机，都应更新到对应层，而不是只留在 rollout summary。

## Repo Working Rules

- 先看日志，再看表象。这个仓库里 UI 症状、联机分叉、战斗卡死经常只是下游表现。
- 先找仓库里同类成品，再写新内容。尤其是 `VariantPerson*`、月球遗物、起始人格链、隐藏立绘链。
- 默认优先信任仓库公共基底的自动命名链，不要无理由手写 override：
  - `AstralPartyRelicModel`
  - `AstralPartyCardModel`
  - `AstralPartyPowerModel`
- 如果必须手写 `RelicId`、`IconBasePath`、`CardId`、`PortraitBasePath`、`FrameBasePath`、`PowerId`、`ResolveIconPath()`，先证明默认链不适用，再确认不会把同套内容的命名链写散。
- 遇到“回合开始该挂哪一种 / 该不该 patch / 哪个 hook 更窄更稳”这类问题，先读 `references/timing-map.md`。
- 遇到中文玩法名、遗物名、系统名容易翻错或混用时，先看 `references/bilingual-glossary-template.md`，后续按仓库实际用词补全。
- 发现一种会反复出现的 repo-specific 失败模式时，默认要把它沉淀回这个 overlay，而不是继续留在临时对话里。
- 仓库里凡是需要跨存档、联机或显示稳定性的概率/倍率/百分比状态，默认先想“稳定整型刻度”而不是 `float` / `decimal` 持久化。
- 遇到单条功能链的 API 漂移时，默认优先做 feature-local compat helper；不要动不动扩成全局 patch 或通用兼容层。
- 对依赖本地化、UI 节点树、延后初始化对象的 patch，优先考虑 required patcher + deferred patcher 分层，并把 apply 时机绑到明确初始化事件。
- 做多阶段玩法链时，如果参数已经开始包含来源、目标、交付方式、bonus/fallback 等多维状态，默认尽快收口成窄 `...ExecutionContext` / `...ResolveContext`。
- 当前仓库里，构建 warning 只要暴露空引用、不安全重载、可空边界失真，默认就按 runtime 薄弱点修，不把它们当纯编译噪音。
- 对选牌、抽奖励、生成怪物、显示 hover 这类高频链，默认接受“返回 null / 数量不符就直接安全返回”的保守语义，优先不中断主流程。

## Repo Stability Notes

### 本地化与查看 UI

- 先看 `logs\saves\mod_data\DevMode\instances\*\session.log`
- 再看 `logs\godot*.log`
- 先分流，再修：
  - `GetRawText: Key '...title/description/...' not found`
    - 这是缺 key、ID 不匹配、写错 table、或 locale 回退。
  - `Localization formatting error`
    - 这是 key 找到了，但格式变量没被注入。
- 这两类问题在本仓库经常并存，不要统称成“本地化没写好”。
- `PowerModel.SmartDescription` 是高风险面：
  - 没有 `smartDescription` 时回退 `Description`
  - 有 `smartDescription` 时只会 `new LocString("powers", SmartDescriptionLocKey)`
  - 不会自动补 `Amount`、`DynamicVars` 或别的占位符
- 因此 `smartDescription` 里凡是有 `{Amount}`、`{Threshold}`、`{Energy}`、`{OwnerName}` 之类变量，都必须先确认当前查看链显式注值。
- 做中英文对照或本地化翻译时，前缀要先单独识别，再决定是否简写：
  - 写清楚前缀不是为了强制保留前缀，而是为了翻译时优先判断“这个前缀是否可以安全省略”
  - 常见像 `Person*`、`Variant Person*`、`*Power`、`Moon Prop*`、`Token Gold*`、`Base Ability*` 这类，都先按结构识别，再做简称
- 中英文本地化除了语义对应，还要保持标签结构对齐：
  - 不要出现中文用 `[gold]`、英文改成 `[card]` 这种跨语种结构漂移
  - 不要出现中文用 `[blue]`、英文改成 `[power]` 这种标签替换
  - 本仓库本地化统一不使用 `[power]`、`[card]`、`[relic]` 这类标签
- 本地化里的占位变量写法要遵循 RitsuLib 教程和当前仓库注值链：
  - 先确认变量由哪条 `LocString` / `SmartDescription` / helper 注入
  - 不要在文案里随意新增仓库当前不会注值的占位符
  - 变量名与花括号结构在中英日三语里保持一致，避免只改某一语种

### 新异格 / 新人格 / 成套内容

- 不能只看抽象基类就开写，必须先找至少一个仓库内已正常工作的同类成品。
- 对 `VariantPerson*`、`Person*`、配套 skill card、配套 power，一律整套核对，不只检查 relic。
- 对 `CooldownPersonaRelicBase` 这类冷却型人格，以及仓库内同口径的冷却型异格，默认把配套 skill card 配成：
  - `ShouldAutoApplyCooldownEnchantment => true`
  - `CanonicalKeywords` 包含 `AstralUnique`
  - 除非用户明确要求关闭冷却附魔、移除唯一，或另行指定别的词缀组合。
- 做附魔时不要默认放宽 `CanEnchant`：
  - 先明确目标牌型，再落代码。
  - 默认优先实现 `CanEnchantCardType(CardType)`，先把牌型边界封在附魔模型自身。
  - 推荐保持双层判断：
    - `CanEnchantCardType(CardType)` 管牌型
    - `CanEnchant(CardModel card)` 管关键词、已有附魔、额外状态
  - 只要入口是“从牌组中选可附魔卡牌”，优先尝试 `CardSelectCmd.FromDeckForEnchantment(...)` 之类专用入口，不要先上 generic 选牌再补 filter。
  - 当前仓库已踩过“冷却附魔没限定牌型，后面再补兼容”的坑；默认先问清或沿同类成品收口成 `Attack`、`Skill` 或 `Attack/Skill`。
  - 不要只靠调用点 filter 兜底错误牌型；这类约束要先落在附魔类本体。
- 新增或排错时，至少一起搜索这些面：
  - `RelicId`
  - `IconBasePath`
  - `CardId`
  - `PortraitBasePath`
  - `FrameBasePath`
  - `PowerId`
  - `ResolveIconPath()`
- 还要核对这 4 个面是否一致：
  - `RegisterRelic(...)` 的 public entry / `StableEntryStem`
  - 代码 id
  - 三语本地化 key
  - 资源命名
- 只要运行时真实 entry 和本地化 key 有一处拼写分段不一致，就优先修这个，不要先怀疑 UI。
- 只要某个 power 会被 `HoverTipFactory.FromPower<T>()` 引到 relic/card 查看链里，就必须把它当成 canonical 模型也会被访问的对象处理。
- `Description`、`SmartDescriptionLocKey`、`GetDescriptionLocKey()`、`GetSmartDescriptionLocKey()` 这些查看链函数里，默认不要直接读取 `Owner`、`Owner.Player`、`CombatState` 等 mutable 状态，除非先做 canonical-safe 防护。
- 卡牌升级相关需求里，如果目标只是“突破默认升级上限”，默认先检查目标卡类能否直接设置 `MaxUpgradeLevel`；不要先猜需要额外 patch 升级系统。

### 战斗 / 联机 / 日志优先

- 对联机或战斗异常，先抓最早的硬错误，再区分是否还伴随 checksum divergence。
- 不要拿分叉症状代替根因。
- 战斗 hook 里只要遍历目标集合时可能造成击杀、移除、融化、power 移除、队列改写，就优先怀疑 live collection mutation。
- 默认先快照再结算，不要边遍历边调用可能改集合的命令。

### 新版兼容 / patch 漂移

- 当前仓库做新版兼容时，先清 loader 级红字，再清 gameplay patch 红字；不要先被下游 UI 症状带跑。
- 对 `Persona -> Person` 这类内部命名纠偏，兼容层不要长期保留：
  - 如果只是仓库内调用点过渡，优先尽快删掉 `global using` 旧名别名，直接把调用点改成 `Person*` 正名。
  - 否则后面很容易继续把 `StartingPersona*` 之类错误名扩散进 settings / snapshot / UI 链。
- 如果确实要给本地 JSON / telemetry active-run 快照做旧字段兼容，先确认 `System.Text.Json` 实际落盘命名策略：
  - 当前仓库 `JsonSerializerDefaults.Web` 默认是 camelCase。
  - 不要拿 `PersonaSelected` 这种 PascalCase 字面去判断旧文件内容是否存在；要么按真实落盘字段名兼容，要么直接不保留旧快照兼容。
- 运行时若出现 `Detected old-style dependencies without min version specified` 或 `does not declare min game version`，先修 `ReAstralPartyMod.json`：
  - 补 `min_game_version`
  - 依赖改成对象写法并补 `min_version`
- 对 Harmony / patcher 直连原生方法时，参数名也属于兼容面：
  - 例如 `AbstractModel.AfterCardPlayed(...)` 当前运行时参数名是 `choiceContext`
  - patch 形参随手写成 `context` 也会导致 apply 失败
- `Hook.AfterTurnEnd` 这类原生 Hook 不要沿用旧的双参 target 记忆；当前分支先对照运行时 `sts2.xml` 或 RitsuLib lifecycle patch 的 target 列表确认。
- 日志里 `[Optional] ... Failed` 也会把启动阶段刷红；先区分这些 optional patch 是否只是兼容漂移，还是已经影响主流程。

### 仓库特定系统面

- 起始人格 / Neow restore 问题：
  - 先查状态恢复和重建链，例如 `StartingPersonaNeowReadyFlow`、`StartingPersonaRelicSelectionScreen`、`SetInitialEventState(...)`
  - 不先猜 UI 延迟、动画时序或展示层问题
- 隐藏 beta 立绘：
  - 先看 `godot*.log` 的实时设置
  - 再看资源目录是否真有对应 `_beta` 文件
  - 再查子类是否 override 了默认路径解析
- 发牌入运行牌组：
  - 优先走正式 `RunState.AddCard(...)` 链
  - 不要先手动给 `Owner`
  - 修 helper 时顺带检查同一条入牌链的相邻调用点，避免只修一处

### 玩法设置 / 内容模式

- 这个仓库的“玩法设置”默认不是单一 UI 开关，而是跨 4 层语义：
  - 本地设置
  - 房间玩法面板 / lobby snapshot
  - run snapshot
  - 运行时 getter / installer / patch 读取
- 只改其中一层通常不够；如果用户说“增加玩法设置”或“房间同步”，默认一起检查：
  - `ReAstralPartyModSettings`
  - 设置页注册
  - `CharacterSelectGameplayPreviewPatch`
  - `LobbyGameplaySettingsSync`
  - `ReAstralPartyRunSettingsSync`
  - `ReAstralPartyModSettingsManager` getter
- `原版模式 / 整合包模式` 下，优先把玩法项的可编辑/锁定/隐藏语义集中放进 `AstralContentModeRegistry`，不要把 mode 判断散在每个 toggle 和业务 patch 里。
- 明确区分“玩法设置”和“通用设置”：
  - 玩法设置可以做 mode-scoped 双份配置
  - `其他 / 遥测 / 通知 / 联机诊断` 这类通用设置应保持单份全局值，不应随内容模式切换而跳到另一套状态
- 如果某个总开关只是隐藏 UI，不足以代表运行时关闭；默认还要在统一 getter 层收口，让 runtime 也按关闭处理。
- 对“默认启用 / 默认 ban”类需求，不要只改 `ReAstralPartyModSettings` 字段默认值；默认一起检查：
  - 持久化设置迁移是否真的落盘到 `settings.json`
  - `_runtimeSettings` 首次建立时是否已经吃到迁移结果
  - lobby / run snapshot 初始值是否带上该默认值
  - 对应 UI 列表是否真的包含目标内容，而不是只改了状态不改来源列表
- 设置 / lobby / run snapshot 这类边界类型里，如果旧错误命名只是过渡 alias，不要长期依赖：
  - 能直接改主字段/主类型名的，尽快改成 `StartingPerson*`、`Person*`。
  - 只把真正需要跨版本读取旧存档/旧消息的兼容留在边界层，不要把 alias 扩散回业务代码。

### 商店 / 月球体系

- 月球商店相关逻辑默认先分清两条语义，不要混写：
  - `EnableMoonPropRelics` 控制普通商店自然库存里是否允许出现月球遗物
  - `EnableMoonPropShopSlots` 控制商店下方额外固定追加的 3 个月球遗物位
- 仓库里如果本身已经有现成框架，例如 ban relic、settings sync、reward sync、候选池过滤，默认先沿现有框架查“为什么没命中”，不要一上来给单个内容补特殊分支。
- 对 ban relic 这类框架问题，先核对真实运行时 canonical ID，再看 UI、默认值和过滤入口：
  - 优先从运行时保存、历史记录、日志或 `ModelDb` 确认真实 ID
  - 不要拿手写短 ID、显示名或想当然的 entry stem 直接当过滤 key
- 如果要禁止普通商店自然刷某类遗物，优先“替换 entry”为同类型可用替代项，不要直接删除 entry，避免商店正常位数量变少。
- 特殊商人 / fake merchant 仍然是高风险链：
  - 优先在 `NMerchantInventory.Initialize(...)` 前缀整体排除 `NFakeMerchantInventory`
  - 不要只在 UI clone 或单个 helper 里晚期兜底
- 当同一功能同时改库存数据和商店 UI 时，默认先改正常库存，再补额外 slots，避免把追加位误当成普通库存过滤掉。
- ban relic 机制默认不是底层全局强封禁，而是“各个来源主动读取 ban 集”：
  - 如果需求只是“默认不让某个商店/卡池/候选池刷出某件 relic”，优先修对应来源入口 + 默认 ban 落盘链
  - 如果需求是“所有来源彻底禁用某件 relic”，需要逐入口审计，不要假设 ban UI 自动全局生效

### 构建 / 导出

- 对 `B:\Documents\re-astral-party-mod`，默认“构建导出”就是直接更新游戏目录：
  - `D:\Steam\steamapps\common\Slay the Spire 2\mods\ReAstralPartyMod\`
- 不要默认再额外导出到临时验证目录、仓库自定义输出目录或其它多余位置。
- 只有当游戏目录文件被占用、Godot 导出失败、或用户明确允许 compile-only fallback 时，才退回临时验证产物。
- 如果只是 C# 改动且资源未变，优先确认 DLL 已更新到游戏目录；不要把“compile-only 产物生成成功”当成“已经导出”。

### 隐藏运行时载体

- 这个仓库里，若某个玩法用隐藏 modifier 或等价运行时组件更稳定，可以采用，但默认要求：
  - 不显示图标
  - 不进常规 modifier UI
  - 不进结算展示
- 这条尤其适用于清醒梦、梦境模式、只用于保存本局状态的隐藏效果载体；可见 UI 与运行时持有层应分开设计。

### Decimal 边界

- `decimal` 只用于运行时计算和公式推导。
- 一旦跨到存档、联机同步、奖励状态、计数器显示、HP 快照、阈值里程碑等边界，必须转成稳定标量形态。
- 优先复用 `ReAstralPartyCardCode\Utils\StableNumericStateHelper.cs`，不要在业务文件里散落重复的取整和序列化逻辑。
- 不要给 `[SavedProperty]` 直接挂 `decimal`。
- 不要把 `decimal` 直接作为需要持久化或奖励同步的字段形态。

### `019e56fb...` 相关仓库经验

- 对“某张具体卡进入某个牌堆后触发”的行为，默认优先考虑 `AstralPartyCardModel.AfterCardChangedPiles(...)` 这种窄 hook，而不是先上全局 patch。
- 先澄清效果语义，再决定是复用现有概念层还是新造一层。对本仓库，像 Omen 这种已有壳层，默认优先扩展现有 `*Base`，不要新造平行概念。
- 生成卡牌结果优先走仓库现成的多人局安全 helper，不要直接绕开通知链。

## When To Use This Overlay

- 目标仓库是 `B:\Documents\re-astral-party-mod`
- 任务涉及本仓库自己的遗物、卡牌、power、人格、起始流程、月球体系、隐藏资源链
- 症状包含：
  - UI 显示英文 / 原始 ID
  - hover / 查看时本地化异常
  - 战斗中卡死、无发牌、无 intent、联机分叉
  - 起始人格或 Neow 链状态错乱
  - 资源明明存在但运行时不显示
