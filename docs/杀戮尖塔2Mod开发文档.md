# 杀戮尖塔2 BaseLib Mod 开发文档

## 文档定位

这份文档基于当前工作区里的两部分源码整理：

- `C:\Users\Lenovo\Desktop\aa\sts2\MegaCrit\sts2\Core`
- `C:\Users\Lenovo\Desktop\aa\sts2\MegaCrit\sts2\Core\BaseLib-StS2`

目标不是复述外部 Wiki，而是直接从当前源码快照反推一份适合实际开发的中文手册。正文只讨论“对写 Mod 真正有用”的内容：

- BaseLib 已经包装好的公开抽象类、接口、变量类、工具类。
- 这些 BaseLib 扩展点对应的 `Core` 核心类覆写点。
- BaseLib 还没有直接抽象出来，必须自己用 `0Harmony.dll` 改的系统。

本文所有示例都采用中性教程风命名，例如 `ExampleCard`、`ExampleCharacter`、`ExampleRewardPatch`。示例重点是“源码上对得上、读完就能改”，不是讲世界观。

## 先建立一个正确认知

当前这套生态有三层能力边界：

| 层级 | 典型内容 | 你该怎么用 |
| --- | --- | --- |
| `BaseLib 直接抽象` | `CustomCardModel`、`CustomCharacterModel`、`CustomRelicModel`、`CustomEventModel`、`CustomMonsterModel`、`SimpleModConfig`、`SavedSpireField` | 优先用这一层。它最稳定，可读性最好，也最不容易和其他 Mod 打架。 |
| `BaseLib 内部已用 Harmony 打通` | 自定义图鉴、角色选择补丁、自定义能量图标、自定义卡背、自定义场景自动转换、`Persist` / `Refund` / `Exhaustive`、配置菜单、血条预测 | 你在外部只写模型或调用工具类，不需要自己补丁；但要知道它背后已经靠 Harmony 生效。 |
| `你自己的 Harmony 补丁` | 地图节点规则、奖励屏额外 UI、营火按钮视觉、自定义结算页、每日上传拦截、成就条件重写 | BaseLib 没有公开抽象时再动手。优先 `Prefix` / `Postfix`，最后才上 `Transpiler`。 |

## 工程骨架与最小初始化

### 推荐目录

```csharp
// ExampleMod/
//   ExampleMod.csproj
//   ExampleMod.json
//   ExampleModMain.cs
//   Config/
//     ExampleModConfig.cs
//   Cards/
//     ExampleSlash.cs
//   Characters/
//     ExampleCharacter.cs
//   Relics/
//     ExampleStarterRelic.cs
//   Events/
//     ExampleEvent.cs
//   Patches/
//     ExampleRewardPatch.cs
//   localization/
//     zhs/
//       cards.json
//       characters.json
//       relics.json
//       events.json
//       powers.json
//       settings_ui.json
//   images/
//   scenes/
```

### `csproj` 关键依赖

BaseLib 自己的 `BaseLib.csproj` 已经确认了两个最关键的外部引用：

- `sts2.dll`
- `0Harmony.dll`

如果你不是直接把 BaseLib 源码合进自己的工程，而是作为依赖使用，项目文件至少要能接到这些程序集。

```csharp
// 下面是“结构示意”，重点是告诉你需要的引用关系。
// 真正路径按你的 StS2 安装目录与 mods 目录调整。

<Project Sdk="Godot.NET.Sdk/4.5.1">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>true</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="0Harmony">
      <HintPath>$(Sts2DataDir)/0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="sts2">
      <HintPath>$(Sts2DataDir)/sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>

    // 如果你走 NuGet 依赖 BaseLib，用 PackageReference。
    <PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" />
  </ItemGroup>
</Project>
```

### 最小入口

`Core\Modding\ModInitializerAttribute.cs` 表明 Mod 初始化入口仍然是 `[ModInitializer(nameof(Initialize))]`。

```csharp
using System.Reflection;
using BaseLib.Config;
using BaseLib.Patches.Localization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace ExampleMod;

[ModInitializer(nameof(Initialize))]
public static class ExampleModMain
{
    public const string ModId = "ExampleMod";

    public static readonly Harmony Harmony = new(ModId);
    public static readonly Logger Logger = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        // 让 BaseLib 的 SimpleLoc 规则作用到你整个 mod 的本地化文本。
        SimpleLoc.EnableSimpleLoc(ModId);

        // 如果你有自动生成的设置页，初始化时注册。
        ModConfigRegistry.Register(ModId, new ExampleModConfig());

        // 所有需要你自己写的 Harmony 补丁在这里挂上。
        Harmony.PatchAll(Assembly.GetExecutingAssembly());

        // 触发一次类型加载，确保自定义内容被 ModelDb / BaseLib 看到。
        _ = ModelDb.Card<ExampleSlash>();
        _ = ModelDb.Character<ExampleCharacter>();
        _ = ModelDb.Relic<ExampleStarterRelic>();
        _ = ModelDb.Event<ExampleEvent>();

        Logger.Info("ExampleMod initialized.");
    }
}
```

### `PoolAttribute` 与 ID 前缀规则

`BaseLib.Patches.Content.CustomContentDictionary` 和 `PrefixIdPatch` 有两条非常重要的规则：

- 卡牌、遗物、药水这类“池内容”必须用 `[Pool(typeof(SomePoolType))]` 指明自己属于哪个池，否则 BaseLib 不知道把它插到哪里。
- 只要类型实现了 `ICustomModel`，BaseLib 会在 `ModelDb.GetEntry` 阶段自动给 ID 加命名空间前缀；如果你需要固定 ID，可以用 `[CustomID("your_fixed_id")]`。

```csharp
using BaseLib.Utils;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ExampleMod.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class ExampleSlash : BaseLib.Abstracts.CustomCardModel
{
    // ...
}

[CustomID("examplemod.fixed_relic_id")]
public sealed class ExampleStarterRelic : BaseLib.Abstracts.CustomRelicModel
{
    // ...
}
```

