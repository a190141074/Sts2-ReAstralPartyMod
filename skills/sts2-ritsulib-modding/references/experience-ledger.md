# RitsuLib 经验账本（强制阅读）

这些条目来自已经发生过的反复错误。它们不是泛化建议：匹配任务时必须逐条核对，
并且不能用一次新案例覆盖旧条目。若新代码与账本冲突，先停下来取证，再更新实现或
明确记录一个局部例外。

## 启动、注册与版本

- “内容不出现”先查启动和注册链，再查业务逻辑：manifest、`min_game_version`、依赖
  对象写法与 `min_version`、`[ModInitializer]`、`EnsureGodotScriptsRegistered`、
  `ModTypeDiscoveryHub.RegisterModAssembly`、Harmony 初始化、实际加载 DLL/PCK。
- 看到 compat 目录名不能证明游戏正在运行该版本。用最新 `release_info.json`、带时间戳
  日志和实际加载路径确认 runtime；不要把游戏版本目录称成 RitsuLib 版本。
- `sts2.dll`、`sts2.xml`、`STS2-RitsuLib.dll`、`STS2-RitsuLib.xml` 必须成套核对。签名、
  参数名、重载、继承关系不确定时停止猜测，查 XML、反射或反编译。
- 如果主 DLL 已产出而 manifest generation/导出后处理失败，分开报告两件事；不要把后处理
  错误误判为 API 代码未编过。Compat 构建要隔离 intermediate/output，避免递归导出。

## Hook、生命周期与模型边界

- 先判定目标是玩家自己的回合还是整个 side turn、单对象还是横切观察、模型 override 还是
  lifecycle event，再选最窄入口。不要凭感觉选择最宽的 Harmony patch。
- 对原生 patch，参数名也属于契约；例如 `choiceContext` 不能凭旧代码写成 `context`。
- 依赖本地化、UI 节点或延迟初始化的链要区分 required patch 与 deferred patch，并绑定真实
  初始化条件。
- 能用现有 base/helper 就不要重写框架；复杂多阶段流程用窄的 `...ExecutionContext`/
  `...ResolveContext` 收口，不要为了一个功能造全局大抽象。

## 联机、持久化与随机

- **联机状态不能沿用单机命令入口**：修改任何多人局状态前，检查 Hook 是否提供 `PlayerChoiceContext`、实现是否丢弃 context，以及 `PowerCmdCompat` 等兼容层是否用 `ThrowingPlayerChoiceContext` 代替了真实联机上下文。无上下文 Hook（例如 `AfterEnergySpent(CardModel, int)`）触发的命令必须证明由谁执行、如何复制到其他端；不得假定卡牌播放同步就会同步 Hook 副作用。日志堆栈不是 API 契约，当前 `sts2.dll`/XML 和项目编译引用才是。
- 大厅/角色选择不要继续依赖旧的 `StartRunLobby.LocalPlayer` 面；优先用
  `lobby.NetService.NetId` 和 `lobby.Players`，把本地玩家/房主/客户端判断收口到 helper。
- `SavedAttachedState`、反射枚举字段、type discovery 遇到过时 multiplayer 类型时要容忍
  类型加载失败并记录 owner、成员名和失败类型，不能让一条旧引用炸掉整条 discovery 链。
- 联机状态使用一份权威的持久化/同步状态；按玩家同步资格与最终值，不能只同步全局完成布尔值。
  命令链要稳定排序，调用会改变集合的命令前先快照集合。
- 语义为“每次触发重新随机”时，hash 必须包含跨触发单调推进且各端一致的事件序号；不能只
  使用每回合重置的计数。失败尝试也要推进 sequence。
- 跨存档、联机或显示稳定的概率/倍率/百分比优先用稳定整型刻度，不默认持久化 `float`/
  `decimal`。

## 内容、附魔与本地化

- 先找仓库内完整 working example，对齐初始化链、注册链、真实 patch 目标、运行日志和资源，
  不能只因目录或命名相似就照抄。
- 卡牌/遗物/能力/事件要一起审计注册、canonical id、资源路径、三语 key、生成/变换路径、
  hover 图和共享观察者；删除旧机制要全仓搜索代码、文案、文档、资源和生成描述。
- 附魔模型自身必须用 `CanEnchantCardType` 收死牌型，再用 `CanEnchant` 补关键词和已有状态；
  不要只在调用点 filter，也不要只写 `base.CanEnchant(card)`。
- 玩家给出的原句是硬边界。不要擅自改数字、时机、范围、句序、标签、占位符或换行；实现
  备注、内部 id、兼容来源不能进入玩家文案。
