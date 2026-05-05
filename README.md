<div align="center">
<img style="width: 256px; height: auto; border-radius: 12px;" src="icon.svg" alt="ReAstralPartyMod"/>
<h1>ReAstralPartyMod</h1>
<div style="display: flex; flex-direction: row; justify-content: center; flex-wrap: wrap; gap: 6px;">
<img src="https://img.shields.io/badge/-C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#"/>
<img src="https://img.shields.io/badge/-.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET"/>
<img src="https://img.shields.io/badge/-Godot-478CBF?style=for-the-badge&logo=godotengine&logoColor=white" alt="Godot"/>
<img src="https://img.shields.io/badge/-Slay%20the%20Spire%202-8B0000?style=for-the-badge&logoColor=white" alt="Slay the Spire 2"/>
<a href="https://github.com/BAKAOLC/STS2-RitsuLib"><img src="https://img.shields.io/badge/-STS2--RitsuLib-5538DD?style=for-the-badge&logo=github&logoColor=white" alt="STS2-RitsuLib"/></a>
<img src="https://img.shields.io/badge/version-0.1.8-ffaf50?style=for-the-badge" alt="Version"/>
</div>
<div><b>简体中文</b></div>
</div>

`ReAstralPartyMod` 是一个以 **STS2-RitsuLib** 为前置的《杀戮尖塔 2》模组。当前版本围绕“人格遗物 + 筹码遗物 + 事件卡”三条主线构建玩法，把原先散落在旧工程里的角色和系统整合进同一个可运行项目中。

这个 README 以当前仓库代码和 `zhs` 本地化为准生成，面向中文版玩家阅读。文档中的颜色标签、关键字标签和游戏内富文本标记已被移除，避免 Markdown 中混入游戏内专用标签。

---

## 模组概览

| 项目 | 内容 |
|------|------|
| Mod Id | `ReAstralPartyMod` |
| 模组名称 | 星引擎MOD |
| 作者 | 雒邪 |
| 版本 | `0.1.8` |
| 前置 | `STS2-RitsuLib` |
| 目标框架 | `net9.0` |
| Godot SDK | `Godot.NET.Sdk 4.5.1` |
| 入口文件 | `Scripts/MainFile.cs` |
| 初始化方式 | `ModInitializer` + `EnsureGodotScriptsRegistered` + `AstralKeywords.RegisterAll()` + `RegisterModAssembly` |

### 内容规模

| 类型 | 数量 |
|------|------|
| 卡牌 | 44 |
| Power | 45 |
| 遗物 | 101 |
| 人格遗物 | 24 |
| 人格衍生遗物 | 6 |
| 筹码与特殊遗物 | 71 |
| 本地化文件 | `cards / powers / relics / potions / modifiers / enchantments / card_keywords / relic_collection / rest_site_ui` |

## 主要玩法系统

### 人格遗物
- 人格遗物是当前模组的核心入口。大多数人格会在战斗中按冷却发放一张专属技能牌，并围绕单独的能力、计数器或衍生遗物展开。
- 项目里已经形成了一套统一的人格模板：冷却类人格主要复用 `CooldownPersonaRelicBase`，展示、发牌、战斗起始和回合推进逻辑都集中在公共基类里。
- 多个人格会在获得时自动附送对应的人格衍生遗物，用来承接整局成长、战斗计数或特殊结算。

### 筹码遗物
- 筹码遗物分成蓝色、紫色、金色和专属四大类，既包含单体增益，也包含套装、系列和联机辅助效果。
- 当前工程保留了“筹码效果桥接为战斗内能力”的路线，便于在不直接动态增删遗物的前提下，把筹码效果临时注入战斗。
- 一部分筹码依赖低血量、标记、星光、治愈、人格技能牌或联机分发事件触发，因此项目里也配套了低血量判定、生成卡归因和奖励同步辅助类。

### 事件卡与战斗临时牌
- `Event*` 和 `Events*` 卡牌主要服务于事件池、人格技能触发和特殊阶段推进。
- `Skill*` 卡牌承担人格技能牌、战斗衍生牌和玩法中枢卡的职责，例如【调制饮料】、【无可阻挡】、【忍术连击】等。

### 多人联机适配
- 当前工程保留了大量联机同步辅助：包括生成卡通知、多人选项确定性处理、起始人格选择同步、奖励同步和人格效果归因。
- README 只描述当前实际启用的内容，不承诺旧存档兼容，也不保留旧 BaseLib 时期的内容 ID 兼容层。

## 卡牌列表

### 基础能力牌

| 类名 | 中文名 | 费用 | 类型 | 稀有度 | 目标 | 效果 |
|------|------|------|------|------|------|------|
| `BaseAbilityOrbitalRailgun` | 轨道炮 | `0` | `技能` | `稀有` | `任意敌人` | 对目标造成{Damage:diff()}点伤害。无视格挡。 |

### 收集器与特殊牌

| 类名 | 中文名 | 费用 | 类型 | 稀有度 | 目标 | 效果 |
|------|------|------|------|------|------|------|
| `CollectorsCardIAmDragon` | 我是龙！ | `1` | `攻击` | `稀有` | `所有敌人` | 对所有敌人造成{Damage:diff()}点伤害，临时减少其{StrengthLoss:diff()}点力量。<br>当你受到伤害时自动打出<br>如果你本回合失去过生命，额外造成{BonusDamage:diff()}点伤害。 |
| `CollectorsCardStagnantProtocol` | 静滞协议 | `2` | `能力` | `稀有` | `自己` | 获得100层静滞协议。 |
| `CuriousCandyMachine` | 怪奇糖果机 | `0` | `技能` | `稀有` | `自己` | 消耗，保留，0费用。<br>使用后扣除12金币，获得随机1种糖果系列药水。 |

### 事件卡