## 通用能力

### 本地化：`ILocalizationProvider`、Loc 记录体、`SimpleLoc`

BaseLib 的本地化补丁核心是 `ModelLocPatch`：

- 它会在 `ModelDb.Init` 结束后，把所有实现了 `ILocalizationProvider` 的模型内容写回对应 Loc 表。
- 映射表已经包含 `cards`、`characters`、`encounters`、`monsters`、`orbs`、`potions`、`powers`、`relics`、`enchantments` 等分类。
- 这意味着即使某个 `Custom*Model` 自身没有内置 `Localization` 属性，只要你让派生类额外实现 `ILocalizationProvider`，仍然可以把本地化直接写在类里。

#### 常用 Loc 记录体

| 类型 | 用途 |
| --- | --- |
| `CardLoc` | 卡牌标题与说明。 |
| `CharacterLoc` | 角色标题、代词、商店/营火/结算等一整套角色文本。 |
| `EventLoc` / `EventPageLoc` / `EventOptionLoc` | 事件标题、页面描述、选项标题与选项描述。 |
| `MonsterLoc` | 怪物名称与招式标题。 |
| `EncounterLoc` | 遭遇标题与失败文本。 |
| `PotionLoc` | 药水标题与说明。 |
| `PowerLoc` | 能力标题、普通描述、智能描述。 |
| `RelicLoc` | 遗物标题、描述、Flavor。 |
| `CardModifierLoc` | 附魔 / affliction 标题、描述与额外卡面文本。 |

#### `SimpleLoc` 简写规则

`SimpleLoc.EnableSimpleLoc(ModId)` 开启后，你自己的本地化字符串会自动走简化语法解析：

- `*文本*`：高亮成金色。
- `!D!`：差值显示变量，例如伤害的升级差值。
- `@D@`：反向差值变量。
- `[E?]`：能量图标。
- `-旧文本-+新文本+`：升级前后文本切换。
- 复数规则与若干智能描述语法也在这个补丁里处理。

```csharp
using BaseLib.Abstracts;

public sealed class ExampleLocalizedCard : BaseLib.Abstracts.CustomCardModel
{
    public override List<(string, string)>? Localization => new CardLoc(
        "示例斩击",
        "#造成 !D! 点伤害。 *升级后* -返还 0 点能量-+返还 1 点能量+。"
    );

    public ExampleLocalizedCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }
}
```

### 动态变量与文本：`DynamicVarSource`、计算变量、卡牌构造器

#### `DynamicVarSource`

`DynamicVarSource` 是一个统一入口，作用是把 `CardModel`、`RelicModel`、`PowerModel` 包装成“拥有 `DynamicVars` + `Owner` + 来源对象”的同构数据源。它最适合配合 `CommonActions.Apply<T>` 一类通用函数。

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.Powers;

public static class ExampleApplyHelper
{
    public static async Task ApplyWeakFromCard(Creature target, CardModel card)
    {
        DynamicVarSource source = card;
        await CommonActions.Apply<WeakPower>(target, source);
    }
}
```

#### 额外变量类

| 类型 | 作用 | 什么时候用 |
| --- | --- | --- |
| `CustomCalculatedVar` | 自定义命名的计算变量。 | 同一张卡上想放多个计算变量时。 |
| `CustomCalculatedDamageVar` | 自定义命名的计算伤害变量。 | 一张卡上既有普通伤害又有第二套动态伤害时。 |
| `CustomCalculatedBlockVar` | 自定义命名的计算格挡变量。 | 卡牌、遗物、能力都能用。 |
| `ExhaustiveVar` | 显示“本场战斗还能打几次”。 | 模仿 StS1 `Exhaustive`。 |
| `PersistVar` | 显示“本回合内还能保留几次/生效几次”。 | 需要按回合递减的卡牌效果。 |
| `RefundVar` | 让 BaseLib 的 `RefundPatch` 负责返还能量。 | 卡牌效果依赖能量返还，但你不想自己补丁。 |

#### `ConstructedCardModel` 的成员含义

`ConstructedCardModel` 适合“我要快速堆一个有动态变量、关键字、提示和升级逻辑的教程卡”，而不是手写 `CanonicalVars` / `CanonicalKeywords` / `ExtraHoverTips` 全家桶。

| 成员 | 作用 | 在游戏里的读取时机 |
| --- | --- | --- |
| `WithVars` | 手工塞任意 `DynamicVar`。 | 文本生成、预览、数值计算。 |
| `WithVar` | 塞一个普通命名变量。 | 同上。 |
| `WithBlock` | 生成 `BlockVar`。 | 卡面文本、格挡判定。 |
| `WithDamage` | 生成 `DamageVar`。 | 卡面文本、伤害判定。 |
| `WithCards` | 生成 `CardsVar`。 | 抽牌、生成卡牌说明。 |
| `WithEnergy` | 生成 `EnergyVar`，并自动补能量 tooltip。 | 说明文本、tooltip。 |
| `WithPower<T>` | 生成 `PowerVar<T>`，并自动补能力 tooltip。 | 说明文本、tooltip、`CommonActions.Apply<T>`。 |
| `WithTags` | 添加卡牌标签。 | 卡池过滤、遗物/能力联动。 |
| `WithCalculatedVar` | 通用计算变量。 | 预览或目标变化时会重新计算。 |
| `WithCalculatedDamage` | 计算伤害变量。 | 目标改变时刷新伤害预览。 |
| `WithCalculatedBlock` | 计算格挡变量。 | 目标改变时刷新格挡预览。 |
| `WithKeywords` / `WithKeyword` | 添加关键字，并可配置升级时增删。 | 文本渲染、图鉴、检索。 |
| `WithCostUpgradeBy` | 指定升级后的能量变化。 | 升级时。 |
| `WithTip` / `WithEnergyTip` | 手动添加 hover tip。 | 卡牌悬停时。 |
| `ConstructedUpgrade` | 应用构造式升级逻辑。 | 你需要在 `OnUpgrade()` 里自己调用。 |

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ExampleMod.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class ExampleConstructedCard : ConstructedCardModel
{
    public override List<(string, string)>? Localization => new CardLoc(
        "示例构筑卡",
        "#造成 !Damage! 点伤害并获得 !Block! 点格挡。若目标生命值不高于 20，额外造成 !Finisher! 点伤害。"
    );

    public ExampleConstructedCard()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
        WithDamage(7, upgrade: 3)
            .WithBlock(6, upgrade: 2)
            .WithCalculatedDamage(
                "Finisher",
                baseVal: 0,
                bonus: (card, target) => target != null && target.CurrentHp <= 20 ? 4 : 0,
                props: ValueProp.Move,
                bonusUpgrade: 2)
            .WithKeyword(CardKeyword.Exhaust, upgradeType: UpgradeType.Remove)
            .WithCostUpgradeBy(-1)
            .WithPower<WeakPower>(1)
            .WithTip(new TooltipSource(_ => HoverTipFactory.ForEnergy()));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        await CommonActions.CardBlock(this, cardPlay);

        if (cardPlay.Target != null && cardPlay.Target.CurrentHp <= 20)
        {
            await DamageCmd.Attack(DynamicVars.GetDynamicVar("Finisher").BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        ConstructedUpgrade();
    }
}
```