- 标准关键词（如 Exhaust、Ethereal、Retain）由 UI 自动显示，除非用户明确要求，不要在描述
  中重复。数字不要手动着色；命名效果统一沿用既有标签。
- `smartDescription` 有变量时先确认当前 UI 是否显式注值；canonical 模型无 Owner/CombatState
  也必须能安全生成标题、描述、hover 和注册信息。
- 新增内容先做最小可运行切片：入口、依赖、一个内容类型、本地化和资源路径；确认注册与加载后再扩展复杂联动。
- 新写或重写中文时以当前仓库和本地游戏资源的既有术语为准；短句直述玩家结果，保留变量、动态图标和条件模板结构，不为了润色改坏占位符。
- 标题、设置项、悬浮提示和图鉴说明面向玩家写，短而直接；事件叙事可以有风格，但选项必须先让代价和收益可辨识。

## 反复失败模式

### ReAstralParty 手电筒：2026-10-01 漏查 RitsuLib 额外角标教程

- 修改位置：`TokenPurpleFlashlightStronglight`、`TokenGoldFlashlightFlashburst` 及已删除的 `FlashlightDamageBadgePatch`。原错误：只核对原生 `NRelicInventoryHolder` 的单计数槽与仓库例子，没有按已有 source-map 先读 RitsuLib doc，便复制原生数字节点并新增 Harmony patch；违反了现有前置功能优先的规则。
- 依据：`RitsuLib-doc\RitsuLib\02 - 玩法基底\07 - 额外角标\README.md` 已有遗物角标示例；当前项目引用的 `STS2-RitsuLib.Runtime.dll` / `.xml` 确认存在 `IRelicExtraIconAmountLabelSpecsProvider`、`ExtraIconAmountLabelSpec.Plain` 和 `TopRight`，RitsuLib 自行处理刷新与退出清理。仅搜索较小的 `STS2-RitsuLib.xml` 不能证明功能缺失。
- 修正：两件手电筒直接实现 provider，右上角实时显示当前增伤；删除原 UI patch、注册项和 `.uid`。永恒星光增加时通过两件遗物的 `DisplayAmountChanged` 通知刷新，canonical/无 Owner 查询返回零，不另存派生值。
- 机制核对：最终用户确认紫手电每 4 张、金手电每 3 张不低于 1 费的攻击牌触发；主计数分别循环 0–3、0–2。显示样式一致不能推导触发阈值一致，按每件最终确认的文案核对代码、三语与生成说明；增伤继续按当前永恒星光 / 14、/ 10 向下取整。
- 下次修改前自检：先读角标教程，再核对当前实际引用的 Runtime 接口和仓库同类实现；确认主计数与派生角标各自语义、canonical 安全、跨模型刷新来源和生命周期。不因原生限制或仓库无例子而跳过 RitsuLib 现成功能。
- 验证边界：provider 与动态增伤的针对性程序集检查、计数修正检查及 0.6.34 构建导出已通过；角标实机布局、真实星光应用和双人 checksum 尚未验证，不能把这些检查写成联机实测通过。

### ReAstralParty 电流剑：2026-10-01 非法 SavedProperty 类型

- 修改位置：`ReAstralPartyCardCode/Relics/ChargeTokenRelics.cs` 的 `TokenBlueElectricBlade.AstralParty_ElectricBladeStrengthBonus`。原错误：把 `PowerCmd.Apply<StrengthPower>` 接收的 `decimal` 数值类型照搬为 `[SavedProperty] decimal`，没有先检查持久化类型契约，也没有对照同文件 `TokenBlueEnergyRecycler` 的 `[SavedProperty] int`。这不是电流剑机制要求，而是混淆了命令参数与存档字段。
- 依据：`logs/BugReport-2026_10_01_02_03_12/logs/godot.log` 在添加电流剑后明确报 `Property AstralParty_ElectricBladeStrengthBonus ... is not a valid type for [SavedProperty] (type System.Decimal)`，堆栈进入 `NetFullCombatState.FromRun` 和 checksum；`Scripts/SavedPropertyGovernance.cs` 已将 `decimal` 判为不支持类型，且启动时已输出该字段的 `unsupported_type` 警告。不能以编译通过或力量命令接受 `decimal` 反推存档合法。
- 修正：该字段只记录电流剑已经施加的整数力量差额，改为 `[SavedProperty] int`；施加力量时再由命令参数类型进行转换。保留原 `PlayerChoiceContext`。此修正已构建导出，但双人联机的攻击牌收尾、充能与力量最终值仍需实机确认。
- 下次修改前自检：新增或改动任何 `[SavedProperty]` 时，逐项对照同类模型字段、当前游戏支持的序列化类型及 `SavedPropertyGovernance` 告警；明确区分业务计算/命令参数类型和持久化字段类型。若已有 `unsupported_type` 告警，先修该告警再进行联机复测，不得把后续卡牌悬空归因于未证实的播放 Hook。仓库中其余 `[SavedProperty] decimal` 须按各自完整调用链单独核查，不凭本次一条日志宣称其行为已验证。
- 同次复查发现 `TokenPurpleSharpeningStone` 的两个 `[SavedProperty] decimal` 也属于同一错误类别：它们保存的力量/反转鳞片状态是离散整数，已改为 `int`；命令调用仍使用 `decimal` 参数。后续不得只修日志中第一个字段后停止全量搜索。

