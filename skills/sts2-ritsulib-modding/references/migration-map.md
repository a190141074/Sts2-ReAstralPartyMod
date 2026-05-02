# Migration Map

本文件提炼自 `B:\Documents\re-astral-party-mod\doc\从BaseLib 到 RitsuLib.md`，用于快速把 BaseLib 写法迁到 RitsuLib。

## 1. 初始化与内容发现

### BaseLib

- 依赖属性与框架默认扫描。

### RitsuLib

- 在 `ModInitializer` 入口里显式调用：
  - `RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger)`
  - `ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly)`

如果没做这一步，AutoRegistration 特性通常不会生效。

## 2. 注册特性

| 场景 | BaseLib | RitsuLib |
| --- | --- | --- |
| 卡牌注册 | `[Pool(typeof(TestCardPool))]` | `[RegisterCard(typeof(TestCardPool))]` |
| 遗物注册 | `[Pool(typeof(TestRelicPool))]` | `[RegisterRelic(typeof(TestRelicPool))]` |
| 药水注册 | `[Pool(typeof(TestPotionPool))]` | `[RegisterPotion(typeof(TestPotionPool))]` |
| 事件注册 | 常见为手动/旧框架写法 | `[RegisterSharedEvent]` / `[RegisterActEvent(typeof(...))]` |
| 先古之民 | 常见为手动/旧框架写法 | `[RegisterSharedAncient]` / `[RegisterActAncient(typeof(...))]` |

## 3. 基类替换

| 内容 | BaseLib | RitsuLib |
| --- | --- | --- |
| 卡牌 | `CustomCardModel` | `ModCardTemplate` |
| 遗物 | `CustomRelicModel` | `ModRelicTemplate` |
| 药水 | `CustomPotionModel` | `ModPotionTemplate` |
| 能力 | `CustomPowerModel` | `ModPowerTemplate` |
| 附魔 | `CustomEnchantmentModel` | `ModEnchantmentTemplate` |
| 遭遇 | `CustomEncounterModel` | `ModEncounterTemplate` |
| 先古之民 | `CustomAncientModel` | `ModAncientEventTemplate` |
| 角色 | `PlaceholderCharacterModel` | `ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>` |

## 4. ID 规则

### BaseLib

- 常见格式类似：`命名空间前缀-原始ID`

### RitsuLib

- 常见格式类似：`{ModId}_{类别}_{原始ID}`

不要把旧 BaseLib 的 ID 拼接习惯直接搬过来。迁移时优先查当前项目或 RitsuLib 实际 registry 约定。

## 5. 关键词与动态变量

| 场景 | BaseLib | RitsuLib |
| --- | --- | --- |
| 附加 HoverTip | `ExtraHoverTips` | `AdditionalHoverTips` |
| 关键词集合 | `CanonicalKeywords` | `RegisteredKeywordIds` |
| 关键词 HoverTip | `HoverTipFactory.FromKeyword(...)` | `ModKeywordRegistry.CreateHoverTip(...)` |
| 关键词声明 | `[CustomEnum("UNIQUE")]` | `[RegisterOwnedCardKeyword("Unique", IconPath = "...")]` |
| 动态变量 Tooltip | `.WithTooltip` | `.WithSharedTooltip` |

关键词相关迁移时，优先再看：

- `RitsuLib-code\Docs\zh\LocalizationAndKeywords.md`
- `RitsuLib-code\Keywords\ModKeywordRegistry.cs`

## 6. 角色、卡池、starter 内容

| 场景 | BaseLib | RitsuLib |
| --- | --- | --- |
| 角色 visuals 路径 | `CustomVisualPath` | `CustomVisualsPath` |
| 角色选择背景 | `CustomCharacterSelectBg` | `CustomCharacterSelectBgPath` |
| 初始卡组 | `StartingDeck` | `StartingDeckEntries` 或 `[RegisterCharacterStarterCard]` |
| 初始遗物 | `StartingRelics` | `StartingRelicTypes` 或 `[RegisterCharacterStarterRelic]` |
| 角色池绑定 | `CardPool` / `RelicPool` / `PotionPool` | `ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>` |
| 卡池 | `CustomCardPoolModel` | `TypeListCardPoolModel` |
| 遗物池 | `CustomRelicPoolModel` | `TypeListRelicPoolModel` |
| 药水池 | `CustomPotionPoolModel` | `TypeListPotionPoolModel` |

## 7. 事件、遭遇、先古之民

| 场景 | BaseLib | RitsuLib |
| --- | --- | --- |
| 遭遇章节条件 | `IsValidForAct(ActModel act)` | `[RegisterActEncounter(typeof(...))]` |
| 遭遇房间类型 | `base(RoomType.Monster)` | `public override RoomType RoomType => RoomType.Monster` |
| 遭遇场景路径 | `CustomScenePath` | `CustomEncounterScenePath` |
| 先古之民背景路径 | `CustomScenePath` | `CustomBackgroundScenePath` |
| 先古之民出现条件 | `IsValidForAct(ActModel act)` | `IsAllowed(IRunState runState)` |
| 先古之民选项池 | `MakeOptionPools` | `AllPossibleOptions` + `GenerateInitialOptions()` |

## 8. 升级/替换映射

### BaseLib

- 卡牌升阶常通过接口实现。
- 遗物替换常通过 override 返回新 relic。

### RitsuLib

- 可以使用注册特性：
  - `[RegisterArchaicToothTranscendence(typeof(TargetCard))]`
  - `[RegisterTouchOfOrobasRefinement(typeof(TargetRelic))]`
- 或者在 `Init()` 里显式注册：
  - `RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<TFrom, TTo>()`
  - `RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<TFrom, TTo>()`

## 9. 场景与 Godot 脚本

BaseLib 对部分场景转换更自动。

RitsuLib 仍然要求你在入口保留：

```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
```

如果自定义场景脚本失效，先排查这个，而不是先怀疑内容注册。

## 10. 迁移时的默认检查清单

1. 入口是否切成 RitsuLib 初始化方式
2. `[Pool]` 是否替换成对应 `Register...` 特性
3. `Custom...Model` 是否替换成 `Mod...Template`
4. starter 内容与 pool 绑定是否迁移
5. 关键词/动态变量接口是否替换
6. 事件/先古之民/升级映射是否换成 RitsuLib 形式
7. manifest 和 `csproj` 是否补齐 `STS2-RitsuLib`