| 类名 | 中文名 | 费用 | 类型 | 稀有度 | 目标 | 效果 |
|------|------|------|------|------|------|------|
| `EventAngelsDescent` | 天使降临 | `0` | `技能` | `稀有` | `所有友方` | 所有单位回复{Heal:diff()}点生命。 |
| `EventCrowdedPassage` | 拥挤通道 | `0` | `技能` | `稀有` | `所有友方` | 所有友方获得{Buffer:diff()}层缓冲和{Vulnerable:diff()}层易伤。 |
| `EventDeusExMachina` | 天降神兵 | `0` | `技能` | `稀有` | `自己` | 选择1张除天降神兵外的事件牌，发动2次。 |
| `EventEquality` | 人人平等 | `0` | `技能` | `稀有` | `所有友方` | 将所有友方的生命值设为1，并获得{Block:diff()}点格挡。 |
| `EventFightFun` | 战斗，爽！ | `0` | `技能` | `稀有` | `所有友方` | 每名队友获得1张巨石。 |
| `EventFoodSafety` | 食品安全 | `0` | `技能` | `稀有` | `所有友方` | 所有单位获得{Poison:diff()}层中毒。 |
| `EventFoodSafetyDoom` | 食品安全 | `0` | `技能` | `稀有` | `所有友方` | 所有单位获得{DoomPower:diff()}层灾厄。 |
| `EventGiftFromSky` | 天降之物 | `0` | `技能` | `稀有` | `所有友方` | 所有友方抽{Cards:diff()}张牌，并获得{StarLight:diff()}点星光。 |
| `EventHandErase` | 手牌抹除 | `0` | `技能` | `稀有` | `所有友方` | 所有友方弃置手牌最右侧的1张牌。 |
| `EventPlayerRepresentative` | 玩家代表 | `0` | `技能` | `稀有` | `自己` | 只有你进行判定。<br>若结果为1-3，你受到{SelfDamage:diff()}点伤害，队友受到{TeamDamage:diff()}点伤害；<br>若结果为4-6，你获得{SelfStarLight:diff()}点星光，队友获得{TeamStarLight:diff()}点星光。 |
| `EventRedHeatWarning` | 红温警告 | `0` | `技能` | `稀有` | `所有友方` | 所有玩家获得{VigorPower:diff()}点活力。 |
| `EventsConcealingInvestigationA` | 隐匿调查·阶段1 | `0` | `技能` | `稀有` | `自己` | 对随机非Boss非精英敌人施加1层标记。<br>邦尼与事件触发者获得隐匿。 |
| `EventsConcealingInvestigationB` | 隐匿调查·阶段2 | `0` | `技能` | `稀有` | `自己` | 对随机非Boss敌人施加1层标记。<br>邦尼与事件触发者各获得1张攻击牌；若其抽牌堆与弃牌堆都没有攻击牌，则改为获得{energyPrefix:energyIcons(1)}。<br>然后双方获得隐匿。 |
| `EventsConcealingInvestigationC` | 隐匿调查·阶段3 | `0` | `技能` | `稀有` | `自己` | 所有单位获得1层标记。<br>邦尼与事件触发者各获得1张攻击牌；若其抽牌堆与弃牌堆都没有攻击牌，则改为获得{energyPrefix:energyIcons(1)}。<br>然后双方获得隐匿。 |
| `EventsConcealingInvestigationD` | 真相揭露 | `0` | `技能` | `稀有` | `自己` | 所有单位获得2层标记。<br>所有队友各获得1张攻击牌；若其抽牌堆与弃牌堆都没有攻击牌，则改为获得{energyPrefix:energyIcons(1)}。<br>然后所有队友获得隐匿。 |
| `EventSprint` | 疾跑 | `0` | `技能` | `稀有` | `所有友方` | 所有玩家获得{Dexterity:diff()}点临时敏捷。 |
| `EventThunderStrike` | 天打雷劈 | `0` | `技能` | `稀有` | `所有友方` | 对所有单位造成{Damage:diff()}点伤害。 |

### 技能牌

| 类名 | 中文名 | 费用 | 类型 | 稀有度 | 目标 | 效果 |
|------|------|------|------|------|------|------|
| `SkillBite` | 撕咬 | `0` | `攻击` | `稀有` | `自己` | 获得1层觉醒。根据你当前的觉醒层数，获得至多4点临时力量。 |
| `SkillChainReaction` | 连锁反应 | `0` | `技能` | `稀有` | `自己` | 使除了自己以外的所有玩家抽1张牌；若其手牌少于4张，则改为抽2张牌。<br>然后将1张撕咬加入你的手中。 |
| `SkillConcealingOperation` | 隐匿行动 | `0` | `技能` | `稀有` | `任意敌人` | 对目标施加调查目标和2层标记。<br>你获得等同于目标当前标记层数×2的星光。<br>详见调查进度。 |
| `SkillDragonsRoar` | 龙之咆哮 | `0` | `攻击` | `稀有` | `任意敌人` | 施加1层易伤和1层虚弱。<br>获得3点临时力量，1层龙吼护体。 |
| `SkillFamousBlade` | 名刀 | `0` | `攻击` | `稀有` | `任意敌人` | 造成{Damage:diff()}点伤害。<br>打出后，至多消耗2层剑气,用于增强剑意。<br>获得1层剑气。 |
| `SkillFateGuidance` | 命运指引 | `0` | `技能` | `稀有` | `自己` | 你拥有的人格遗物冷却-1，并获得{energyPrefix:energyIcons(1)}。 |
| `SkillFateWeakMprint` | 虚弱印记 | `0` | `技能` | `稀有` | `任意敌人` | 使目标获得虚弱印记。 |
| `SkillHealingSlime` | 治愈黏液 | `0` | `技能` | `稀有` | `任意友方` | 获得{HalfLifeHealPower:diff()}层治愈。 |
| `SkillIronVirgin` | 铁处女 | `1` | `技能` | `稀有` | `自己` | 获得1层铁处女，直到下一回合开始前免疫伤害。<br>如果当前回合还未触发边界强化，则额外获得3点临时力量。 |
| `SkillMixedCocktails` | 调制饮料 | `0` | `技能` | `稀有` | `任意玩家` | 选择一名玩家<br>弃置至多3张手牌，按调制为目标提供效果，然后抽取等于弃置数量的卡牌。 |
| `SkillMudTruckCrash` | 泥头车，创死死！ | `0` | `攻击` | `稀有` | `任意敌人` | 造成{Damage:diff()}点伤害。<br>对所有其他敌人造成等量的伤害，并施加骨折。 |
| `SkillNinjutsuCombo` | 忍术连击 | `0` | `技能` | `稀有` | `自己` | 获得{energyPrefix:energyIcons(1)}。将1张随机基础技能牌加入手中。 |
| `SkillPowerfulPity` | 强者怜悯 | `0` | `技能` | `稀有` | `任意玩家` | 指定一名玩家，选择手中的至多3张牌，复制到其手中。<br>你获得等同于复制数量的星光，弃置所选牌，并获得1层永恒星光。 |
| `SkillProductRestocking` | 商品补货 | `0` | `技能` | `稀有` | `自己` | 弃置所有手牌，抽取原手牌数量+1张的卡牌。 |
| `SkillRemoteIntrusion` | 远程侵入 | `0` | `技能` | `稀有` | `自己` | 立即获得1张攻击牌到手中，本回合其费用为0。<br>本回合你打出的攻击牌不会触发【人格：猫猫】的被动防火墙弃牌效果。 |
| `SkillRoyalPrerogative` | 女王特权 | `0` | `技能` | `稀有` | `自己` | 抽3张牌。<br>被抽到的牌将附带临时，并在回合结束时强制弃置。 |
| `SkillSaveMeMousy` | 鼠鼠救我 | `0` | `技能` | `稀有` | `任意友方` | 选择一名玩家，其抽1张牌，并获得鼠鼠护盾和反击。 |
| `SkillShadowFusion` | 暗影融合 | `0` | `技能` | `稀有` | `自己` | 吸收所有双生之影。<br>每吸收一个影子使你本回合内力量+1。 |
| `SkillSolarBombardment` | 轨道轰炸 | `0` | `技能` | `稀有` | `随机敌人` | 弃置所有手牌,对随机敌人进行基础{Damage:diff()}点伤害的轨道轰炸，共{Repeat:diff()}次。 |
| `SkillTransfer` | 转账 | `0` | `技能` | `稀有` | `任意友方` | 你失去5金币，使目标玩家获得5星光<br>你的财富积累+1。 |
| `SkillTroubleMaker` | 麻烦制造者 | `0` | `技能` | `稀有` | `自己` | 展示3张随机事件牌，选择1张立即发动。<br>获得{StarLight:diff()}点星光。 |
| `SkillUnstoppable` | 无可阻挡 | `0` | `技能` | `稀有` | `自己` | 立即获得蓄势待发。<br>弃置你手上的所有攻击牌。<br>获得1张泥头车，创死死！。<br>抽12张牌。 |
| `SkillVampireBite` | 嘬你一口 | `0` | `技能` | `稀有` | `自己` | 如果你的生命值高于当前最大生命值的50%，则受到6点伤害。<br>获得1层喋血。 |

## Power 列表

