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
- 官方 `zhs` 风格只作为后续新写或重写文案时的参考基线，不自动回刷当前仓库已有文本。
- 如果用户明确说“先不使用”“只总结”“不要回刷现有文本”，则这类工作只更新 skill / glossary 规则，不修改 `ReAstralPartyMod/localization/*`。
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
- 本仓库本地化默认走务实风格：
  - 优先保留玩家一眼能懂的核心效果，不擅自扩写成“解释实现过程”的长句
  - 如果用户已经给出更短口径，默认直接贴用户口径，不自作主张补“正层数”“取消这次获得”这类实现细节
  - 只有当省略后会直接改变规则理解时，才补最小必要限定
- 参考官方 `zhs` 风格时，默认额外遵守这些边界：
  - 官方风格解决的是句式、术语、标签克制和玩家阅读顺序，不自动覆盖仓库既定世界观命名
  - 如果仓库已有明确前缀、玩法名或用户指定口径，仓库口径优先
  - 如果官方习惯写法与仓库既定术语冲突，先记入 glossary 或 skill，再单独确认，不直接替换现有内容
  - 复用官方高频表达时，仍要遵守本仓库现有规则：标签结构跨语种对齐、不要新增未注值占位符、默认务实短句

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
- 默认优先看“最新带时间戳日志里的第一条硬错误”，不要先被外层 async 包装栈、后续连锁报错或滚动 `godot.log` 里的旧噪音带跑。
- 如果故障是在某轮最近修改之后才出现，默认先沿“最近改动链”排查；除非最新日志里的更早硬错误已经明确指向别处，否则不要优先翻旧系统。
- 启动期的 topbar / hover / summary / debug display 不是“无害 UI”：只要它们在 `_Ready`、`Initialize`、run bootstrap 或 map/topbar 初始化阶段执行，就按主流程代码处理，任何 `ModelDb` / 本地化 /资源读取都必须允许安全失败。
- 普通 `ModifierModel` 不是无害运行时载体：只要进入 `RunState.Modifiers`，原版 Neow 就会按 modifier run 处理，导致普通 Neow 遗物选项不生成。不要在 `StartRun`、run bootstrap、Neow ready-page 或初始事件生成前安装隐藏 modifier。
- 做内容迁移、删包或拆独立模组后，默认一起复核“运行时注册表 + 系列映射表 + topbar/hover/debug summary + fallback 文案表”；不要只删玩法入口，遗漏展示链中的旧 `ModelId`。
- 如果沿错误方向做了试探性修改，一旦最新日志证明根因不在那条链上，就应立即回退这部分无关改动，再收口到真正相关的最近改动链；不要把误判修补继续留在工作树里。
- 战斗 hook 里只要遍历目标集合时可能造成击杀、移除、融化、power 移除、队列改写，就优先怀疑 live collection mutation。
- 默认先快照再结算，不要边遍历边调用可能改集合的命令。

### 召唤物 / 分身 / 宠物

- 这类内容默认按高风险链处理；不能只因为 `PlayerCmd.AddPet(...)` 成功返回，就判定“召唤物已经做完了”。
- 不要再走“`AddPet` + 复制本体视觉节点”这种半套实现：
  - 这最多只能证明战斗里多了一个实体引用
  - 不能证明它有稳定的血条、hover、交互、受击、承伤和位置表现
- 开写前必须先找一个已验证可用的完整成品，对齐至少这 6 条链：
  - 入场
  - 独立位置
  - 血条 / 可交互
  - hover / tip
  - 受伤 / 承伤 / 目标转移
  - 联机或战斗内同步
- 如果目标语义里包含“替本体承伤”“同步本体打牌”“战斗结束回收生命”这类复合机制，先把“召唤物实体链稳定”单独证明，再接后续玩法；不要一边补实体、一边叠镜像行为。
- 若没有仓库内现成成品可直接复用，就先去外部已工作的 pet / companion / summon 实现取证，再决定是否落代码；不要靠抽象基类和 API 名字猜。
- 若直到取证结束，都不能证明这条召唤物链能稳定覆盖玩家真正关心的表现面，就应直接降级方案或明确说做不到，不要先交一个“看起来像成功生成、实际上不可用”的半成品。

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
  - 如果 Neow 普通遗物选项消失，优先检查是否有我方隐藏/调试/全局玩法 `ModifierModel` 过早加入 `RunState.Modifiers`；原版 `Neow.GenerateInitialOptions()` 会用 `RunState.Modifiers.Count <= 0` 决定是否走普通遗物分支