### ReAstralParty 人格选择：2026-09-28 Neow 收尾同步

- 修改位置：`ReAstralPartyCardCode/Online/StartingPersonNeowReadyFlow.cs` 的 `RunSelectionTaskAsync`。原错误/风险：人格选择完成后调用 `SetInitialEventState(false)` 重建每名玩家的 Neow 选项，可能使不同端额外推进随机流。依据：`godot (7).log` 首次分歧在退出 `EVENT.NEOW`，主机与客户端 `Niche` 计数为 6/0；同文件的 `RestoreOriginalOptionsOrFinish` 已有缓存恢复链。修正：完成后统一恢复准备页注入前缓存的选项和描述，不重新生成。下次修改前自检：逐端追踪准备页缓存、结束恢复和 Neow 退出时的 RNG 计数；未经联机实测，不把计数差异直接写成已证实的根因。
- 修改位置：同文件的 `RestoreOriginalOptionsOrFinish`。原错误/风险：复用缓存恢复链时，外部 Seven Curses Neow 页可能被补入本模组额外选项。依据：原重建分支对 `ExternalSevenCursesPage` 明确使用缓存原页；普通 Neow 页才调用 `EnsureSelectedCustomOptionPresent`。修正：缓存恢复时保留同一判别，普通 Neow 补回已选自定义选项，外部页和其他 Ancient 原样恢复。下次修改前自检：逐项对照普通 Neow、外部 Seven Curses 页和替代 Ancient 的缓存与恢复结果。
- 修改位置：同文件的重建辅助链。原错误/风险：切换为缓存恢复后，`RebuildInitialOptionsOrFallback`、临时修改 `RunState.Modifiers` 的反射链和重复的缓存恢复函数成为孤儿代码，保留会让后续维护误走旧路径。依据：仓库调用搜索显示这些方法仅服务于被替换的完成后重建入口。修正：删除失去调用方的辅助代码，保留原有 UI 刷新和远程选择前恢复入口。下次修改前自检：搜索所有 `SetInitialEventState(false)`、`RunState.Modifiers` 反射写入及恢复函数调用点，确认没有从人格选择收尾重新引入选项生成。

- 不要把一个人格的时序特例写成所有人格的通用规则；先看最近 working relic 和基类。
- “编译通过”只证明语法/类型成立，不证明 hook 被安装、状态被同步、DLL/PCK 被游戏加载或
  玩家看到了正确行为。每次结论要标明 source/static、build/export、runtime log、in-game
  哪一层有证据。
- 只改 initiating call site 不足以证明旧机制消失；必须搜索所有入口、观察者、资源和生成文本。
- 可选外部模组不要在初始化时假定对方 DLL 已加载；将类型判断推迟到运行时，并优先 patch
  我方稳定控制的原版/RitsuLib 入口。
- 不要把修改前置发布包当作业务模组修复；前置 hash/variant 错误先归类为前置发布问题。
- 发放遗物后不要强转 `RelicCmd.Obtain` 返回对象证明成功；从玩家实际持有实例重新读取，
  只有后续效果和计数变化可验证时才结算“已发放”。
- `STS2_WineFox-main` 只能作为结构案例，必须抽取模式后贴合当前仓库；不要因为目录、命名或旧版本相似就整段照抄。
- 需要高频实机复现时，再读取 `STS2-DevMode` 的 README/docs/manual/scripts；它只用于验证和调试，不能取代运行时 XML、源码和日志证据。

## Verification gate

完成前必须回答：

1. 这次需求命中了哪些账本条目？逐条如何核对？
2. 哪个同类实现或当前 runtime 证据支持了选择？
3. 哪些层级已验证，哪些仍需最小实机场景？