| 类名 | 中文名 | 类型 | 叠层方式 | 效果 |
|------|------|------|------|------|
| `AmbushPower` | 突袭 | `增益` | `计数` | 获得等同于层数的临时力量。你的回合结束时移除。 |
| `BloodthirstPower` | 喋血 | `增益` | `计数` | 你的下一张攻击牌造成伤害时，会将其中的50%转化为回复生命。<br>本回合内，部分依赖低生命值的效果会按低于15%生命结算。 |
| `BoundaryReinforcementPower` | 边界强化 | `增益` | `计数` | 持续2回合。获得的当回合没有效果。若从获得时起直到下一个自身回合开始前都未受到伤害，则在下一个自身回合开始时获得3点临时力量。期间只要受到伤害，则移除此能力。 |
| `BronzeGongPower` | 强化·大铜锣 | `增益` | `计数` | 每层使你获得1点临时力量和1层临时幻影·逆鳞。回合结束时移除。 |
| `CandyEnergySupplementBarPower` | 糖果·能量补充棒 | `增益` | `计数` | 每层会在本回合和下回合各提供3点临时力量与2点临时敏捷。 |
| `ConcealingPower` | 隐匿 | `增益` | `不叠层` | 直到你的下一回合开始前，免疫敌方来源的伤害。<br>在此期间，第一次对敌人造成伤害时，获得等同于其标记层数的突袭，然后移除此能力。 |
| `CopyQuotaPower` | 复制额度 | `增益` | `计数` | 显示你本回合还能消耗多少次技能牌，才会让【人格：忍者】的复制效果失效。你的回合开始时重置为9。 |
| `CosmosFreezesPower` | 静滞 | `减益` | `计数` | 此单位造成伤害时，会按(1-100/(100+层数))计算后直接减少伤害，图标数字显示当前减少值。<br>当层数大于10时，每回合结束减少当前层数的10%，最低减少1层。 |
| `CounterPower` | 反击 | `增益` | `计数` | 受到未被格挡的伤害后，消耗1层反击，对攻击来源造成等同于该伤害+当前力量的伤害。 |
| `CrossedTwinCarpPower` | 强化·交错双鲤 | `增益` | `不叠层` | 获得6层活力。下次造成伤害时，额外无视本次被格挡的伤害，然后移除。 |
| `CuteIsJusticePower` | 可爱即正义 | `增益` | `计数` | 每层获得1点力量。 |
| `CyberKittyFirewallBypassPower` | 被动防火墙关闭 | `增益` | `不叠层` | 本回合你打出的攻击牌不会触发【人格：猫猫】的被动防火墙弃牌效果。 |
| `CyberKittyNodePower` | 节点 | `增益` | `计数` | 回合开始时，将节点值随机刷新为1到10。<br>在Boss房中，不会触发这个位置表现效果。 |
| `DragonAwakeningPower` | 觉醒 | `增益` | `计数` | 战斗结束时，跃龙门会按层数减少七十二天雷。 |
| `DragonRoarWardPower` | 龙吼护体 | `增益` | `不叠层` | 在敌方行动开始前，免疫敌方来源的伤害。 |
| `ElegantFeatherPower` | 优雅之羽 | `增益` | `计数` | 每层使你获得1点临时力量。受到未被格挡的伤害时，减少1层。 |
| `EsotericEmpowerPower` | 密宗强化 | `增益` | `计数` | 当你的技能牌对敌方单位造成伤害时，额外造成等同于本能力层数的伤害。 |
| `ExposurePower` | 曝光 | `减益` | `计数` | 被拥有手电筒系列遗物的玩家击杀时，击杀者获得1层永恒星光。 |
| `FateWeakImprintPower` | 虚弱印记 | `减益` | `不叠层` | 受到的任意伤害+1。不可叠加，重复施加会刷新为2回合。<br>若带有此印记时被击倒，击杀者获得1张命运指引。<br>持续2回合。 |
| `FracturePower` | 骨折 | `减益` | `计数` | 施加易伤1层和缩小1层。<br>持续1回合。 |
| `GiantAnchorPower` | 大铁锚 | `增益` | `计数` | 每层使你获得1点临时力量。你的回合结束时减少1层。 |
| `HalfLifeHealPower` | 治愈 | `增益` | `计数` | 回合开始时，回复等同于层数的生命，下回合层数减半。 |
| `HuntersFeastPower` | 掠食 | `增益` | `计数` | 本场战斗结束时，永久提升等同于层数的暗影额度上限。<br>进入精英房，提供1层掠食；<br>进入Boss房时，提供3层掠食。 |
| `IAmDragonTemporaryStrengthLossPower` | 龙威压制 | `减益` | `计数` | 本回合内，力量-{Amount}。回合结束时恢复。 |
| `InvestigationTargetPower` | 调查目标 | `减益` | `不叠层` | 不可叠加，不可重复。<br>带有此能力的单位被击杀后，击杀者会立即触发1张隐匿调查事件牌，【人格：邦尼】持有者获得5点星光。 |
| `IronVirginWardPower` | 铁处女 | `增益` | `不叠层` | 直到你的下一回合开始前，免疫所有伤害。 |
| `LingHunLianJiePower` | 灵魂链接 | `增益` | `计数` | 灵魂链接。 |
| `LittleCarpDollPower` | 强化·小鲤鱼玩偶 | `增益` | `计数` | 每层使你获得1点力量和1点敏捷。 |
| `MarkLockPower` | 标记 | `减益` | `计数` | 可叠加，但无论有多少层，受到的任意伤害只会额外+1。每回合减少1层。 |
| `MixedCocktailsPower` | 调制效果 | `增益` | `不叠层` | 本次调制饮料提供的效果：<br>临时力量：{TemporaryStrength}<br>临时敏捷：{TemporaryDexterity}<br>额外敏捷：{Dexterity}<br>回复：{Heal}<br>能量：{Energy:energyIcons()}<br>抽牌：{Draw}<br>格挡：{Block} |
| `ModificationPower` | 改造 | `增益` | `计数` | 你的回合结束时，减少1层。 |
| `MouseShieldPower` | 鼠鼠护盾 | `增益` | `不叠层` | 鼠鼠护盾会帮你减少999点未被格挡的伤害。 |
| `ProblemStudentPower` | 问题学生 | `减益` | `不叠层` | 不可叠加。获得4点敏捷，受到的伤害+2。在Boss房中受到敌方攻击后移除。 |
| `ReadyToStrikePower` | 蓄势待发 | `增益` | `不叠层` | 获得2点临时力量。<br>本回合你的攻击牌造成的伤害将无视格挡。<br>本回合每当你抽到1张牌时进行判定：若不是攻击牌，则获得1点活力并返回抽牌堆；若是攻击牌，则获得1点临时力量并进入弃牌堆。<br>当【无可阻挡】结束后，你的临时力量或活力个位数为1或6的倍数时，向手中加入1张泥头车，创死死！。 |
| `ReversedScalesHolographicPower` | 幻影·逆鳞 | `增益` | `计数` | 每层使你获得1点伤害减免。 |
| `ReversedScalesPower` | 幻影·逆鳞 | `增益` | `计数` | 每层使你获得2点伤害减免。每次获得1层时，获得2点临时力量。回合结束时移除。 |
| `ShadowsLimitPower` | 暗影额度 | `增益` | `计数` | 显示本回合还能通过攻击牌施加几次双生之影。 |
| `StagnantCosmosPower` | 静滞协议 | `增益` | `计数` | 每当你打出一张实际消耗不低于{energyPrefix:energyIcons(1)}的攻击牌或技能牌并造成伤害时，消耗1层静滞协议，使目标获得等同于该牌费用的静滞。 |
| `StarLightPower` | 星光 | `增益` | `计数` | 本场战斗结束后，获得等同于星光层数的金币。 |
| `SwordAuraPower` | 剑气 | `增益` | `计数` | 前2层会使你的攻击额外造成1点伤害，第3层会额外造成2点伤害。最多3层。 |
| `TridentEmpowerPower` | 强化·三叉戟 | `增益` | `不叠层` | 不可叠加。获得1点力量和1点敏捷。 |
| `TrueDragonFormPower` | 真龙形态 | `增益` | `不叠层` | 伤害+4，获得2点力量、1点敏捷，并永久获得2点最大生命。不可叠加。 |
| `TwinShadowsPower` | 双生之影 | `减益` | `计数` | 双生之影都会在目标回合开始时使其失去1点生命。<br>持续2回合。 |
| `VoidCrystalPower` | 虚空协议 | `增益` | `不叠层` | 不可叠加，不可重复。3回合后，能量上限+{energyPrefix:energyIcons(1)}。 |
| `ZuoTeaCakePower` | 佐茶蛋糕 | `增益` | `计数` | 每层使你获得1点临时力量和1点临时敏捷。回合结束时移除本能力。 |

