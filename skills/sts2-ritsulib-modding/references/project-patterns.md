# Project Patterns

## STS2_WineFox-main 的可复用模式

`D:\MOD\杀戮尖塔2mod制作\STS2_WineFox-main` 是一个高价值的 RitsuLib 实战案例。它最值得复用的是组织方式，不是具体业务逻辑。

### 入口模式

入口在 `Main.cs`，关键初始化顺序是：

1. `RitsuLibFramework.CreateLogger(...)`
2. `RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger)`
3. 必要的 runtime patcher / 生命周期订阅
4. `ModTypeDiscoveryHub.RegisterModAssembly(Const.ModId, assembly)`
5. 额外内容注册与默认表初始化

结论：

- RitsuLib 项目入口不要只保留 Harmony。
- AutoRegistration 依赖显式的 `RegisterModAssembly(...)`。

### 目录分层

WineFox 采用按内容域拆分的目录结构，典型目录包括：

- `Cards`
- `Character`
- `Commands`
- `Content`
- `Enchantments`
- `Events`
- `Hooks`
- `Patches`
- `Powers`
- `Relics`
- `Potions`

适合内容量大的角色或整包 mod。它的优点是：

- 注册入口与内容实现分离
- 关键词、角色资产、命令、Power、Relic 各自有独立落点
- Token / Craft / Starter 这类子域可以继续按目录细分

### 卡牌模式

- 大量卡牌直接使用 `[RegisterCard(typeof(WineFoxCardPool))]`
- Token / Crafting 卡会切到专门 pool
- 说明 RitsuLib 很适合“按 pool + 特性”组织内容，而不是靠手写总表

### 内容组织模式

- `Content` 目录用于放自动注册关键词、角色资产描述符、共享注册逻辑
- `Commands` 目录承载复杂玩法指令与战斗内流程
- `Patches` 只保留 RitsuLib 没直接覆盖的行为

这个模式适合：

- 自定义角色
- 玩法系统较重
- 有多类内容共同协作

## 当前仓库 re-astral-party-mod 的已知模式

当前仓库 `B:\Documents\re-astral-party-mod` 已经是 RitsuLib 前置项目。

### 依赖声明

`ReAstralPartyMod.csproj`：

- 引用了 `sts2.dll`
- 引用了 `0Harmony.dll`
- 引用了 `STS2-RitsuLib.dll`

`ReAstralPartyMod.json`：

- `dependencies` 已包含 `STS2-RitsuLib`

### 入口模式

入口在 `Scripts/MainFile.cs`，关键顺序是：

1. `[ModInitializer(nameof(Init))]`
2. `RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger)`
3. 预热 `SavedProperty` 类型缓存
4. 手动调用 `AstralKeywords.RegisterAll()`
5. `ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly)`
6. 仅对 RitsuLib 未覆盖的运行时行为调用 `Harmony.PatchAll(assembly)`

这说明当前仓库采用的是：

- RitsuLib 负责内容注册
- Harmony 只补运行时行为
- 关键词有一部分先手动注册，再做程序集自动发现

### 目录形态

当前仓库的内容代码主要位于：

- `ReAstralPartyCardCode\Cards`
- `ReAstralPartyCardCode\Relics`
- `ReAstralPartyCardCode\Powers`
- `ReAstralPartyCardCode\Potions`
- `ReAstralPartyCardCode\Events`
- `ReAstralPartyCardCode\Keywords`
- `ReAstralPartyCardCode\Modifiers`
- `ReAstralPartyCardCode\RestSite`
- `ReAstralPartyCardCode\Utils`
- `ReAstralPartyCardCode\Patches`

资源位于：

- `ReAstralPartyMod\images`
- `ReAstralPartyMod\localization`

结论：

- 继续开发当前仓库时，优先延续它的 `ReAstralPartyCardCode` + `ReAstralPartyMod` 双层分离。
- 不要为了模仿 WineFox，把现有目录强行重构成 `Commands` / `Content` / `Character` 风格，除非用户明确要求。

## 何时参考哪个项目

### 优先参考当前仓库

当任务是：

- 继续给 `re-astral-party-mod` 加内容
- 修这个仓库的注册、依赖、关键词、SavedProperty、Harmony patch
- 对齐这个仓库的图片、本地化、路径和命名

### 优先参考 WineFox

当任务是：

- 设计一个新的 RitsuLib 项目结构
- 不确定某类内容在完整项目里该放哪
- 需要看 RitsuLib 在大型 mod 中如何组织卡牌、角色池、关键词、命令、事件、Powers、Relics

### 再看 RitsuLib code

当任务是：

- 需要确认真实 API 名字或签名
- 文档写法和项目案例不一致
- 怀疑某个特性或 registry 的行为边界