- 人格选择 / `StartingPerson*` / `StartingPersona*` / Neow ready-page / host-launch / persona overlay / remote choice pre-restore` 默认属于仓库高危区：
  - 能不动就不动；这条链默认按坏味道很重的历史屎山处理，不要把它当普通 UI / 同步功能随手重构
  - 只要用户已经明确表达“尽量不要碰 / 能不动就不动 / 先查问题”，默认停在日志取证和根因定位；除非用户随后明确说“执行 / 修改 / 实现”，否则不要擅自动这条链
  - 没有明确根因和双端日志验证时，不要轻易改
  - 如果最新带时间戳日志还没有稳定复现当前症状，不要拿历史 `godot.log` 噪音、旧异常或“看起来像点击层问题”的猜测直接下手改 `StartingPersonRelicSelectionScreen`
  - 任何改动后至少一起核对：
    - 主机 `host_local_click optionPayloadCount`
    - 客机 `host_broadcast optionPayloadCount`
    - 主客 overlay 候选数量与顺序
    - `EVENT.NEOW` 首次真实点击前是否仍停在 ready-page
  - “界面能弹出”不算通过；必须确认主客吃到的是同一份候选人格 payload
  - 这条链优先最小修补，不要顺手重构或扩大到别的同步系统
  - 验证这条链时，默认优先看带时间戳的最新 `godotYYYY-MM-DDTHH.MM.SS.log`，不要先拿滚动 `godot.log` 里的旧异常下结论
  - 如果 `godot.log` 里仍残留旧的 `P132A`、`PlayerVotedForRelic`、黑图标阶段报错，但最新带时间戳日志里已经满足：
    - 主机 `host_local_click optionPayloadCount == resolvedCount`
    - 客机 `host_broadcast optionPayloadCount == resolvedCount`
    - `P014/P015` 正常出现
    - 没有新的首个硬错误打断 overlay 初始化
    - 则默认判定“最近一次已通过”，不要被滚动日志里的历史噪音误导
  - 这条链里，主机本地和主机广播绝不能各自重新生成一份人格候选：
    - `StartingPersonNeowReadyFlow` 必须先生成一次 `serializedRelicOptionIds`
    - 这同一份 payload 同时给 `host_local_click`
    - 同时给 `StartingPersonHostLaunchSync.BroadcastLaunch(...)`
    - 不允许主机本地 `HandleReadyLaunchAsync(...)` 走空 payload 或默认 `null`
  - `StartingPersonHostLaunchSync.BroadcastLaunch(...)` 这类入口，默认优先显式接收已生成 payload，不要在广播函数内部再隐式重建一份候选列表；否则主客很容易因为二次生成时机不同而分叉
  - 如果人格选择 overlay 出现“全黑图标 / 黑圆圈 / 只有底板没有内容”，默认先怀疑 overlay 初始化阶段被异常打断，不要先怀疑图片资源丢失：
    - 先查 `PlayerVotedForRelic`
    - 先查 `NMultiplayerVoteContainer.RefreshPlayerVotes`
    - 先查 `StartingPersonRelicSelectionScreen.BuildRelicHoldersIfNeededAsync()`
    - 很多情况下黑图标只是 holder 停在初始 `Colors.Black`，不是贴图真的没包进去
  - `StartingPersonRelicSelectionScreen` 在 holder 初建阶段默认不要急着刷 vote UI：
    - 不要在每个 holder `Initialize(...)` 后立刻 `RefreshPlayerVotes()`
    - 不要在 holder 全部建完但真实选择/投票上下文还没稳定前强刷 `RefreshVotes(false)`
    - 如果这里命中 `PlayerVotedForRelic` 越界，优先先停掉早期 vote refresh，再决定是否补更窄 gate
  - 如果需求是“`集体准备` 只有房主能开启人格选择”，默认优先把门禁收在 `StartingPersonNeowReadyFlow`：
    - host / singleplayer 才允许 ready option 真正触发 `OnReadyChosenAsync(...)`
    - client 侧可以保留 ready-page 展示，但要在 `ApplyLocalReadyOptionUi(...)` 里做本地不可交互处理
    - 不要让 client 通过本地点击直接打开 overlay；实际启动应继续走 `StartingPersonHostLaunchSync.BroadcastLaunch(...)` + `HandleReadyLaunchAsync(...)`
  - 如果需求是“背景底图不能丢”，先查 `StartingPersonRelicSelectionScreen` 自己的背景链，而不是先加延迟：
    - 这个 overlay 已经同时依赖 `UseSharedBackstop => true`
    - 并且会在 `BuildStaticUi()` 里挂一张全屏 `Background` TextureRect
    - 先核对 `AfterOverlayOpened/Shown/Hidden/Closed` 期间是否被别的 UI rebuild 或 visibility 链打断，再决定要不要碰动画/布局
  - 如果需求是“玩家选择后遗物下面要出现对应头像，像宝箱房那样”，默认优先复用 `NTreasureRoomRelicHolder.VoteContainer` 这条现成显示链：
    - 先让 holder 本身维护正确的“哪个玩家投了哪个 relic”
    - 再让 `PlayerVotedForRelic` / `RefreshPlayerVotes` 去负责头像显示
    - 不要先自定义再叠一层手写头像 UI，否则容易和原 vote 容器、联机刷新、结果动画互相打架
  - 如果需求是“鼠标放在人格上要正常出 relic 信息”，优先把 hover 绑在 holder/control 本体上，并直接复用 `relic.HoverTips`：
    - 参考仓库里 `GroupedRelicSelectionScreen`、`RefreshableTokenRelicSelectionScreen`
    - 用 `NHoverTipSet.CreateAndShow(owner, relic.HoverTips, HoverTip.GetHoverTipAlignment(owner))`
    - 不要只给 icon 子节点绑 hover，否则焦点、手柄和整块点击区域很容易不一致
  - 如果需求是“所有人都选完后才触发结算”，先查提交链是否被 UI 症状误判：
    - `StartingPersonRelicSelectionScreen.CollectSelectionsAsync()` 的多人路径应等 `_multiplayerCommitSource`
    - `MaybeCommitSelections()` 只应由 authority / host 在 `AreAllSelectionsSubmitted()` 为真后发最终 commit
    - 如果表面看起来像“提前结算”，先核对是谁提前写入了 committed snapshot，不要先怀疑结果动画本身
  - 起始人格联机稳定性默认按“两段式”检查：
    - 第一段是 ready-page / host-launch payload 是否一致
    - 第二段是 overlay 内 `selection update -> final commit -> ApplyCommittedSelections(...)` 是否只走一遍
    - 如果用户描述是“显示不全 / 有人先结算 / 有人没看到头像”，先分清是 payload 分叉、vote 容器没刷，还是 final commit 提前触发
  - 对人格选择链，`Asset not cached` 往往只是噪音；若同时存在 `PlayerVotedForRelic` / `Index was out of range` / `P132A`，默认先修越界链，再讨论资源
  - 只要用户没有明确要求动这条链，默认避免顺手“顺便整理 / 顺便统一 / 顺便优化”：
    - 先怀疑是否能在外围绕开
    - 先怀疑是否能只补单点 helper / guard / payload 透传
    - 不要因为看起来命名乱、职责散，就立刻做结构性清理
  - 如果用户已经撤回某次人格选择相关修改，后续默认先按“撤回后的现状”重新取证；不要把已撤回方案当成既成前提继续推进
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
- 把仓库里的玩法设置或功能面板拆到独立模组时，也继续按这 4 层做迁移清单；不要因为新模组里面板已经能显示，就判定迁移完成。
- 尤其是原本带 host-only 编辑门禁的面板，必须先迁：
  - 房主角色识别
  - lobby 观察与注册
  - host 初始 snapshot 建立
  - 点击后的保存回写
- 否则常见结果就是：独立模组面板能挂上去，但 `OnTogglePressed(...)` 一直因为角色未就绪或 snapshot 未建立而 silent no-op。
- 只改其中一层通常不够；如果用户说“增加玩法设置”或“房间同步”，默认一起检查：
  - `ReAstralPartyModSettings`
  - 设置页注册
  - `CharacterSelectGameplayPreviewPatch`
  - `LobbyGameplaySettingsSync`
  - `ReAstralPartyRunSettingsSync`
  - `ReAstralPartyModSettingsManager` getter
- 对独立拆分后的验收，不要停在“build 成功”或“面板看得见”；默认额外确认：
  - 独立模组自己的 `settings.json` 点击后确实变化
  - 房主与客户端看到同一份 lobby / run snapshot
  - 游戏目录里实际加载的是本轮 DLL / PCK
  - 原 `ReAstralPartyMod` 已不再注入旧面板、旧 patch、旧资源路径
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
  - 如果默认 ban 看起来“代码已经加了，但 UI 和运行时都没命中”，默认优先怀疑 3 件事：
    - 用错了 relic ID
    - 迁移只改了内存，没有真正 `Save(...)` 落盘
    - UI 列表来源里根本没把目标内容纳入分类
  - 对旧设置补默认 ban 时，默认做一次性迁移而不是每次启动强塞：
    - 只在“尚未初始化默认 ban”的旧配置上补入默认值
    - 迁移后立刻落盘
    - 用户后续手动取消 ban 后，要尊重用户配置，不要每次启动再自动加回
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
  - 这类问题一旦出现“UI 里看得到内容，但默认 ban 命不中”，优先怀疑默认 ban 存的是错误短 ID，而 UI / 候选池比较用的是完整 canonical ID
- 如果要禁止普通商店自然刷某类遗物，优先“替换 entry”为同类型可用替代项，不要直接删除 entry，避免商店正常位数量变少。
- 特殊商人 / fake merchant 仍然是高风险链：
  - 优先在 `NMerchantInventory.Initialize(...)` 前缀整体排除 `NFakeMerchantInventory`
  - 不要只在 UI clone 或单个 helper 里晚期兜底
- 当同一功能同时改库存数据和商店 UI 时，默认先改正常库存，再补额外 slots，避免把追加位误当成普通库存过滤掉。
- ban relic 机制默认不是底层全局强封禁，而是“各个来源主动读取 ban 集”：
  - 如果需求只是“默认不让某个商店/卡池/候选池刷出某件 relic”，优先修对应来源入口 + 默认 ban 落盘链
  - 如果需求是“所有来源彻底禁用某件 relic”，需要逐入口审计，不要假设 ban UI 自动全局生效
  - 对月球遗物相关问题，默认先复用现有 ban 框架和候选池过滤，不要为了单个 relic 临时写一条 if/黑名单旁路：
    - 先修默认 ban 初始化
    - 再修真实 ID 命中
    - 再确认 `MoonPropShopExtraRelicsHelper` / deterministic helper 真在读取同一份 ban 集
    - 只有框架已经确认无解，才考虑 feature-local 特判

### 构建 / 导出

- 对 `B:\Documents\re-astral-party-mod`，默认“构建导出”就是直接更新游戏目录：
  - `D:\Steam\steamapps\common\Slay the Spire 2\mods\ReAstralPartyMod\`
- 不要默认再额外导出到临时验证目录、仓库自定义输出目录或其它多余位置。
- 只有当游戏目录文件被占用、Godot 导出失败、或用户明确允许 compile-only fallback 时，才退回临时验证产物。
- 如果只是 C# 改动且资源未变，优先确认 DLL 已更新到游戏目录；不要把“compile-only 产物生成成功”当成“已经导出”。
- 对这类联机 / 启动链修复，构建后要额外核对游戏目录与仓库编译产物的时间戳或大小是否真的更新：
  - `B:\Documents\re-astral-party-mod\.godot\mono\temp\bin\Debug\ReAstralPartyMod.dll`
  - `D:\Steam\steamapps\common\Slay the Spire 2\mods\ReAstralPartyMod\ReAstralPartyMod.dll`
  - 如果游戏目录仍是旧 DLL，就不要用新日志之外的现象来评价本轮修复是否生效

### FinalArena 人机对决

- 【人机对决】的固定语义是：每位玩家先获得【电击枪】，再让该实际持有遗物的记录心智 `+1`；电击枪计数器就是记录心智，禁止再用“初始层数至少为 1”的常量补偿替代这条链。
- 发放必须走 `PersonMultiplayerEffectHelper.ObtainRelicDeterministic(...)`，随后用 `player.GetRelic<TokenExclusiveStunGun>()` 重新取得实际实例；不得对 `RelicCmd.Obtain(...)` 返回值做强转后直接作为成功依据。
- 只有 `StunGunMindSyncHelper.AdjustRecordedMindStacks(..., +1, ...)` 使层数从 `N` 变为 `N+1` 后，才能记录该玩家已完成。遗物取得失败、实例未解析或层数未改变时，不能写全局完成状态，后续初始化必须可重试。
- `FinalArenaSeriesRunData` 与状态包对这条效果必须按 `Player.NetId` 保存/同步“已完成玩家”和 host 确认的最终心智层数；client 收到后用 `SetRecordedMindStacks(...)` 幂等收敛到目标值，不能以“层数至少为 1”猜测是否已执行。
- 旧存档若只有历史全局完成位：已有电击枪且记录心智大于 `0` 的玩家视为已按旧补偿完成；无电击枪或层数为 `0` 的玩家必须取消完成推定并补发。

### 隐藏运行时载体

- 这个仓库里，若某个玩法用隐藏 modifier 或等价运行时组件更稳定，可以采用，但默认要求：
  - 不显示图标
  - 不进常规 modifier UI
  - 不进结算展示
- 这条尤其适用于清醒梦、梦境模式、只用于保存本局状态的隐藏效果载体；可见 UI 与运行时持有层应分开设计。
- 但是普通 modifier 会影响 Neow/开局流程。需要 run-scoped 全局战斗效果时，优先用 `RunSavedData` 保存状态，再用具体 hook/patch 在战斗、房间或奖励入口读取；只有确认时机已晚于 Neow 普通选项生成，才允许把隐藏 modifier 装进 `RunState.Modifiers`。

### Decimal 边界

- `decimal` 只用于运行时计算和公式推导。
- 一旦跨到存档、联机同步、奖励状态、计数器显示、HP 快照、阈值里程碑等边界，必须转成稳定标量形态。
- 优先复用 `ReAstralPartyCardCode\Utils\StableNumericStateHelper.cs`，不要在业务文件里散落重复的取整和序列化逻辑。
- 不要给 `[SavedProperty]` 直接挂 `decimal`。
- 不要把 `decimal` 直接作为需要持久化或奖励同步的字段形态。
- 过程允许保留小数，但最终一旦落到“伤害/治疗/格挡/计数奖励”这类整数边界，默认先明确收口规则，再统一走 helper。
- 像“每 3 层 +2”“每 5 点 +1”这类阈值文案，默认先把“满多少组”算成稳定整数，再乘单组收益；不要把倍率直接乘在原始层数上再试图事后兜底。

### `019e56fb...` 相关仓库经验

- 对“某张具体卡进入某个牌堆后触发”的行为，默认优先考虑 `AstralPartyCardModel.AfterCardChangedPiles(...)` 这种窄 hook，而不是先上全局 patch。
- 先澄清效果语义，再决定是复用现有概念层还是新造一层。对本仓库，像 Omen 这种已有壳层，默认优先扩展现有 `*Base`，不要新造平行概念。
- 生成卡牌结果优先走仓库现成的多人局安全 helper，不要直接绕开通知链。

### 延后跨玩家牌组事务与归零补牌

- 跨玩家永久牌组变更绝不能在战斗中直接改 `Deck`、`Owner` 或当前战斗牌堆副本。选牌阶段只保存稳定快照与顺序；实际 `LoadCard -> 正式入目标 Deck -> 正式移除来源 Deck` 必须在战斗后同一条 RitsuLib 托管动作队列中执行。
- “每次打出最多选四张”表示每次打出建立一个独立批次；同回合再次打出必须建立第二批，不能按玩家或战斗压成唯一批次。每批固定顺序、单一 payload、整批幂等，不能拆成每张一个异步网络动作。
- 不要把延后定位 ID 写进所有来源卡的 attached state，也不能为查询而写入空持久字段。来源定位应由批次快照、稳定索引及当前序列化内容共同验证；目标实例的可见/持久状态只在正式创建后写入。
- 永久入牌和移牌必须只走官方 `RunState.LoadCard`、`CardPileCmd.Add`、`CardPileCmd.RemoveFromDeck` 或仓库正式 helper；不直接改集合、不额外 `RunState.RemoveCard`。移除是否完成以指定 `Deck` 归属与 `HasBeenRemovedFromState` 判定，`RunState.ContainsCard` 仍为真是原版可接受状态。
- 目标入牌必须复用正式入运行牌组 helper，并在实际获得者本地端启用预览。`showPreview: true`、原版 `CardPileCmd.Add`、已见卡登记和 owner/deck 校验是一条链，不能为了同步绕掉动画和数量刷新。
- 对战斗结束后的持久牌组结果，pending batch 只能在“目标存在或已成功创建、来源已按官方命令移除、后续替换成功”后删除。异常或中途取消保留 batch 重试；目标已存在时只补来源移除，已完整完成时只记录 `already applied`，不得重复奖励。
- “牌组归零后替换固定牌组”必须在正式 `RemoveFromDeck` 返回的任务完成后重新读取真实 Deck，不能只依赖移除前事件预测。战斗外归零与强者怜悯战后归零需共用幂等 `Ensure...Deck`，但同一来源玩家存在未完成转移 batch 时，通用监听必须让位给该 batch，避免双重补牌。
- 固定替换牌组每张也必须走正式入牌与本地预览，并逐项补齐、严格验证最终组成；只要有一张失败就保留待处理状态。完成标记只能说明曾成功完成，不能阻止未来再次归零后的替换。
- 对这类联机事务，日志至少记录 batch/transfer ID、动作请求/执行、源和目标 Deck 数、`in_deck`、`has_been_removed_from_state`、预览是否显示，以及 `applied` / `already applied` / `retry`。验收必须用新局的主客日志，而非已分叉存档。

### 卡牌文本、关键词与交互 UI

- 用户给定的技能文本是玩法规格的一部分。除非明确要求扩写，不得把内部能力拆成玩家可见的重复句子；先实现，再用用户给定的简洁表述精确落到三语本地化。
- 自定义关键词若要求彩色/金色显示，注册位置、卡牌正文注入和 hover tip 必须同时验收。文本中的富文本颜色标签必须按本仓库本地化格式写入，不能只注册 keyword 而漏掉实际渲染颜色。
- “从手牌离开”类规则以唯一 `Hand -> 非 Hand` 牌堆变化为门禁。打出应等待本体结算后再处理，弃置/直接消耗则立即处理；非手牌之间移动绝不重复触发。星光等 power 必须走官方 `PowerCmd.Apply`，不要拿其它 token helper 冒充普通能力结算。
- 右键卡牌、动态手牌描边等交互优先使用当前 RitsuLib 注册器。手牌描边规则应注册在 `CardModel` 基类并显式覆盖不可打出可见性；条件只检查目标关键词，避免漏掉原版或其它模组被附加关键词的卡。
- 专用遗物选择 overlay 需要按视口和候选数等比缩放卡片、图标、文字和间距，并在尺寸变化时重新布局。候选顺序和同步索引必须保持稳定；这类适配不得外溢到人格选择或通用选择屏。

### 跨战斗计数与内容完整性

- “全局计数”要保存累计总数，而不是仅保存取模阶段。初始为 `0`；显示阶段由总数导出，例如 `0 -> 1 -> 2 -> 3 -> 1`。所有依赖同一“每第三张”的复制、冷却和数值奖励必须读取同一累计值，并在实际成功结算后统一刷新。
- 回合内上限与跨战斗累计计数是两条独立状态：前者只在回合开始重置，后者跨战斗、房间、读档保留。不要让获得遗物本身伪造一次出牌。
- 新卡/新遗物及品质或 ID 调整必须同步审计：类名、canonical ID、注册表、资源和 `.import`、项目引用、三语本地化、图鉴/池分类和静态文档。资源命名统一 ASCII；新 ID 不做迁移时要明确旧存档边界。
- 任何“战斗中选择、战斗结束后发放”的遗物或人格奖励，优先复用原版 `FORBIDDEN_GRIMOIRE` 式奖励栏。选择只记录确定性 pending；奖励由原版领取链发放，不能在 `AfterCombatEnd` 直接改遗物栏。

### 泰拉棱镜 / 视觉模型与玩法模型分离

- 复用原版 VFX 不等于复用原版玩法卡牌。若需求只借用君王之剑的模型，实际战斗卡必须是独立 `CardModel`；否则 hover 标题、`OnPlay`、铸造、寻锋和格挡等原版逻辑会一起被继承。
- 原版 VFX 的 `Card` 决定主 hover 身份；capability 追加 hover 只能增加来源卡提示，不能覆盖主标题。因此应把独立泰拉棱镜卡传给 `NSovereignBladeVfx.Create(...)`，再手动挂载 VFX。
- 自动生成并长期挂载的战斗卡必须进入正式自定义战斗牌堆，并通过 `CardPileCmd.Add` / `CardCmd.AutoPlay` 往返；不要依赖 `PileType.None`、脱离牌堆的卡或手写集合。
- 首张攻击拦截只能保留一个前置入口。若 `BeforeCardPlayed` 先登记后，`CardModel.OnPlayWrapper` 再检查“尚未登记”，会导致直接拦截失效，原攻击效果先执行。
- 伤害类遗物必须启用 `ShouldReceiveCombatHooks`；倍率应按 `dealer`、`cardSource.Owner` 和 `cardSource.Type` 收窄，并显式排除独立自动卡，避免棱镜自己的原始面板伤害再次被削弱。
- 构建成功不能证明运行时链路成功；必须同时检查实际加载的 DLL/PCK、VFX 的 `Card` 身份、挂载牌堆数量、自动播放和回收日志。
- 任何让玩家查看、计算或悬停的生成卡，都必须把可见卡面作为玩法完成的一部分验收：标题、卡图、`DamageVar`、描述占位符和 hover 不能留到“后续再补”。
- 装载/快照伤害只保留一个权威值：装载成功时，同一步把来源牌的原始面板伤害写入 capability 与独立卡的 `DynamicVars.Damage.BaseValue`；卡面 `{Damage:diff()}` 和实际 `DamageCmd` 都读取这份值，禁止显示与结算各算一套。
- 独立卡继承的默认资源路径通常不存在。若要复用原版卡图，必须在 `AssetProfile` 显式填写已验证的原版普通和 Beta `res://` 路径；不能因为借用了原版 VFX 就假设卡面会自动继承。
- VFX hover 的主卡来自 `Vfx.Card`，来源牌只能作为 `AdditionalHoverTips` 追加。capability 必须在创建 VFX 前初始化；节点实际进入场景树后才刷新原版 hover 链。
- 拦截一张原牌以改为装载时，只有独立卡已成功加入正式战斗牌堆后才跳过原牌效果。任一步失败必须以防递归门禁调用原版 `OnPlayWrapper(...)` 恰好一次，不能吞卡、重复结算或把牌留在出牌堆。
- 自动卡的 `PileType.Play` 移动职责必须先按当前版本反编译 `CardCmd.AutoPlay` 与 `CardModel.OnPlayWrapper` 确认。两者中已有一方负责入出牌堆时，不要再手动重复 `Add(... Play ...)`；每个自动卡只允许一条正式的入牌、结算和结果牌堆回收链。
- 这类功能提交前的最低实测不是“模型出现”：必须分别验证卡面伤害、实际伤害、原版卡图、主 hover 标题、来源卡 tip、自动打出、回收，以及满手牌和多把挂载的顺序表现；未实测时不得表述为已修复。

## When To Use This Overlay

- 目标仓库是 `B:\Documents\re-astral-party-mod`
- 任务涉及本仓库自己的遗物、卡牌、power、人格、起始流程、月球体系、隐藏资源链
- 症状包含：
  - UI 显示英文 / 原始 ID
  - hover / 查看时本地化异常
  - 战斗中卡死、无发牌、无 intent、联机分叉
  - 起始人格或 Neow 链状态错乱
  - 资源明明存在但运行时不显示
