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
- 发现一种会反复出现的 repo-specific 失败模式时，默认要把它沉淀回这个 overlay，而不是继续留在临时对话里。

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

### 新异格 / 新人格 / 成套内容

- 不能只看抽象基类就开写，必须先找至少一个仓库内已正常工作的同类成品。
- 对 `VariantPerson*`、`Person*`、配套 skill card、配套 power，一律整套核对，不只检查 relic。
- 对 `CooldownPersonaRelicBase` 这类冷却型人格，以及仓库内同口径的冷却型异格，默认把配套 skill card 配成：
  - `ShouldAutoApplyCooldownEnchantment => true`
  - `CanonicalKeywords` 包含 `AstralUnique`
  - 除非用户明确要求关闭冷却附魔、移除唯一，或另行指定别的词缀组合。
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

### 战斗 / 联机 / 日志优先

- 对联机或战斗异常，先抓最早的硬错误，再区分是否还伴随 checksum divergence。
- 不要拿分叉症状代替根因。
- 战斗 hook 里只要遍历目标集合时可能造成击杀、移除、融化、power 移除、队列改写，就优先怀疑 live collection mutation。
- 默认先快照再结算，不要边遍历边调用可能改集合的命令。

### 新版兼容 / patch 漂移

- 当前仓库做新版兼容时，先清 loader 级红字，再清 gameplay patch 红字；不要先被下游 UI 症状带跑。
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