## 遗物列表

### 人格遗物

| 类名 | 中文名 | 稀有度 | 效果 |
|------|------|------|------|
| `PersonBionicJasmine` | 【人格：绿油油】 | `古代` | · 战斗开始时，力量-1、敏捷-1。<br>· 每累计获得13点步数，交替获得1点力量或1点敏捷。 |
| `PersonBlueWhale` | 【人格：大虎鲸】 | `古代` | · 每3回合开始时，将1张虚弱印记加入手中。<br>· 拥有虚弱印记的敌人被击倒时，你获得3金币。<br>· 如果战斗恰好在第6回合结束，你获得6金币;之后每次触发时，额外增加2金币。 |
| `PersonCyberKitty` | 【人格：猫猫】 | `古代` | · 每3回合开始时，将1张远程侵入加入手中。<br>· 回合开始时，若你的节点大于3，获得1点力量和敏捷，将1张攻击牌置入手中免费打出；<br>· 你每回合第一次打出攻击牌后，弃置手牌中最左侧的1张攻击牌，将其在本场战斗中升级； |
| `PersonDeityLin` | 【人格：凛】 | `古代` | · 每3回合开始时，获得1层调查发现。<br>· 战斗开始时，将1张活体书页加入手中。<br>· 每消耗3层人格衍生：活体书页，活体书页的基础伤害永久+1。 |
| `PersonFeng` | 【人格：枫】 | `古代` | · 每3回合开始时，将1张运功加入手中。<br>· 战斗开始时，获得1层养精蓄锐。<br>· 若你本回合没有打出攻击牌，则回合结束时获得1层养精蓄锐；在精英房和Boss房中改为获得2层。<br>· 打出攻击牌时，移除1层养精蓄锐；若打出前正好有5层，则额外移除2层，并对其他敌方单位造成与主目标相同的实际伤害。 |
| `PersonInkShadowHunter` | 【人格：小猎手】 | `古代` | · 每3回合开始时，将1张暗影融合加入手中。<br>· 回合开始时，为1名随机敌人施加1道双生之影。<br>· 当你对敌方打出1到3费的攻击牌时，为目标施加1道双生之影；施加次数等于你当前的暗影额度上限。 |
| `PersonJillSteinle` | 【人格：调酒师】 | `古代` | · 每3回合开始时，将1张调制饮料加入手中。<br>· 若恰好弃置3张不同类型的牌，你获得3星光。<br>· 若恰好弃置3张相同类型的牌，目标额外获得3敏捷。 |
| `PersonMascotGirlMimi` | 【人格：米米】 | `古代` | · 每3回合开始时，将1张商品补货加入手中。<br>· 通过商品补货每弃置1张牌，获得1点星光。<br>· 通过商品补货累计抽取25张牌后，从3个当前未拥有的筹码遗物中选择1个，获得其对应的临时遗物能力，并将其记录到筹码记忆中。<br>· 战斗结束时，此遗物冷却-1。 |
| `PersonMidnightFlash` | 【人格：午夜闪光】 | `古代` | · 每3回合开始时，将1张无可阻挡加入手中。<br>· 当你在蓄势待发状态下击杀敌人时，此人格遗物冷却-2。 |
| `PersonMousyLian` | 【人格：鼠鼠】 | `古代` | · 每3回合开始时，将1张鼠鼠救我加入手中。 |
| `PersonNinja` | 【人格：忍者】 | `古代` | · 每3回合开始时，将1张忍术连击加入手中。<br>· 你每回合打出第3/6/9张技能牌时，复制最后打出的非人格技能牌加入手中，并附加消耗。 |
| `PersonOasisQueen` | 【人格：绿洲女王】 | `古代` | · 每3回合开始时，将1张女王特权加入手中。<br>· 造成伤害时，手牌中每有1张带有临时的牌，伤害+1，最多+3。 |
| `PersonPoisonedApple` | 【人格：邦妮】 | `古代` | · 每3回合开始时，将1张隐匿行动加入手中。<br>· 将隐匿调查系列事件加入可选卡池；<br>· 你的攻击牌对拥有标记的敌人造成伤害时，伤害+3。 |
| `PersonProprietress` | 【人格：老板娘】 | `古代` | · 进入商店时获得10金币。<br>· 只要局内任意玩家持有此遗物，所有玩家的商品都有3%的概率打三折;每经过1个商店，该折扣概率提高3%。<br>· 每2回合开始时，将1张转账加入手中。。 |
| `PersonSamuraiPrawn` | 【人格：太刀虾】 | `古代` | · 每3回合开始时，将1张名刀加入手中。<br>· 每回合你首次攻击敌人时，获得1层剑气，最多3层 |
| `PersonShadowScion` | 【人格：阿尔】 | `古代` | · 本局游戏第1场战斗开始时，所有玩家获得1张王国资产。<br>· 每3回合开始时，将1张强者怜悯加入手中。<br>· 当你使任意玩家获得卡牌或抽牌时，你与该玩家各获得1点星光。<br>· 你每拥有12层永恒星光，战斗开始时便获得1点力量和1点敏捷。 |
| `PersonSlimeLulu` | 【人格：史莱姆】 | `古代` | · 最大生命值-10。<br>· 每3回合开始时，将1张治愈黏液加入手中。<br>· 受到伤害后，获得1层治愈，并使此人格遗物冷却-1。 |
| `PersonSocialFearNun` | 【人格：社恐修女】 | `古代` | · 每3回合开始时，将1张铁处女加入手中。<br>· 每回合开始时，施加1层边界强化。 |
| `PersonSupermanMegas` | 【人格：美甲师】 | `古代` | · 每3回合开始时，将1张轨道轰炸加入手中。<br>· 回合结束时，若你的手牌少于5张，则下回合额外抽1张牌。<br>· 打出轨道轰炸后，抽1张牌。 |
| `PersonUnclePederman` | 【人格：叔叔】 | `古代` | · 每3回合开始时，将1张真的生气了！加入手中。<br>· 回合开始时，刷新节点为1到6，并随机获得-2到+2的临时力量、临时敏捷、活力、格挡与能量。<br>· 若节点为6，本回合你的攻击牌无视格挡。若节点为1，则下回合节点必定为6。 |
| `PersonVampire` | 【人格：吸血鬼】 | `古代` | · 每3回合开始时，将1张嘬你一口加入手中。<br>· 当你的生命值低于25%/50%/65%时，分别获得1层可爱即正义； |
| `PersonWeirdEgg` | 【人格：蒸蛋】 | `古代` | · 每3回合开始时，将1张麻烦制造者加入手中。 |
| `PersonXiaoLei` | 【人格：小雷】 | `古代` | · 每3回合开始时，将1张连锁反应加入手中。<br>· 当你使其他角色获得卡牌时，你获得1层觉醒。<br>· 经历七十二天雷，三十六天火后，解锁真龙形态。 |
| `PersonZhao` | 【人格：照】 | `古代` | · 每3回合开始时，将1张降神加入手中。<br>· 战斗开始时，固有追击。<br>· 当你自身或当前拥有降神的目标使用攻击牌时，按其实际费用累计；每累计满2费，你获得1层狐火。 |

