# AstralPartyMod/星引擎patryMOD

`AstralPartyMod` 是一个基于 `BaseLib` 的《杀戮尖塔 2》模组工程，用于扩展卡牌、事件、遗物与能力内容。

**使用ai 进行开发。**

## 模组内容

### 事件卡

| 名称 | 类名 | 效果                                                         |
| --- | --- | --- |
| 天使降临 | `EventAngelsDescent` | 所有单位回复生命。 |
| 拥挤通道 | `EventCrowdedPassage` | 所有玩家获得缓冲与易伤。 |
| 天降神兵 | `EventDeusExMachina` | 从其他事件牌中选择 1 张，发动 2 次。 |
| 人人平等 | `EventEquality` | 所有玩家生命值降为 1，并获得格挡。 |
| 战斗，爽！ | `EventFightFun` | 所有玩家获得一张巨石+。 |
| 食品安全 | `EventFoodSafety` | 所有单位获得中毒。 |
| 天降之物 | `EventGiftFromSky` | 所有玩家抽牌并获得星光。 |
| 手牌抹除 | `EventHandErase` | 所有玩家弃置手牌中最右边的一张牌。 |
| 玩家代表 | `EventPlayerRepresentative` | 进行一次判定，不同结果会让自己与队友分别受到伤害或获得星光。 |
| 红温警告 | `EventRedHeatWarning` | 所有玩家获得火力。 |
| 疾跑 | `EventSprint` | 所有玩家获得敏捷。 |
| 天打雷劈 | `EventThunderStrike` | 所有单位受到伤害。 |

### 技能卡

| 名称 | 类名 | 效果 | 角色 |
| --- | --- | --- | --- |
| 名刀 | `SkillFamousBlade` | 造成伤害，至多消耗 2 层剑气；每累计消耗 2 层剑气，本局游戏中此牌永久增加 1 点伤害；使用后获得 1 层剑气。 | 【人格：蒸蛋】 |
| 占位牌 | `SkillStarShop` | 未实现 | 【人格：老板娘】 |
| 麻烦制造者 | `SkillTroubleMaker` | 展示 2 张随机其他事件牌，选择 1 张立刻发动，并获得星光。 | 【人格：太刀虾】 |

### 遗物

| 名称 | 类名 | 效果 |
| --- | --- | --- |
| 【人格：蒸蛋】 | `PersonWeirdEgg` | 每 3 回合结束时，将 1 张麻烦制造者加入手牌。 |
| 【人格：太刀虾】 | `PersonSamuraiPrawn` | 每 3 回合结束时，将 1 张名刀加入手牌；每回合你第一次攻击敌人时，获得 1 层剑气，最多 3 层。 |

### 能力

| 中文牌名 | 类名 | 中文效果 |
| --- | --- | --- |
| 星光 | `StarLightPower` | 本场战斗结束后，获得等同于星光层数的金币。 |
| 剑气 | `SwordAuraPower` | 每层使你的攻击额外造成 1 点伤害；第 3 层额外造成 2 点伤害；最多 3 层。 |

## 依赖

- `BaseLib`