### 常用命令封装：`CommonActions`、`DamageCmd`、`CreatureCmd`、`PowerCmd`、`CardPileCmd`、`RewardsCmd`

`CommonActions` 不是必须，但它把 BaseLib 文档里最常见的几种行为写成了稳定入口：

| 方法 | 作用 | 适合什么情况 |
| --- | --- | --- |
| `CommonActions.CardAttack` | 从卡牌当前 `DamageVar` / `CalculatedDamageVar` 自动拼 `DamageCmd.Attack`。 | 单体、随机、多目标攻击卡。 |
| `CommonActions.CardBlock` | 从卡牌的 `BlockVar` 或指定变量获得格挡。 | 技能牌、防御牌。 |
| `CommonActions.Draw` | 读取 `CardsVar` 抽牌。 | `抽 X 张牌` 这类卡。 |
| `CommonActions.Apply<T>` | 从卡牌 / 能力 / 遗物的 `PowerVar<T>` 套用能力。 | “施加虚弱/易伤/力量”最省心。 |
| `CommonActions.ApplySelf<T>` | 对自己上能力。 | Buff 类卡牌。 |
| `CommonActions.SelectCards` | 打开一个简单卡牌选择网格。 | 弃牌、回收、检索。 |

源自 `Core` 的底层命令也要记住几个：

- `DamageCmd.Attack(decimal)`：构造攻击命令。
- `CreatureCmd.GainBlock(...)`：给生物获得格挡。
- `CreatureCmd.Heal(...)`：治疗。
- `PowerCmd.Apply<T>(...)`：施加能力。
- `PowerCmd.Remove(...)`：移除能力。
- `CardPileCmd.Draw(...)`：抽牌。
- `CardPileCmd.Add(...)`：把牌移动到某个牌堆。
- `CardSelectCmd.FromSimpleGrid(...)`：基础选牌界面。
- `RewardsCmd.OfferCustom(...)`：直接弹一个奖励屏。

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;

public static class ExampleCommandRecipes
{
    public static async Task PlayAttackAndDebuff(CardModel card, CardPlay cardPlay, PlayerChoiceContext context)
    {
        await CommonActions.CardAttack(card, cardPlay).Execute(context);

        if (cardPlay.Target != null)
        {
            await PowerCmd.Apply<WeakPower>(cardPlay.Target, 2, card.Owner.Creature, card, false);
        }
    }

    public static async Task OfferBonusRewards(Player player)
    {
        List<Reward> rewards =
        [
            new GoldReward(30, player),
            new PotionReward(ModelDb.Potion<EnergyPotion>().ToMutable(), player),
            new RelicReward(ModelDb.Relic<Anchor>().ToMutable(), player)
        ];

        await RewardsCmd.OfferCustom(player, rewards);
    }
}
```

### 资源、UI、场景：`ISceneConversions`、`NodeFactory`、自动场景转换

`BaseLib-StS2\docs\auto_conversion.md` 和 `SceneConversionPatch.cs` 已经把机制写得很清楚：BaseLib patch 的不是 `Instantiate<T>()`，而是非泛型 `PackedScene.Instantiate(GenEditState)`。这让你可以在 Godot 编辑器里用普通 `Node2D` / `Control` 做根节点，再在运行时自动转成 `NCreatureVisuals`、`NEnergyCounter` 等游戏节点类型。

#### 什么时候该用 `CreateCustomVisuals()`，什么时候该用 `CustomVisualPath`

| 方式 | 适合场景 |
| --- | --- |
| `CustomVisualPath` / `CustomEnergyCounterPath` | 你已经有 `.tscn` 资源，希望 BaseLib 负责自动转换。 |
| `CreateCustomVisuals()` / `CreateCustomSprite()` / `CustomIcon` | 你想在代码里直接拼一个节点树，而不是从磁盘场景读。 |

#### `NodeFactory` 的常见用法

```csharp
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Nodes.Combat;

public static class ExampleSceneRegistration
{
    public static void RegisterManualSceneConversions()
    {
        NodeFactory.RegisterSceneType<NCreatureVisuals>(
            "res://ExampleMod/scenes/monsters/example_support_drone.tscn");
    }