### 人格衍生遗物

| 类名 | 中文名 | 稀有度 | 效果 |
|------|------|------|------|
| `PersonalityDerivativeMascotGirlMimiTokenMemory` | 【人格衍生：筹码记忆】 | `古代` | · 记录本局游戏中你通过商品补货获得的临时筹码能力。<br>· 当同一种临时筹码能力累计获得达到3次时，在战斗结束后的奖励中加入对应的筹码遗物。 |
| `PersonalityDerivativeNinjaGarrote` | 【人格衍生：绞毙】 | `古代` | · 初始层数为1。<br>· 战斗开始时，获得等同于本遗物层数的密宗强化。<br>· 你在一场战斗中每打出6张不低于一费的技能牌，使人格：忍者冷却-1。<br>· 你在一场战斗中每打出9张不低于一费的技能牌，本遗物永久层数+1。 |
| `PersonalityDerivativeProprietressWealthism` | 【人格衍生：财富】 | `古代` | · 每经过两个房间，获得等于财富层数的金币。 |
| `PersonalityDerivativeSwordIntent` | 【人格衍生：剑意】 | `古代` | · 名刀每累计消耗2层剑气，此遗物层数+1。<br>· 每次打出名刀后，对目标追加一次等于剑意层数的伤害。 |
| `PersonalityDerivativeXiaoLeiDragonGate` | 【人格衍生：跃龙门】 | `古代` | · 战斗结束时，每有1层觉醒，七十二天雷-1。<br>· 每次受到伤害时，三十六天火-1。<br>· 当两项试炼都归零后，每场战斗开始时获得真龙形态，并将1张龙之咆哮加入手中。 |
| `PersonalityDerivativeLivingFolio` | 【人格衍生：活体书页】 | `古代` | · 初始层数为1，最多9层。<br>· 进入事件房时，层数+1。<br>· 每打出1张事件卡，层数+1；麻烦制造者不计入此效果。 |

### 蓝色筹码

| 类名 | 中文名 | 稀有度 | 效果 |
|------|------|------|------|
| `TokenBlueAtm` | 【ATM】 | `普通` | 当你使其他玩家获得星光时，目标额外获得1点星光，你获得1点星光。 |
| `TokenBlueBoxingGloveGeneral` | 【拳击手套·初级】 | `普通` | 每回合开始获得1层活力。<br>拥有【套装·拳击手套】效果。 |
| `TokenBlueDie10` | 【十面·骰子】 | `未标注` | 如果你正好在第10回合结束战斗，则在下场战斗开始时获得10点星光和7点格挡。<br><br>每触发3次后，获得你没有的骰子系列遗物。若骰子系列已集齐，则此效果不再触发。 |
| `TokenBlueDie12` | 【十二面·骰子】 | `未标注` | 如果你正好在第12回合结束战斗，则在下场战斗开始时获得12点星光并恢复9点生命。<br><br>每触发3次后，获得你没有的骰子系列遗物。若骰子系列已集齐，则此效果不再触发。 |
| `TokenBlueDie20` | 【二十面·骰子】 | `未标注` | 如果你正好在第20回合结束战斗，则在下场战斗开始时获得20点星光，并获得其他骰子在下场战斗开始时的所有效果。<br><br>每触发3次后，获得你没有的骰子系列遗物。若骰子系列已集齐，则此效果不再触发。 |
| `TokenBlueDie4` | 【四面·骰子】 | `未标注` | 如果你正好在第4回合结束战斗，则在下场战斗开始时获得4点星光和{energyPrefix:energyIcons(2)}。<br><br>每触发3次后，获得你没有的骰子系列遗物。若骰子系列已集齐，则此效果不再触发。 |
| `TokenBlueDie6` | 【六面·骰子】 | `未标注` | 如果你正好在第6回合结束战斗，则在下场战斗开始时获得6点星光并额外抽3张牌。<br><br>每触发3次后，获得你没有的骰子系列遗物。若骰子系列已集齐，则此效果不再触发。 |
| `TokenBlueDie8` | 【八面·骰子】 | `未标注` | 如果你正好在第8回合结束战斗，则在下场战斗开始时获得8点星光，并对所有敌人造成5点伤害。<br><br>每触发3次后，获得你没有的骰子系列遗物。若骰子系列已集齐，则此效果不再触发。 |
| `TokenBlueElegantFeather` | 【优雅之羽】 | `普通` | 每当你受到的伤害被格挡时，获得1层优雅之羽，最多3层。 |
| `TokenBlueFlashlightGeneral` | 【手电筒·一般】 | `普通` | 击杀敌方时，恢复4点生命。<br>拥有【套装·手电筒】效果。 |
| `TokenBlueGiantAnchor` | 【大铁锚】 | `普通` | 受到未被格挡的伤害时，获得1层大铁锚。<br>拥有【系列·梦想号】效果。 |
| `TokenBlueHandheldFanSmall` | 【手持风扇·小】 | `普通` | 使用人格技能牌后，对随机怪物施加1层标记。 |
| `TokenBlueMarkSprayCan` | 【标记喷罐】 | `普通` | 使用技能牌对敌方造成伤害后，对目标施加1层标记。 |
| `TokenBlueMedicalKitEmergencyTreatment` | 【医疗箱·紧急治疗】 | `普通` | 每回合开始时，获得1层治愈。 |
| `TokenBlueMembersReferenceStandard` | 【会员推荐信】 | `普通` | 在你的第3的倍数回合开始时，额外抽1张牌。 |
| `TokenBlueMotorcycleHelmetGeneral` | 【摩托头盔·一般】 | `普通` | 战斗开始时，获得1层幻影·逆鳞。<br>每回合开始时，获得1点格挡。 |
| `TokenBluePiggyBank` | 【小猪存钱罐】 | `普通` | 你每消耗{energyPrefix:energyIcons(6)}，获得3点星光。 |
| `TokenBlueSandwichBiscuitGeneral` | 【夹心饼干·一般】 | `普通` | 获得8点最大生命。 |
| `TokenBlueSpeedRollerGeneral` | 【速度滑轮·初级】 | `普通` | 战斗开始时，获得1点敏捷。 |
| `TokenBlueTargetBoard` | 【标靶】 | `普通` | 你的攻击牌伤害+1。<br>回合开始时，对随机敌人施加1层标记。 |

### 紫色筹码

