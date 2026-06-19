# ReAstralParty Timing Map

这份索引按三层组织：

1. 游戏本体 Hook / 模型能力面
2. RitsuLib lifecycle 事件层
3. 本仓库默认优先入口

## 游戏本体时机

### `BeforeCombatStart()`

- 适合：开场一次性状态注入、开场 buff、开场生成物。
- 本仓库常见用途：人格开场状态、开场护甲/召唤、战斗起始标记。
- 风险：如果后面还会额外插入怪物/对象，要检查这些新增对象是否需要补吃开场 hook。

### `BeforeCombatStartLate()`

- 适合：明确需要晚于普通开场初始化的补跑场景。
- 当前仓库已验证用途：`MoonPropBeadsOfFealty` 给额外插入的精英怪补跑 `BeforeCombatStartLate()`。
- 默认策略：不是首选入口，只在确实需要“比普通开场更晚”的语义时用。

### `BeforeSideTurnStart(...)`

- 适合：整个 side 的开始阶段，例如人格冷却牌准备、side 级预处理。
- 比 `AfterPlayerTurnStart(...)` 更合适的条件：效果目标是 side，不是某个玩家自己。

### `AfterSideTurnStart(...)`

- 适合：side turn 已开始，但仍属于该 side 早期阶段的统一处理。
- 风险：同样偏宽，默认比玩家级入口更容易误伤。

### `AfterPlayerTurnStart(...)`

- 适合：文案明确写“你的回合开始时”“持有者自己的回合开始时”。
- 本仓库默认：只要不是 side 级共通效果，先考虑这里。
- 风险：如果会遍历并伤害多个单位，优先快照集合。

### `AfterTurnEnd(...)`

- 适合：回合结束收尾、衰减、计数推进、清理状态。
- 默认先分清这是玩家自己的结束，还是整个 side 的结束。
- 如果是直接 patch `Hook.AfterTurnEnd`，当前新版分支不要默认按旧双参 target 写；先对照运行时 `sts2.xml` 或 RitsuLib lifecycle patch 已验证的 target 形状。

### `AfterCardChangedPiles(...)`

- 适合：某张具体卡进入某个牌堆后触发。
- 本仓库已验证：Enigmatic 那组诅咒进 Exhaust 后挂 Omen，优先走这个窄 hook。
- 默认写法：先用 `card == this`、`oldPileType`、`Pile?.Type` 把条件收窄。

### `ModifyDamageAdditive(...)`

- 适合：基础伤害加减。
- 默认问法：这条效果改的是基础值，还是最终伤害。

### `ModifyDamageMultiplicative(...)`

- 适合：最终伤害乘区。
- 只有文案明确写“最终伤害”时，才优先考虑这层。

### `ModifyHpLostBeforeOsty*`

- 适合：受伤前拦截、承伤转移、召唤承伤、受伤倍率。
- 风险：先分清要改的是受伤前数值，还是后置结果。

### `TryModifyEnergyCostInCombat(...)`

- 适合：战斗内费用变化。
- 默认优先于直接改卡牌费用字段。

### `TryModifyRestSiteOptions(...)`

- 适合：休息点选项增删改、额外锻造次数等。

### `TryModifyRewardsLate(...)`

- 适合：奖励栏尾段增删 reward。
- 典型用途：移除 `GoldReward`、追加额外奖励。

## RitsuLib 生命周期层

### 默认原则

- 只要目标是某个具体模型自己的窄行为，优先模型 override。
- 只要目标是横切观察 / 订阅，而不是模型自持逻辑，再考虑 lifecycle event。

### 常用事件

#### `CombatStartingEvent`

- 对应底层 `Hook.BeforeCombatStart`
- 适合：战斗开始的框架级监听

#### `SideTurnStartingEvent`

- 对应底层 `Hook.BeforeSideTurnStart`
- 适合：side turn 开始前的观察

#### `SideTurnStartedEvent`

- 对应底层 `Hook.AfterSideTurnStart`
- 适合：side turn 已经开始后的观察

#### `CardMovedBetweenPilesEvent`

- 对应底层 `Hook.AfterCardChangedPiles`
- 适合：全局观察牌堆迁移
- 如果只是单张卡自己的效果，优先 `AfterCardChangedPiles(...)`

#### `CardExhaustedEvent`

- 适合：只关心“有卡被消耗了”的全局观察

#### `BeforeFlushEvent`

- 适合：flush 前观察

#### `CardsFlushedEvent`

- 适合：同时观察 flushed / retained 结果
- 比旧 `CardRetainedEvent` 更符合当前版本兼容口径

## 本仓库默认优先入口

### 卡牌入特定牌堆

- 默认先看：`AstralPartyCardModel.AfterCardChangedPiles(...)`
- 适合：某张卡进 Hand / Discard / Exhaust 后触发
- 对照：
  - `EnigmaticTheAcknowledgment`
  - `EnigmaticTheTwist`
  - `EnigmaticTheInfinitum`

### 人格 / token 遗物桥接

- 默认先看：
  - `TokenRelicBridgeHelper`
  - `TokenRelicBridgePower`
- 适合：把 token relic 的战斗时机桥接到 power / 受击对象上
- 比直接再补一套 patch 更稳的原因：仓库已经限定了允许转发的 override 面。

### 冷却人格发牌

- 默认先看：`CooldownPersonaRelicBase`
- 关键入口：
  - `BeforeSideTurnStart(...)`
  - `AfterTurnEnd(...)`
- 适合：按回合推进计数、在满足冷却时机时发牌

### 常规“玩家自己回合开始”

- 默认先看：`AfterPlayerTurnStart(...)`
- 适合：持有者自己的 turn-start 触发
- 风险：遍历并伤害多个单位时先快照

### 常规“整 side turn 开始”

- 默认先看：
  - `BeforeSideTurnStart(...)`
  - `AfterSideTurnStart(...)`
- 适合：side 级预处理或 side 级开场状态

### 战斗开始一次性状态注入

- 默认先看：`BeforeCombatStart()`
- 适合：开场 buff、开场生成、开场资源注入
- 如果是后插入单位需要补吃开场状态，再额外考虑补跑 `BeforeCombatStartLate()`

## 默认决策 checklist

1. 这是玩家自己的时机，还是 side 级时机
2. 这是单模型窄行为，还是横切观察
3. 这是基础伤害，还是最终乘区
4. 是否会影响多人局同步、RNG、集合遍历
5. 当前仓库是否已有更窄的 base/helper
6. 当前仓库是否已有已工作的同类成品可直接对照