    public static NCreatureVisuals CreateVisualsNow()
    {
        return NodeFactory<NCreatureVisuals>.CreateFromScene(
            "res://ExampleMod/scenes/monsters/example_support_drone.tscn");
    }
}
```

### 配置与设置：`ModConfig`、`SimpleModConfig`、配置特性

BaseLib 已经在 `ModConfigPatch.cs` 里把配置入口打进：

- 主菜单 `NMainMenu`
- 设置界面 `NSettingsScreen`
- 自己的 `NModConfigSubmenu`

所以你的任务只剩下三件事：

- 继承 `SimpleModConfig` 或 `ModConfig`。
- 用特性描述 UI。
- 初始化时 `ModConfigRegistry.Register(ModId, new YourConfig())`。

#### 常用配置特性

| 特性 | 作用 |
| --- | --- |
| `ConfigSection` | 新建一个分区标题。 |
| `SliderRange` / `SliderLabelFormat` | 数值滑条范围与显示格式。 |
| `ConfigHoverTip` / `HoverTipsByDefault` | 给设置项挂 tooltip。 |
| `ConfigTextInput` | 字符串输入校验。 |
| `ConfigButton` | 把方法渲染成按钮。 |
| `ConfigVisibleIf` | 条件显示。 |
| `ConfigColorPicker` | 颜色选择器。 |
| `ConfigHideInUI` | 读写配置但不自动生成 UI。 |
| `ConfigIgnore` | 完全忽略这个属性。 |

```csharp
using BaseLib.Config;
using Godot;

namespace ExampleMod.Config;

[HoverTipsByDefault]
internal sealed class ExampleModConfig : SimpleModConfig
{
    [ConfigSection("General")]
    public static bool EnableBonusReward { get; set; } = true;

    [SliderRange(0, 100)]
    [SliderLabelFormat("{0}%")]
    [ConfigVisibleIf(nameof(EnableBonusReward))]
    public static int BonusGoldPercent { get; set; } = 25;

    [ConfigTextInput(TextInputPreset.SafeDisplayName, MaxLength = 24)]
    public static string DisplayName { get; set; } = "Example Mod";

    [ConfigColorPicker(EditAlpha = false)]
    public static Color AccentColor { get; set; } = new("4EC9B0");

    [ConfigHideInUI]
    public static int InternalStatsCounter { get; set; }

    [ConfigButton("RESET_INTERNAL_STATS", Color = "#b03f3f")]
    private void ResetInternalStats()
    {
        InternalStatsCounter = 0;
        SaveDebounced();
    }
}
```

### 状态存储：`SpireField`、`SavedSpireField`

#### `SpireField<TKey, TValue>`

`SpireField` 本质上是对 `ConditionalWeakTable` 的便捷封装，适合“只在运行时临时挂点额外状态”。它不会自动进存档。

#### `SavedSpireField<TKey, TValue>`

`SavedSpireField` 会通过 `SavedSpireFieldPatch` 自动接入 `SavedProperties.FromInternal` / `FillInternal`，支持的值类型在源码里已经写死：

- `int`
- `bool`
- `string`
- `ModelId`
- `int[]`
- `SerializableCard`
- `SerializableCard[]`
- `List<SerializableCard>`
- 以及 enum / enum 数组

使用规则：

- 字段必须是 `static readonly`，这样 `PostModInitPatch` 扫描类型时才能触发注册。
- 名称要稳定，避免和其他 Mod 冲突。

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace ExampleMod.Relics;

public sealed class ExampleStarterRelic : BaseLib.Abstracts.CustomRelicModel
{
    public static readonly SavedSpireField<ExampleStarterRelic, int> TotalCombatHeals =
        new(() => 0, "TotalCombatHeals");

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;
    public override int DisplayAmount => TotalCombatHeals[this];

    public override List<(string, string)>? Localization => new RelicLoc(
        "示例起始遗物",
        "每次战斗胜利后回复 2 点生命。",
        "它会把所有治疗都记进存档。"
    );

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        TotalCombatHeals[this] += 2;
        await CreatureCmd.Heal(Owner.Creature, 2, playAnim: true);
    }
}
```

### 跨 Mod 互操作：`ModInteropAttribute`、`InteropTargetAttribute`

BaseLib 的互操作补丁在 `PostModInitPatch` 里跑。它做的事情是：

- 检查指定 `modId` 是否加载。
- 如果对方在，就把你这个“空方法 / 空属性 / 包装类”编织成真正的调用桥。
- 如果对方不在，就什么都不做，从而避免硬依赖。

#### 最小静态 API 桥

```csharp
using BaseLib.Utils.ModInterop;

namespace ExampleMod.Interop;

[ModInterop("OtherMod", "OtherMod.Api.EntryPoint")]
public static class OtherModApi
{
    [InteropTarget("RegisterKeyword")]
    public static void RegisterKeyword(string keywordId)
    {
    }
}
```

#### 包装对方实例对象

```csharp
using BaseLib.Utils.ModInterop;

namespace ExampleMod.Interop;

[ModInterop("OtherMod")]
[InteropTarget("OtherMod.Api.BlessingHandle")]
public sealed class BlessingHandle : InteropClassWrapper
{
    public BlessingHandle(string id)
    {
    }

    public void AddStacks(int amount)
    {
    }
}
```

### 音频与视觉增强：`FmodAudio`、治疗修正、血条预测

#### `FmodAudio`

`FmodAudio` 当前源码上被标了 `[Obsolete]`。这不代表不能用，而是表示 BaseLib 作者没有承诺这个 API 的长期稳定性。使用原则：

- 只播现成游戏音效：优先普通游戏音频命令。
- 需要装载自定义 bank、替换现有 FMOD 事件、做 snapshot 或声音池：再用 `FmodAudio`。

它的高频方法包括：

- `PlayEvent`
- `PlayFile`
- `LoadBank`
- `RegisterReplacement`
- `RegisterFileReplacement`
- `RegisterEventReplacement`
- `RemoveReplacement`
- `CreatePool`
- `PlayPool`
- `StartSnapshot`
- `StopSnapshot`
- `EventExists`