| 类名 | 中文名 | 稀有度 | 效果 |
|------|------|------|------|
| `TokenPurpleAdrenalineGeneral` | 【肾上腺素·一般】 | `非普通` | 生命值低于50%时，获得1点临时力量；生命值低于25%时，改为获得3点临时力量。 |
| `TokenPurpleArtKnifeBeginner` | 【美工刀·初级】 | `非普通` | 生命值为满时，获得2点临时力量；你的攻击牌造成的伤害额外增加等同于治愈层数的数值。 |
| `TokenPurpleBasicScope` | 【普通瞄具】 | `非普通` | 你的攻击牌伤害+2，并在造成伤害后施加1层标记。 |
| `TokenPurpleBigBackpack` | 【大背包】 | `非普通` | 战斗开始时，能量上限+{energyPrefix:energyIcons(1)}。 |
| `TokenPurpleBoxingGloveIntermediate` | 【拳击手套·中级】 | `非普通` | 战斗开始时，获得1点力量；每回合开始获得1层活力。<br>拥有【套装·拳击手套】效果。 |
| `TokenPurpleFlashlightStronglight` | 【手电筒·强光】 | `非普通` | 累计打出3张不低于1费的攻击牌，获得1点星光。<br>拥有【套装·手电筒】效果。 |
| `TokenPurpleFriendshipBadge` | 【友情徽章】 | `非普通` | 当你使队友获得星光或治愈时，你与该目标各获得1层治愈。 |
| `TokenPurpleMembersReferencePremium` | 【超级会员推荐信】 | `非普通` | 在你的第3的倍数回合开始时，额外抽2张牌。 |
| `TokenPurpleMotorcycleHelmetIntermediate` | 【摩托头盔·中级】 | `非普通` | 战斗开始时，获得2层幻影·逆鳞。<br>每回合开始时，获得2点格挡。 |
| `TokenPurpleSandwichBiscuitIntermediate` | 【夹心饼干·美味】 | `稀有` | 获得15点最大生命。回合开始时，若你当前生命低于最大生命的50%，获得1层反击。 |
| `TokenPurpleSmartWatch` | 【智能手表】 | `非普通` | 回合结束时，如果你的手牌少于5张，下回合额外抽1张牌。 |
| `TokenPurpleSpeedRollerIntermediate` | 【速度滑轮·中级】 | `非普通` | 战斗开始时，获得2点敏捷。 |
| `TokenPurpleTastyCandy` | 【可口糖果】 | `非普通` | 打出不低于1费的手牌时，获得1层治愈。<br>若你当前生命已满，则消耗4层治愈并获得{energyPrefix:energyIcons(1)}。 |

### 金色筹码

| 类名 | 中文名 | 稀有度 | 效果 |
|------|------|------|------|
| `TokenGoldAdrenalineEfficient` | 【肾上腺素·高效】 | `稀有` | 生命值低于50%时，获得3点临时力量；生命值低于25%时，改为获得8点临时力量。 |
| `TokenGoldArtKnifeEnchanted` | 【美工刀·淬魔】 | `稀有` | 生命值为满时，获得4点临时力量；你的技能牌造成的伤害额外增加等同于治愈层数1/2的数值。 |
| `TokenGoldArtKnifeSharp` | 【美工刀·锋利】 | `稀有` | 生命值为满时，获得4点临时力量；你的攻击牌造成的伤害额外增加等同于治愈层数的数值。 |
| `TokenGoldBigStewBowl` | 【大碗炖肉】 | `稀有` | 回合结束时，所有玩家获得1层治愈并恢复1点生命。 |
| `TokenGoldBoxingGlovePremium` | 【拳击手套·高级】 | `稀有` | 战斗开始时，获得2点力量；每回合开始获得3层活力。<br>拥有【套装·拳击手套】效果。 |
| `TokenGoldBufferShield` | 【缓冲盾牌】 | `稀有` | 当敌方单位以攻击意图攻击你时，获得1层治愈并获得3点星光。 |
| `TokenGoldEagleEyeScope` | 【鹰眼瞄具】 | `稀有` | 你的攻击牌造成伤害时，先为目标施加1层标记，本次伤害额外增加目标标记层数×2。 |
| `TokenGoldExplorationSatellite` | 【探天微星】 | `稀有` | 所有星引擎系列效果牌，额外追加1次基础伤害(3点)的轨道轰炸。<br>回合结束时，如果你的手牌少于6张，下回合获得1张轨道炮。 |
| `TokenGoldExtraBattery` | 【额外电池】 | `稀有` | 冷却类人格遗物的冷却上限-1。<br>【人格：绿油油】的步数转换需求降低1/4。 |
| `TokenGoldFlashlightFlashburst` | 【手电筒·爆闪】 | `稀有` | 累计打出3张不低于1费的攻击牌，获得1点星光。<br>每有10层永恒星光，你的攻击牌伤害+1。<br>拥有【套装·手电筒】效果。 |
| `TokenGoldHandheldFanLarge` | 【手持风扇·大】 | `稀有` | 使用人格技能牌后，抽1张牌，并对随机怪物施加1层标记。 |
| `TokenGoldInitialPoint` | 【初始点】 | `稀有` | 在休息处，你可以花费金币升星<br>花费60/90/120金币，获得随机筹码遗物，恢复15%最大生命值；<br>完成3次后，该选项仅恢复15%最大生命值。 |
| `TokenGoldMagicQuiver` | 【魔法箭袋】 | `稀有` | 每回合1次，对拥有标记的怪物使用技能牌造成伤害时：施加1层标记，并复制该技能牌返回手牌。 |
| `TokenGoldMedicalKitCompleteTreatment` | 【医疗箱·完备治疗】 | `稀有` | 每回合开始时，获得3层治愈。 |
| `TokenGoldMembersReferenceUltimate` | 【至尊黑金会员推荐信】 | `稀有` | 在你的第3的倍数回合开始时，额外抽2张牌，并获得{energyPrefix:energyIcons(2)}。 |
| `TokenGoldMotorcycleHelmetPremium` | 【摩托头盔·高级】 | `稀有` | 战斗开始时，获得2层幻影·逆鳞和1点敏捷；<br>每回合开始时，获得3点格挡。 |
| `TokenGoldNinjaShuriken` | 【忍术飞镖】 | `稀有` | 战斗开始时，能量上限+{energyPrefix:energyIcons(1)}。<br>对敌方目标使用技能牌造成伤害时，使其额外受到等同于标记层数的伤害。 |
| `TokenGoldSandwichBiscuitPremium` | 【夹心饼干·可口】 | `非普通` | 获得11点最大生命。 |
| `TokenGoldSpeedRollerPremium` | 【速度滑轮·高级】 | `稀有` | 战斗开始时，获得4点敏捷。 |
| `TokenGoldStarCoinHammer` | 【星币锤】 | `稀有` | 获得15层永恒星光。<br>若你的永恒星光不少于40层且金币不少于100，每回合第一次造成伤害时：<br>消耗15金币，对目标追加一次等于你当前金币数量10%的伤害。<br>若这次伤害正好击杀目标，则获得6点星光。 |
| `TokenGoldVitamin` | 【维生素】 | `稀有` | 你每抽1张牌，获得1层治愈。<br>每回合最多生效9次。 |

### 专属筹码

