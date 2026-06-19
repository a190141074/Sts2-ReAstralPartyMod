# Timing Map

这份索引不是 API 抄录，而是“遇到时机选择问题时先怎么想”。

## 游戏本体 Hook / 模型能力面

### `BeforeCombatStart()`

- 适合：战斗开始时的一次性状态注入、开场 buff、开场生成物。
- 比邻近时机更适合的原因：通常比 turn-start 更早，能让本场后续逻辑都看到这个初始状态。
- 风险：
  - 太早依赖别的单位开场状态时，可能需要更晚入口。
  - 如果会临时插入新怪或新对象，要检查这些对象是否还需要补吃开场 hook。

### `BeforeCombatStartLate()`

- 适合：需要在常规 `BeforeCombatStart()` 之后，再补一次“更晚开场初始化”的场景。
- 默认策略：先确认当前 host / 当前模型类型是否真有这个入口；它不是 RitsuLib lifecycle 目前直接发布的通用事件。
- 风险：不要把它当成默认首选；只有确认需要晚于普通开场初始化时再用。

### `BeforeSideTurnStart(...)`

- 适合：整个 side 的回合开始共通处理，例如“我方回合开始先准备状态”。
- 比 `AfterPlayerTurnStart(...)` 更适合的原因：这是 side 级别，不是单个玩家自己的开始。
- 风险：如果需求只服务拥有者本人，挂这里通常过宽。

### `AfterSideTurnStart(...)`

- 适合：需要等 side turn 已经开始、但仍想在该 side 的早期阶段结算的效果。
- 风险：同样是 side 级时机，默认比玩家级入口更宽。

### `AfterPlayerTurnStart(...)`

- 适合：明确写着“你的回合开始时”“持有者自己的回合开始时”的效果。
- 比 side 级入口更适合的原因：天然更窄，误伤别的玩家/单位的概率更低。
- 风险：
  - 如果遍历单位并可能造成击杀/移除，优先快照集合。
  - 若效果本质上是 side 共通，不要硬塞成玩家级。

### `AfterTurnEnd(...)`

- 适合：回合结束收尾、衰减、计数推进、清理状态。
- 风险：先分清是“持有者自己的回合结束”还是“某个 side 的回合结束”。
- 如果是直接 patch `Hook.AfterTurnEnd`，不要想当然按旧的双参版本写；当前新版分支可能带额外结束单位参数，先对照 `sts2.xml` 或 RitsuLib lifecycle patch 的 target 列表确认。

### `AfterCardChangedPiles(...)`

- 适合：某张具体卡进入某个牌堆、离开某个牌堆的窄行为。
- 比全局 patch / 宽牌堆观察更适合的原因：目标明确，最适合“这张卡进了弃牌/消耗/手牌后触发”的效果。
- 风险：先用 `card == this`、`oldPileType`、当前 `Pile?.Type` 把条件收窄。

### `ModifyDamageAdditive(...)`

- 适合：基础伤害加减，尤其是“提高/降低基础伤害”。
- 默认问法：这条效果改的是基础值，还是最终乘区。
- 风险：不要把明确写“最终伤害”的效果塞进加法面。

### `ModifyDamageMultiplicative(...)`

- 适合：最终伤害乘区，或文案明确写“最终伤害”的效果。
- 风险：不要用它去实现本应是基础伤害变更的月球/遗物数值。

### `ModifyHpLostBeforeOsty*`

- 适合：受伤前拦截、承伤转移、伤害放大/减免、召唤承伤相关逻辑。
- 风险：先分清要改的是受到的原始伤害、未被格挡伤害，还是召唤承伤后的结果。

### `TryModifyEnergyCostInCombat(...)`

- 适合：战斗内费用调整。
- 比直接改卡牌费用字段更稳的原因：更接近结算口径，适合“仅战斗内减费/增费”。

### `TryModifyRestSiteOptions(...)`

- 适合：休息点选项增删改、额外锻造次数、禁用/扩展某类选项。
- 风险：这是休息点级入口，不是回血/奖励入口。

### `TryModifyRewardsLate(...)`

- 适合：奖励栏最后阶段删改 reward，例如移除 `GoldReward`、追加额外奖励。
- 风险：先确认目标真的是 reward list，不要混成直接发金币/发遗物。

## RitsuLib 生命周期 / 事件层

### 什么时候优先 lifecycle event

- 需求是横切观察或订阅，不需要某个具体模型自己拥有 override。
- 需求更像“框架级监听时机”，而不是“这张卡 / 这件 relic 自己触发”。
- 你不想自己上 Harmony patch，而 RitsuLib 已经发布了合适事件。

### 什么时候优先模型 override

- 行为只服务某个具体 card / relic / power / creature。
- 当前仓库已经有成熟 base/helper 能承接。
- 你需要最窄、最不容易误伤的入口。

### 常用 lifecycle 事件

#### `CombatStartingEvent`

- 对应底层 `Hook.BeforeCombatStart`
- 适合：框架级战斗开始监听

#### `SideTurnStartingEvent`

- 对应底层 `Hook.BeforeSideTurnStart`
- 适合：side 级开始观察

#### `SideTurnStartedEvent`

- 对应底层 `Hook.AfterSideTurnStart`
- 适合：需要观察“side turn 已经开始”

#### `CardMovedBetweenPilesEvent`

- 对应底层 `Hook.AfterCardChangedPiles`
- 适合：全局牌堆迁移观察
- 如果只是某张卡自己的效果，通常优先 `AfterCardChangedPiles(...)`

#### `CardExhaustedEvent`

- 适合：你只关心“有卡被消耗了”，不需要自己判断牌堆变化细节

#### `BeforeFlushEvent`

- 适合：flush 前观察

#### `CardsFlushedEvent`

- 适合：新 host API 下同时观察 flushed / retained 结果
- 比旧的 `CardRetainedEvent` 更符合当前版本兼容口径

## 默认决策 checklist

1. 这是单模型行为，还是框架级观察
2. 这是玩家自己的回合，还是整个 side
3. 这是单卡进出牌堆，还是更宽的战斗时机
4. 这是基础伤害面，还是最终乘区
5. 是否会影响多人局同步、RNG 或集合遍历
6. 当前仓库是否已有更窄的 base/helper

如果当前仓库有自己的 overlay skill，再继续读仓库层 `timing-map.md`，看 repo 已验证的默认优先入口。