```csharp
using BaseLib.Utils;
using System.IO;

public static class ExampleAudio
{
    public static void SetupAudio(string modFolder)
    {
        if (FmodAudio.EventExists("event:/sfx/heal"))
        {
            FmodAudio.PlayEvent("event:/sfx/heal");
        }

        FmodAudio.RegisterFileReplacement(
            "event:/sfx/ui/clicks/ui_click",
            Path.Combine(modFolder, "audio", "example_click.wav"));

        FmodAudio.CreatePool(
            "example_attack_pool",
            Path.Combine(modFolder, "audio", "attack_1.wav"),
            Path.Combine(modFolder, "audio", "attack_2.wav"));
    }
}
```

#### `IHealAmountModifier`

任何实现了 `IHealAmountModifier` 的对象，都可以参与治疗值修正。最常见的载体是 Power 或 Relic。

```csharp
using BaseLib.Abstracts;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

public sealed class ExampleRegenPower : CustomPowerModel, IHealAmountModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new PowerLoc(
        "示例回春",
        "你本回合接下来的治疗额外 +{Amount}。",
        "你本回合接下来的治疗额外 +{Amount}。"
    );

    public decimal ModifyHealAdditive(Creature creature, decimal amount)
    {
        return creature == Owner ? Amount : 0m;
    }
}
```

#### `IHealthBarForecastSource` 与 `HealthBarForecastRegistry`

当前 BaseLib 已经补丁了 `NHealthBar.RefreshForeground` / `RefreshMiddleground` / `RefreshText`。你只要返回预测段，它就会画出来。

```csharp
using BaseLib.Abstracts;
using BaseLib.Hooks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

public sealed class ExampleForecastPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new PowerLoc(
        "示例倒计时",
        "回合结束时失去 {Amount} 点生命。",
        "回合结束时失去 {Amount} 点生命。"
    );

    public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        if (Amount <= 0)
        {
            yield break;
        }

        yield return new HealthBarForecastSegment(
            amount: (int)Amount,
            color: new Color("FF8A5B"),
            direction: HealthBarForecastDirection.FromRight,
            order: HealthBarForecastOrder.ForSideTurnEnd(context.Creature, context.Creature.Side));
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
        {
            return;
        }

        await DamageCmd.Attack(Amount)
            .Targeting(Owner)
            .Execute(choiceContext);
    }
}
```

### 所有模型都共享的 Hook 面：`AbstractModel`

除了各内容类型自己那几个专有属性，绝大部分 Mod 逻辑最终都写在 `AbstractModel` 的 Hook 上。当前源码里已经开放了大量回调，按频率记住下面这些就够用：

| Hook | 典型用途 |
| --- | --- |
| `BeforeCombatStart` / `AfterCombatVictory` / `AfterCombatEnd` | 战斗开始与结束。 |
| `BeforeCardPlayed` / `AfterCardPlayed` / `AfterCardPlayedLate` | 卡牌打出链路。 |
| `AfterCardDrawn` / `AfterCardDiscarded` / `AfterCardExhausted` | 抽牌、弃牌、消耗。 |
| `BeforeDamageReceived` / `AfterDamageReceived` / `AfterDamageGiven` | 伤害前后修正。 |
| `BeforePowerAmountChanged` / `AfterPowerAmountChanged` | 能力层数变化。 |
| `BeforeRewardsOffered` / `AfterRewardTaken` / `AfterModifyingRewards` | 奖励系统。 |
| `AfterMapGenerated` / `BeforeRoomEntered` / `AfterRoomEntered` | 地图与房间流转。 |
| `AfterRestSiteHeal` / `AfterRestSiteSmith` | 营火行为。 |
| `AfterPotionUsed` / `AfterOrbEvoked` | 药水与法球。 |
| `BeforeTurnEnd` / `AfterTurnEnd` / `AfterPlayerTurnStart` | 回合时序。 |

理解这件事很重要：`CustomRelicModel`、`CustomPowerModel`、`CustomSingletonModel`、很多卡牌和附魔虽然 BaseLib 外观不同，但最终都在吃同一套 `AbstractModel` Hook 面。

## 按游戏内容分类

### 卡牌与牌池：`CustomCardModel`、`ConstructedCardModel`、`CustomCardPoolModel`、`ITranscendenceCard`、`CustomPile`

这是 BaseLib 里最成熟的一组抽象。`CustomCardModel` 负责和 `CardModel` 对齐，`ConstructedCardModel` 负责把“动态变量 + 关键词 + 提示 + 升级规则”整理成声明式写法，`CustomCardPoolModel` 负责颜色、能量图标和牌池归属，`CustomPile` 则补上原版没有开放的自定义牌堆。

#### `CustomCardModel`

| 成员 | 游戏里什么时候读取 | 用途 |
| --- | --- | --- |
| 构造器 `CustomCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true, bool autoAdd = true)` | 游戏构建 `ModelDb` 时 | 定义费用、类型、稀有度、目标，以及是否进图鉴、是否自动注册。 |
| `CustomPortraitPath` / `CustomPortrait` | 卡牌立绘加载、图鉴、奖励界面 | 换卡面。`CustomPortrait` 优先级更高。 |
| `CustomFrame` / `CreateCustomFrameMaterial` | 卡牌节点创建时 | 换边框纹理或材质。 |
| `Localization` | BaseLib 的本地化补丁收集内容时 | 直接在类里塞 `CardLoc`。 |
| `CanonicalVars` / `CanonicalKeywords` / `CanonicalTags` | 描述文本生成、战斗计算、图鉴预览 | 定义动态变量、关键字、标签。 |
| `OnPlay` | 卡牌真正打出时 | 这里写战斗逻辑。 |
| `OnUpgrade` | 升级事件结算时 | 手动升级费用、变量、行为。 |