| 类名 | 中文名 | 稀有度 | 效果 |
|------|------|------|------|
| `TokenExclusiveAncientWand` | 【古老法杖】 | `非普通` | 所有技能牌造成的伤害+3。<br>战斗开始时，能量上限+{energyPrefix:energyIcons(1)}。<br>拥有【系列·魔法学院】效果。 |
| `TokenExclusiveBoutiqueSwordShield` | 【精品剑盾】 | `稀有` | 战斗开始时，获得2点力量。<br>每回合开始时，获得2层活力。<br>如果是Boss房间，每回合获得1层问题学生。<br>拥有问题学生时，敏捷+4。<br>拥有【系列·魔法学院】效果。 |
| `TokenExclusiveBronzeGong` | 【大铜锣】 | `普通` | 你使用人格技能牌后，所有玩家获得1层强化·大铜锣。<br>拥有【系列·水乡古镇】效果。 |
| `TokenExclusiveCrossedTwinCarp` | 【交错双鲤】 | `稀有` | 每回合开始时，获得强化·交错双鲤。<br>当你获得活力时，如果没有强化·交错双鲤，则获得它。<br>拥有【系列·龙宫游乐园】效果。 |
| `TokenExclusiveCursedSword` | 【诅咒之剑】 | `普通` | 战斗开始时，获得99层脆弱。<br>第1回合开始时和每个3的倍数回合开始时，获得等同于此遗物层数的活力。<br>每当你击倒1名怪物，此遗物层数+1。<br>拥有【系列·御魂庆典】效果。 |
| `TokenExclusiveDreamshipModel` | 【梦想号模型】 | `稀有` | 战斗开始时，获得2点敏捷。<br>你每打出15张牌，人格遗物冷却-1。<br>拥有【系列·梦想号】效果。 |
| `TokenExclusiveInfiniteSnake` | 【无限之蛇】 | `稀有` | 战斗开始时，能量上限+{energyPrefix:energyIcons(1)}。<br>每回合开始时，如果你的手牌少于8张，则将手牌补至8张。<br>拥有【系列·龙宫游乐园】效果。 |
| `TokenExclusiveLittleCarpDoll` | 【小鲤鱼玩偶】 | `非普通` | 使用人格技能牌后，获得1层反击。<br>触发任何反击时，获得1层强化·小鲤鱼玩偶，每回合限1次。<br>拥有【系列·龙宫游乐园】效果。 |
| `TokenExclusiveLittleSnakeDoll` | 【小蛇玩偶】 | `非普通` | 每次受到伤害时，获得1层逆鳞。<br>拥有【系列·龙宫游乐园】效果。 |
| `TokenExclusivePiercingGun` | 【贯穿之铳】 | `稀有` | 你的技能牌造成伤害时，会额外结算本次被格挡的伤害，相当于先消除护盾再造成伤害。<br>拥有【系列·御魂庆典】效果。 |
| `TokenExclusivePsychedelicSeafoodSoup` | 【迷幻海鲜汤】 | `普通` | 每回合开始时，随机获得1层或6层活力。<br>拥有【系列·龙宫游乐园】效果。 |
| `TokenExclusiveStormTalisman` | 【惊涛御守】 | `普通` | 使用技能牌时，获得1层灾厄。<br>每抽1张牌，获得1层治愈。<br>拥有【系列·龙宫游乐园】效果。 |
| `TokenExclusiveTimer` | 【计时器】 | `普通` | 当你获得改造时，额外获得1层。每有2层改造，获得1点力量。<br>拥有【系列·幽魂暗巷】效果。 |
| `TokenExclusiveTrident` | 【三叉戟】 | `非普通` | 你使用人格技能牌后，使1名没有强化·三叉戟的玩家获得强化·三叉戟。<br>拥有【系列·梦想号】效果。 |
| `TokenExclusiveVengeanceHalberd` | 【复仇之戟】 | `非普通` | 如果你拥有防御损失类debuff，则获得5点临时力量。<br>如果你拥有血量损失类debuff，则获得5点临时敏捷。<br>拥有【系列·御魂庆典】效果。 |
| `TokenExclusiveZuoTeaCake` | 【佐茶蛋糕】 | `普通` | 战斗开始时，获得1点力量。<br>获得后，在当前阶段内，你打出的攻击牌对非精英/非Boss单位造成伤害时，获得1层佐茶蛋糕。<br>下一阶段失效。<br>拥有【系列·魔法学院】效果。 |

### 其他筹码

| 类名 | 中文名 | 稀有度 | 效果 |
|------|------|------|------|
| `TokenEternalStarlight` | 【永恒星光】 | `非普通` | 每攀爬3层楼层，获得等同于永恒星光层数的金币。<br>{CurrentSetLine} |

## 其他内容

| 内容类型 | 数量 | 说明 |
|------|------|------|
| 药水 | 4 | 包含起始人格宝箱选择药水、糖果系列药水和随机筹码包。 |
| Modifier | 0 | 当前主要包含星引擎相关的自定义 modifier。 |
| 附魔 | 1 | 当前主要是人格技能牌使用的冷却附魔。 |
| 卡牌关键词 | 20 | 统一通过 `AstralKeywords` 注册并走 owned keyword 本地化。 |
| 遗物图鉴分类文本 | 1 | 用于人格遗物与筹码遗物的图鉴分组和展示文本。 |
| 休息处文本 | 5 | 当前主要服务于【初始点】等自定义休息处选项。 |

## 项目结构

```text
ReAstralPartyMod/
├─ ReAstralPartyCardCode/
│  ├─ Cards/
│  │  ├─ AstralPartyCardModel.cs               # 本模组卡牌模板，统一资源路径、唯一约束和冷却附魔处理
│  │  ├─ BaseAbilityOrbitalRailgun.cs          # 基础能力牌
│  │  ├─ CollectorsCard*.cs                    # 收集器与特殊牌
│  │  ├─ Event*.cs / Events*.cs                # 事件卡与事件阶段卡
│  │  └─ Skill*.cs                             # 人格技能牌与战斗技能牌
│  ├─ Enchantments/
│  │  └─ AstralCooldownEnchantment.cs          # 人格技能牌冷却附魔
│  ├─ Events/                                  # 自定义事件与事件辅助逻辑
│  ├─ Keywords/
│  │  └─ AstralKeywords.cs                     # 关键词门面、owned keyword 注册入口
│  ├─ Modifiers/                               # 自定义 modifier
│  ├─ Patches/
│  │  ├─ GameplayPatchRegistry.cs              # RitsuLib patcher 统一注册入口
│  │  ├─ StartingPersonaRelicSelectionPatch.cs # 起始人格遗物选择同步与流程补丁
│  │  ├─ StarEngine*.cs                        # 星引擎奖励、事件与战斗相关补丁
│  │  ├─ TemporaryCardHighlightPatch.cs        # 临时卡牌 UI 表现补丁
│  │  └─ TreasureRoomRelicSessionHelper.cs     # 宝箱/遗物选择多人同步辅助
│  ├─ Potions/
│  │  └─ PersonChestChoose.cs                  # 起始人格宝箱药水
│  ├─ Powers/
│  │  ├─ AstralPartyPowerModel.cs              # 本模组 Power 模板与图标路径逻辑
│  │  ├─ AstralTemporaryStatPowers.cs          # 临时力量/敏捷等通用能力实现
│  │  └─ TokenRelicBridgePower.cs              # 将筹码遗物效果桥接为战斗内能力的工具基类
│  ├─ Relics/
│  │  ├─ AstralPartyRelicModel.cs              # 本模组遗物模板
│  │  ├─ CooldownPersonaRelicBase.cs           # 冷却型人格遗物公共基类
│  │  ├─ LegacyCooldownPersonaRelicBase.cs     # 旧式冷却人格遗物兼容基类
│  │  ├─ Person*.cs                            # 人格遗物
│  │  ├─ PersonalityDerivative*.cs             # 人格衍生遗物
│  │  └─ Token*.cs                             # 筹码遗物与系列遗物
│  ├─ RestSite/
│  │  └─ InitialPointRestSiteOption.cs         # 初始点休息处选项
│  └─ Utils/
│     ├─ AstralEventCardPool.cs                # 事件卡池、调查阶段卡池和确定性事件抽取
│     ├─ BaseAbilityCardRegistry.cs            # BaseAbility* 卡牌收集、缓存和确定性抽取
│     ├─ BoxingGlovesRelicHelper.cs            # 拳击手套套装计数与共享套装判定
│     ├─ CandyMachineHelper.cs                 # 怪奇糖果机在手检测、补牌与糖果药水发放
│     ├─ CardGainAttribution.cs                # 抽牌/得牌来源归因上下文，供联机和人格效果追踪
│     ├─ CombatTargetOrdering.cs               # 战斗目标稳定排序，避免联机随机目标分歧
│     ├─ CommonActions.cs                      # 常用攻击命令构造器封装
│     ├─ ConcealingInvestigationHelper.cs      # 邦尼调查事件阶段推进、触发者/控制者上下文和结算逻辑
│     ├─ CyberKittyCombatHelper.cs             # 猫猫人格的攻击牌抽取、弃牌升级和战斗内辅助
│     ├─ DeterministicMultiplayerChoiceHelper.cs # 联机确定性选择、远端同步和稳定随机排序
│     ├─ ExtraBatteryRelicHelper.cs            # 额外电池对人格冷却与步数阈值的统一修正
│     ├─ FlashlightRelicHelper.cs              # 手电筒套装计数、共享结算和额外效果辅助
│     ├─ GeneratedCardObserver.cs              # 生成卡加入手牌/牌堆后的通知与观察器分发
│     ├─ GoldModificationGuard.cs              # 金币改动保护，避免重复或递归触发
│     ├─ LowHpStateHelper.cs                   # 当前生命百分比、低血量阈值和相关判定工具
│     ├─ MascotGirlMimiTokenMemoryHelper.cs    # 看板娘筹码记忆的抽牌计数、记录与奖励投放辅助
│     ├─ MidnightFlashHelper.cs                # 午夜闪光专属牌【泥头车，创死死！】识别与生成
│     ├─ PersonaMultiplayerEffectHelper.cs     # 人格效果的多人同步、发牌、给金、授予遗物等封装
│     ├─ PersonaRelicHelper.cs                 # 人格技能牌判定与全体人格冷却推进工具
│     ├─ PersonaRelicRegistry.cs               # 人格遗物总表、可选人格筛选与图鉴相关筛选
│     ├─ PowerSafetyUtils.cs                   # Power 安全性分析，过滤会破坏奖励或战斗结束路径的能力
│     ├─ RecursiveCallGuard.cs                 # 异步递归/重入保护的小型通用门闩
│     ├─ RewardSyncHelper.cs                   # 联机遗物奖励获得流程的同步封装
│     ├─ TokenRelicBridgeHelper.cs             # 将筹码遗物效果桥接成战斗内能力的核心工具
│     ├─ TokenRelicRegistry.cs                 # 筹码遗物总表、随机池与排除规则
│     ├─ VampirePersonaHelper.cs               # 吸血鬼低血量阈值与【可爱即正义】同步逻辑
│     └─ XiaoLeiAwakeningHelper.cs             # 小雷为其他玩家发牌时的觉醒层数发放辅助
├─ ReAstralPartyMod/
│  ├─ images/
│  │  ├─ card_portraits/                       # 卡牌插画与动态卡图资源
│  │  ├─ relic/                                # 遗物图标
│  │  ├─ powers/                               # Power 图标
│  │  ├─ potion/                               # 药水图标
│  │  ├─ enchantments/                         # 附魔图标
│  │  ├─ events/                               # 事件立绘与界面图
│  │  ├─ ui/                                   # 自定义卡框、休息处和图鉴 UI 资源
│  │  └─ background/                           # 起始人格选择等背景图
│  └─ localization/
│     ├─ zhs/                                  # 简体中文本地化
│     └─ eng/                                  # 英文本地化
├─ Scripts/
│  └─ MainFile.cs                              # ModInitializer 入口、RitsuLib 注册、补丁和产物日志
├─ doc/                                        # 迁移文档、玩法设计稿、外部参考资料
├─ logs/                                       # 运行日志与联机问题排查日志
├─ tools/
│  ├─ ValidateLocalization.ps1                 # 中文本地化 UTF-8 校验
│  ├─ readme_extract.json                      # README 生成的提取数据
│  └─ generate_readme.py                       # README 自动生成脚本
├─ ReAstralPartyMod.csproj                     # 构建、PCK 导出和产物复制配置
└─ ReAstralPartyMod.json                       # Mod manifest
```