#### `ConstructedCardModel`

`ConstructedCardModel` 最适合教程卡和批量内容。它把高频工作拆成了声明式方法：

| 方法 | 上下文解释 |
| --- | --- |
| `WithDamage` / `WithBlock` / `WithCards` / `WithEnergy` | 定义最常见的伤害、格挡、抽牌、能量变量，并自动挂进描述。 |
| `WithPower<T>` | 定义某个能力层数变量，并自动追加能力提示。 |
| `WithCalculatedDamage` / `WithCalculatedBlock` / `WithCalculatedVar` | 定义依赖战斗状态实时计算的变量，例如“按当前格挡造成伤害”。 |
| `WithKeyword` / `WithKeywords` | 定义卡牌关键字，还可以通过 `UpgradeType.Add` / `Remove` 指定升级时增删。 |
| `WithTip` / `WithEnergyTip` | 手动追加 hover tip。 |
| `WithTags` | 给卡牌打 `Strike`、`Starter`、`Status` 等标签。 |

#### `CustomCardPoolModel`

| 成员 | 游戏里什么时候读取 | 用途 |
| --- | --- | --- |
| `ShaderColor` / `H` / `S` / `V` | 卡牌边框材质生成时 | 快速决定整套职业牌颜色。 |
| `CustomFrame(CustomCardModel card)` | 某张卡需要特殊边框时 | 允许同一职业内做单卡差异化边框。 |
| `BigEnergyIconPath` / `TextEnergyIconPath` | 费用图标、提示、描述文本里能量前缀生成时 | 自定义职业能量图标。 |
| `IsShared` | `ModelDb` 收集共享牌池时 | 诅咒、状态、全职业公共牌池用这个。 |

#### `ITranscendenceCard`

这个接口只有一个方法：

| 方法 | 游戏里什么时候调用 | 用途 |
| --- | --- | --- |
| `GetTranscendenceTransformedCard()` | 超越类机制检查到这张牌需要变形时 | 返回变形后的目标卡。 |

#### 完整示例：一张攻击牌、一张超越形态、一个职业牌池

```csharp
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

public sealed class ExampleCharacterCardPool : CustomCardPoolModel
{
    public override Color ShaderColor => new("5FB6FF");

    // 这两个图标会被卡牌费用、提示里的能量前缀、部分 UI 共用。
    public override string? BigEnergyIconPath => "ExampleMod/images/ui/example_energy_big.png";
    public override string? TextEnergyIconPath => "ExampleMod/images/ui/example_energy_text.png";
}

[Pool(typeof(ExampleCharacterCardPool))]
public sealed class ExampleSlash : ConstructedCardModel, ITranscendenceCard
{
    public ExampleSlash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
        WithBlock(4, 2);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
        WithTags(CardTag.Strike);
    }

    public override List<(string, string)>? Localization => new CardLoc(
        "示例斩击",
        "造成 {Damage} 点伤害。获得 {Block} 点格挡。 NL 消耗。"
    );

    public override string? CustomPortraitPath => "ExampleMod/images/cards/example_slash.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 这里直接绑定 cardPlay.Target，和游戏里“玩家把牌拖到一个敌人身上”是同一个上下文。
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 这里传 cardPlay 是为了让格挡动作保留正确的出牌来源和动画上下文。
        await CommonActions.CardBlock(this, cardPlay);
    }

    public CardModel GetTranscendenceTransformedCard()
    {
        return ModelDb.Card<ExampleTranscendedSlash>().ToMutable();
    }
}

[Pool(typeof(ExampleCharacterCardPool))]
public sealed class ExampleTranscendedSlash : ConstructedCardModel
{
    public ExampleTranscendedSlash() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(18, 4);
        WithBlock(10, 3);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
        WithTags(CardTag.Strike);
    }

    public override List<(string, string)>? Localization => new CardLoc(
        "示例终斩",
        "造成 {Damage} 点伤害。获得 {Block} 点格挡。 NL 消耗。"
    );

    public override string? CustomPortraitPath => "ExampleMod/images/cards/example_transcended_slash.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        await CommonActions.CardBlock(this, cardPlay);
    }
}
```

#### 自定义牌堆：`CustomPile`

`CustomPile` 适合做“保留区”“蓄力区”“符文槽”这种不属于原版抽牌堆/弃牌堆/消耗堆的系统。

| 成员 | 游戏里什么时候调用 | 用途 |
| --- | --- | --- |
| `[CustomEnum] public static PileType Xxx;` | BaseLib 初始化自定义枚举时 | 生成新的 `PileType`。 |
| 无参构造器 `: base(Xxx)` | BaseLib 自动注册自定义牌堆时 | 这是 `CustomEnums` 自动建堆的入口。 |
| `CardShouldBeVisible` | 牌桌刷新此牌堆可见牌时 | 决定牌在这个堆里是否要显示在桌面上。 |
| `GetTargetPosition` | 卡牌飞向该牌堆时 | 决定飞行动画终点。 |
| `GetNCard` | 需要从桌面反查对应卡牌节点时 | 给自定义堆做牌面交互。 |
| `NeedsCustomTransitionVisual` / `CustomTween` | 需要完全自定义入堆表现时 | 自己接管动画。 |

```csharp
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

public sealed class ExampleReservePile : CustomPile
{
    [CustomEnum]
    public static PileType Reserve;

    public ExampleReservePile() : base(Reserve)
    {
    }

    public override bool CardShouldBeVisible(CardModel card)
    {
        // 这个例子让所有进入保留区的牌都继续显示在桌面上，方便做“蓄力展示条”。
        return true;
    }

    public override Vector2 GetTargetPosition(CardModel model, Vector2 size)
    {
        // 这里把保留区放在玩家手牌右上角。
        return new Vector2(1530f, 860f);
    }

    public override NCard? GetNCard(CardModel card)
    {
        return null;
    }
}
```

把牌送进这个牌堆时直接用 `CardPileCmd.Add(card, ExampleReservePile.Reserve, source: this)`；BaseLib 的 `CustomPilePatches` 会把这个新牌堆并到 `PlayerCombatState.AllPiles` 和牌桌位置计算里。

#### 常见坑

- `ConstructedCardModel` 已经帮你处理了大量升级逻辑；如果你又在 `OnUpgrade` 里重复改同一变量，很容易翻倍。
- `CustomPile` 必须有公开无参构造器，否则 `CustomEnums` 自动注册时会直接报错。
- `CustomPortraitPath` 只是换图，不会自动帮你换边框配色；职业色还是要从 `CustomCardPoolModel` 处理。
- `ITranscendenceCard` 返回的应该是 `ToMutable()` 后的牌实例，而不是 canonical model 本体。

### 角色与职业资源：`CustomCharacterModel`、`PlaceholderCharacterModel`

角色是 BaseLib 覆盖最广的一层。`CustomCharacterModel` 负责职业定义，`PlaceholderCharacterModel` 负责“先复用原版资源跑通流程，再逐步替换”的开发路径。

#### `CustomCharacterModel`

| 成员 | 游戏里什么时候读取 | 用途 |
| --- | --- | --- |
| `Localization` | 角色注册与本地化加载时 | 定义角色名、称谓、营火台词、死亡规避文本等整套角色文本。 |
| `HideFromVanillaCharacterSelect` | 原版选人界面构建按钮时 | 只在自定义 UI 里出现，不进原版选人。 |
| `AllowInVanillaRandomCharacterSelect` | 原版随机角色按钮 roll 时 | 控制该角色是否能被随机到。 |
| `CustomVisualPath` / `CreateCustomVisuals` | 战斗中生成角色立绘节点时 | 自定义战斗立绘。 |
| `CustomTrailPath` | 卡牌拖尾生成时 | 自定义卡牌拖尾。 |
| `CustomIconPath` / `CustomIcon` / `CustomIconTexturePath` | 左上角头像、每日、历史记录、存档摘要 | 自定义职业头像。 |
| `CustomEnergyCounterPath` / `CustomEnergyCounter` | 战斗能量条初始化时 | 自定义能量计数器。 |
| `CustomRestSiteAnimPath` / `CustomMerchantAnimPath` | 营火、商店角色动画载入时 | 自定义营火/商店角色表现。 |
| `CustomCharacterSelectBg` / `CustomCharacterSelectIconPath` / `CustomCharacterSelectTransitionPath` | 选人界面绘制时 | 自定义选人背景、图标、切场材质。 |
| `CustomMapMarkerPath` | 地图上角色标记显示时 | 自定义地图行进光标。 |
| `CustomAttackSfx` / `CustomCastSfx` / `CustomDeathSfx` | 角色动作时 | 自定义攻击、施法、死亡音效。 |
| `CardPool` / `RelicPool` / `PotionPool` | 建局、掉落、商店、事件抽取时 | 职业资源来源。 |
| `StartingDeck` / `StartingRelics` / `StartingPotions` | 开局建档时 | 初始构筑。 |
| `StartingHp` / `StartingGold` / `MaxEnergy` / `BaseOrbSlotCount` | 创建玩家实体时 | 基础数值。 |
| `GetArchitectAttackVfx()` | 建筑师事件中播放角色攻击特效时 | 返回该角色可用的攻击特效列表。 |

#### `PlaceholderCharacterModel`

`PlaceholderCharacterModel` 已经帮你把 `ironclad` / `silent` / `defect` 这类原版占位资源路径都铺好了。最适合先做逻辑验证：

- 先继承 `PlaceholderCharacterModel` 并重写 `PlaceholderID => "silent"`。
- 等到牌池、遗物池、事件流程都跑通，再逐个换成你自己的 `CustomVisualPath`、`CustomEnergyCounterPath`、`CustomCharacterSelectBg`。

#### 完整示例：一个可进原版选人的教程角色

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

public sealed class ExampleCharacter : PlaceholderCharacterModel
{
    public override string PlaceholderID => "silent";

    public override int StartingHp => 70;
    public override int StartingGold => 99;
    public override int MaxEnergy => 3;
    public override int BaseOrbSlotCount => 1;