## 开发说明

### 新增卡牌

| 步骤 | 位置 | 说明 |
|------|------|------|
| 1 | `ReAstralPartyCardCode/Cards/Xxx.cs` | 新建卡牌类并继承 `AstralPartyCardModel`。 |
| 2 | 类声明 | 使用 `[RegisterCard(typeof(...Pool))]` 走 RitsuLib 自动注册。 |
| 3 | `ReAstralPartyMod/localization/zhs/cards.json` | 添加中文 `title / description` 和必要的运行时文本。 |
| 4 | `ReAstralPartyMod/localization/eng/cards.json` | 添加英文对应条目。 |
| 5 | `ReAstralPartyMod/images/card_portraits/` | 按自动路径规则补齐卡图文件。 |

### 新增遗物

| 步骤 | 位置 | 说明 |
|------|------|------|
| 1 | `ReAstralPartyCardCode/Relics/Xxx.cs` | 新建遗物类并继承 `AstralPartyRelicModel` 或现有公共基类。 |
| 2 | 类声明 | 使用 `[RegisterRelic(typeof(...Pool))]` 走 RitsuLib 自动注册。 |
| 3 | `ReAstralPartyMod/localization/zhs/relics.json` | 添加中文 `title / description`。 |
| 4 | `ReAstralPartyMod/localization/eng/relics.json` | 添加英文对应条目。 |
| 5 | `ReAstralPartyMod/images/relic/` | 补齐遗物图标文件，命名遵循类名转蛇形路径。 |

### 新增 Power

| 步骤 | 位置 | 说明 |
|------|------|------|
| 1 | `ReAstralPartyCardCode/Powers/XxxPower.cs` | 新建 Power 类并继承 `AstralPartyPowerModel`。 |
| 2 | `ReAstralPartyMod/localization/zhs/powers.json` | 添加中文 `title / description / smartDescription`。 |
| 3 | `ReAstralPartyMod/localization/eng/powers.json` | 添加英文对应条目。 |
| 4 | `ReAstralPartyMod/images/powers/` | 补齐图标文件，路径按 `AstralPartyPowerModel` 的默认解析规则放置。 |

### 本地化约定
- 卡牌、Power 和遗物都按 RitsuLib 固定 public entry 规则命名，例如 `RE_ASTRAL_PARTY_MOD_CARD_*`、`RE_ASTRAL_PARTY_MOD_POWER_*`、`RE_ASTRAL_PARTY_MOD_RELIC_*`。
- 关键词通过 `AstralKeywords` 集中注册，业务代码不要散落硬编码 keyword id。
- 中文本地化文件统一使用显式 UTF-8 编码，避免 Rider、PowerShell 或脚本链路写出乱码。

## 构建与输出

| 项目 | 配置 |
|------|------|
| Slay the Spire 2 目录 | `D:\Steam\steamapps\common\Slay the Spire 2` |
| 备用游戏目录 | `D:\SteamLibrary\steamapps\common\Slay the Spire 2` |
| RitsuLib 引用 | `$(Sts2Dir)\mods\RitsuLib\STS2-RitsuLib.dll` |
| Godot 编辑器 | `D:\MOD\杀戮尖塔2mod制作\megadot-4.5.1-m.8-windows-x86_64-llvm-editor-csharp\MegaDot_v4.5.1-stable_mono_win64.exe` |
| 主输出目录 | `$(Sts2Dir)\mods\$(MSBuildProjectName)\` |
| 备用输出目录 | `$(SecondarySts2Dir)\mods\$(MSBuildProjectName)\` |

常用构建命令：

```powershell
dotnet build /p:RunPckExport=false
```

构建完成后会自动处理以下产物：
- `dll` 复制到游戏 `mods/ReAstralPartyMod/` 目录。
- 如果启用导出，则由 Godot 生成 `.pck` 并复制到相同目录。
- 入口初始化时会记录 `dll / pck / manifest` 的 SHA256，便于排查联机和部署问题。

## 致谢

- [STS2-RitsuLib](https://github.com/BAKAOLC/STS2-RitsuLib)
- 《杀戮尖塔 2》mod 社区中的案例工程、迁移文档和联机调试经验