    public override CardPoolModel CardPool => ModelDb.CardPool<ExampleCharacterCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<ExampleRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<ExamplePotionPool>();

    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        ModelDb.Card<ExampleSlash>().ToMutable(),
        ModelDb.Card<ExampleSlash>().ToMutable(),
        ModelDb.Card<ExampleSlash>().ToMutable(),
        ModelDb.Card<ExampleSlash>().ToMutable()
    };

    public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[]
    {
        ModelDb.Relic<ExampleStarterRelic>().ToMutable()
    };

    public override IReadOnlyList<PotionModel> StartingPotions => new PotionModel[]
    {
        ModelDb.Potion<ExampleBurstPotion>().ToMutable()
    };

    public override string? CustomEnergyCounterPath => "ExampleMod/scenes/ui/example_energy_counter.tscn";
    public override string? CustomCharacterSelectBg => "ExampleMod/scenes/char_select/example_character_bg.tscn";
    public override string? CustomMapMarkerPath => "ExampleMod/images/map/example_map_marker.png";

    public override List<(string, string)>? Localization => new CharacterLoc(
        "示例者",
        "示例者",
        "一位专门拿来教学的角色。",
        "他",
        "他",
        "他的",
        "他的",
        "松香",
        "轮到我了。",
        "我还没倒下。",
        "这次先记你一笔。",
        "金币总得花在刀刃上。",
        "示例修正",
        "你的部分示例内容会获得额外说明。"
    );
}
```

### 遗物与遗物池：`CustomRelicModel`、`CustomRelicPoolModel`

`CustomRelicModel` 自己只新增了 `Localization` 和 `GetUpgradeReplacement`，但它背后挂的是完整的 `RelicModel` 行为面，所以你真正要写的是“哪些继承属性在流程里被读”。

#### `CustomRelicModel` + `RelicModel`

| 成员 | 游戏里什么时候读取/调用 | 用途 |
| --- | --- | --- |
| `Localization` | 本地化注册时 | 推荐直接返回 `RelicLoc`。 |
| `GetUpgradeReplacement()` | 游戏尝试把遗物升级成另一个遗物时 | 返回替换后的 relic canonical model。 |
| `MerchantCost` | 商店生成该遗物售价时 | 改价格。 |
| `ShowCounter` / `DisplayAmount` | 遗物 UI 刷新时 | 显示数字计数。 |
| `SpawnsPets` | 游戏判断这件遗物是否会召唤宠物时 | 影响部分战斗和存档逻辑。 |
| `ExtraHoverTips` | 鼠标悬停遗物时 | 添加关联卡牌/能力提示。 |
| `AfterCombatVictory` / `BeforeCombatStart` / `AfterRestSiteHeal` 等 Hook | 对应流程触发时 | 绝大多数遗物效果都写在这些继承 Hook 里。 |

#### `CustomRelicPoolModel`

| 成员 | 游戏里什么时候读取 | 用途 |
| --- | --- | --- |
| `IsShared` | 共享遗物池收集时 | 跨职业池。 |
| `BigEnergyIconPath` / `TextEnergyIconPath` | 遗物描述里的能量前缀或相关 UI | 如果遗物池需要独立能量色，可以在这里配。 |

#### 完整示例：带存档字段、可升级、会召唤宠物的起始遗物

```csharp
using BaseLib.Abstracts;
using BaseLib.Fields;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

public sealed class ExampleRelicPool : CustomRelicPoolModel
{
}

public sealed class ExampleStarterRelic : CustomRelicModel
{
    public static readonly SavedSpireField<Player, bool> HasSummonedTrainingPet = new("example_has_summoned_pet", () => false);

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Amount;
    public override int MerchantCost => 120;
    public override bool SpawnsPets => true;

    public override List<(string, string)>? Localization => new RelicLoc(
        "示例徽章",
        "每次战斗胜利后，计数 +1。首次进入战斗时召唤一个教学宠物。",
        "适合拿来演示存档字段和遗物替换。"
    );

    public override RelicModel? GetUpgradeReplacement()
    {
        return ModelDb.Relic<ExampleUpgradedRelic>();
    }

    public override async Task BeforeCombatStart(PlayerChoiceContext choiceContext)
    {
        if (HasSummonedTrainingPet.Get(Owner))
        {
            return;
        }

        // 这里直接走原版 PlayerCmd.AddPet，BaseLib 不需要额外补丁。
        await PlayerCmd.AddPet<ExampleTrainingPet>(Owner);
        HasSummonedTrainingPet.Set(Owner, true);
    }

    public override Task AfterCombatVictory(AbstractRoom room)
    {
        Amount += 1;
        return Task.CompletedTask;
    }
}

public sealed class ExampleUpgradedRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Amount;

    public override List<(string, string)>? Localization => new RelicLoc(
        "示例徽章+",
        "每次战斗胜利后，计数 +2。",
        "给“遗物升级替换”用的目标形态。"
    );

    public override Task AfterCombatVictory(AbstractRoom room)
    {
        Amount += 2;
        return Task.CompletedTask;
    }
}
```

### 药水与药水池：`CustomPotionModel`、`CustomPotionPoolModel`

药水层的 BaseLib 扩展重点在图标资源和本地化，行为本体还是走 `PotionModel`。

#### `CustomPotionModel`

| 成员 | 游戏里什么时候读取/调用 | 用途 |
| --- | --- | --- |
| `CustomPackedImagePath` / `CustomPackedOutlinePath` | 药水图标、背包、掉落奖励显示时 | 自定义药水主图和描边。 |
| `Localization` | 本地化加载时 | 推荐直接返回 `PotionLoc`。 |
| `Rarity` / `Usage` / `TargetType` | 掉落池筛选、使用时选目标、背包展示时 | 药水基础行为。 |
| `ExtraHoverTips` | 鼠标悬停药水时 | 添加关联卡牌/能力说明。 |
| `Use(PlayerChoiceContext context, Creature? target)` | 玩家点击药水并选完目标时 | 药水实际效果。 |

#### `CustomPotionPoolModel`

和牌池/遗物池一样，主要负责共享池和能量图标。

#### 完整示例：一个单体攻击兼上弱的药水

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

public sealed class ExamplePotionPool : CustomPotionPoolModel
{
}

public sealed class ExampleBurstPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    public override string? CustomPackedImagePath => "ExampleMod/images/potions/example_burst.tres";
    public override string? CustomPackedOutlinePath => "ExampleMod/images/potions/example_burst_outline.tres";

    public override List<(string, string)>? Localization => new PotionLoc(
        "示例爆裂药",
        "对一名敌人造成 8 点伤害，并施加 1 层虚弱。"
    );

    public override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<WeakPower>()
    };

    public override async Task Use(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target == null)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, target, 8m, ValueProp.Move, Owner.Creature);
        await PowerCmd.Apply<WeakPower>(target, 1m, Owner.Creature, null, false);
    }
}
```

#### 常见坑

- `CustomPotionModel` 只帮你改资源路径，不会替你自动把药水塞进某个角色池里；归属还是看 `PotionPool` 和 `[Pool]`/内容拼接流程。
- 药水如果要附带额外说明，优先重写 `ExtraHoverTips`，不要硬把提示文字塞进主描述里。
- 遗物如果要跨战斗记状态，尽量用 `SavedSpireField`；否则只用普通字段会在重新进房或读档时丢失。
