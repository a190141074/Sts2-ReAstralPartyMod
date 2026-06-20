# ReAstralParty Workflows

## 时机 / Hook 选择

如果问题本质上是：

- “回合开始该挂哪一种”
- “玩家自己的回合开始，还是整个 side turn”
- “该写模型 override，还是订阅 lifecycle event”
- “这里是不是该用仓库里的 base/helper，而不是再 patch 一层”

先读 [timing-map.md](timing-map.md)。

固定 checklist：

1. 目标是“玩家自己的回合”还是“整个 side turn”
2. 目标是“单对象变化”还是“全局观察”
3. 目标是“只服务某张卡/某件 relic”还是“横切整个系统”
4. 是否会影响多人局同步 / RNG / 集合遍历
5. 是否已存在仓库内更窄的成熟 base/helper 可直接复用

## 本地化 / 查看 UI 排错

固定顺序：

1. 先查 `B:\Documents\re-astral-party-mod\logs\saves\mod_data\DevMode\instances\*\session.log`
2. 再查 `B:\Documents\re-astral-party-mod\logs\godot*.log`
3. 先分类：
   - `GetRawText: Key '...' not found`
   - `Localization formatting error`
4. 再决定是修 key / entry / table / locale，还是修格式变量与注值链

固定 checklist：

1. 三语都查：
   - `zhs`
   - `eng`
   - `jpn`
2. 不要只看主描述，至少一起查：
   - `title`
   - `description`
   - `smartDescription`
   - `remoteDescription`
   - `flavor`
   - `stats_*`
   - `select_prompt`
3. 对 power 额外检查：
   - `Description`
   - `SmartDescriptionLocKey`
   - `GetDescriptionLocKey()`
   - `GetSmartDescriptionLocKey()`
4. 只要 `smartDescription` 有变量，占位符默认视为不安全，先确认当前 UI 链是否显式注值。

## 新异格 / 新人格 / 成套内容

固定顺序：

1. 先找一个仓库内已正常工作的同类成品。
2. 不只检查 relic，要把同套的 relic / card / power 一起看。
3. 默认先删除不必要的显式命名 override 这个念头，再判断是否真要手写。
4. 如果保留 override，逐项核对：
   - 默认链为什么不适用
   - 会不会和同套其他子件的命名结果不一致
5. 至少搜索一次运行时真实 entry、代码 id、本地化 key 是否一致。

最低检查面：

- `RegisterRelic(...)` / `StableEntryStem`
- `RelicId`
- `IconBasePath`
- `CardId`
- `PortraitBasePath`
- `FrameBasePath`
- `PowerId`
- `ResolveIconPath()`

## 战斗 / 联机 / 卡死排错

固定顺序：

1. 先找最早的直接异常
2. 再看后续是否出现 `StateDivergence`、checksum split、同步分叉
3. 如果 hook 内会伤害、击杀、移除、融化、移除 power、改 reward、改 creature 列表，优先检查 live collection mutation
4. 能快照的集合先快照，再调用 `CreatureCmd` / `PowerCmd` / `RelicCmd`

典型高风险面：

- `AfterPlayerTurnStart(...)`
- `BeforeCombatStart(...)`
- `AfterCombatEnd(...)`
- 遍历 `Players`、`Enemies`、`Relics`、`Powers` 时直接发命令

## 新版兼容 / 启动红字

固定顺序：

1. 先看 `logs\godot*.log` 最早的 `[ERROR]`
2. 先分是 loader / manifest 红字，还是 patch apply 红字
3. 如果是 manifest：
   - 先补 `min_game_version`
   - 再把旧字符串 `dependencies` 改成当前分支对象写法，并补 `min_version`
4. 如果是 patch apply：
   - 先对照运行时 `sts2.xml` / `STS2-RitsuLib.xml`
   - 再对照 `RitsuLib-code` 里对应 lifecycle patch 的 target 列表
5. 最后才怀疑业务逻辑本身

固定 checklist：

1. `ReAstralPartyMod.json` 是否还停留在旧 manifest 口径
2. `AbstractModel` / `Hook` 目标方法的参数类型和参数名是否和当前运行时一致
3. 是否误用了旧版 `Hook.AfterTurnEnd(CombatState, CombatSide)` 这类 target
4. optional patch 失败是否只是兼容漂移，还是已经打断主流程

## 起始人格 / Neow / 资源显示

### 起始人格 / Neow

1. 先查 `StartingPersonaNeowReadyFlow`
2. 再查 `StartingPersonaRelicSelectionScreen`
3. 再查事件状态恢复与 `SetInitialEventState(...)`
4. 最后才怀疑 UI 动画、ready 顺序、展示延迟

### 隐藏 beta 立绘

1. 先查 `logs\godot*.log` 中的实时设置
2. 再确认资源目录是否真的有对应 `_beta` 文件
3. 再搜索目标卡牌是否 override 默认 beta 路径解析

### 发牌入运行牌组

1. 优先走正式 `RunState.AddCard(...)`
2. 不要预设 `Owner`
3. 修一条 helper 时，顺手搜索同一条入牌链的其他入口

## 玩法设置 / 内容模式

固定顺序：

1. 先确认这是“玩法设置”还是“通用设置”
2. 如果是玩法设置，再确认它是否需要：
   - 全局设置页
   - 房间玩法面板
   - lobby snapshot
   - run snapshot
   - runtime getter / patch
3. 如果涉及 `原版模式 / 整合包模式`，优先把可编辑 / 锁定 / 隐藏规则收口到 `AstralContentModeRegistry`
4. 再检查旧设置、旧 lobby、旧 run snapshot 的缺字段回退值
5. 最后才补单个业务实现

固定 checklist：

1. `ReAstralPartyModSettings` 是否新增字段
2. 设置页 `RegisterSettingsPage()` 是否注册并带本地化
3. `CharacterSelectGameplayPreviewPatch` 是否补房主可改 / 客户端只读同步
4. `LobbyGameplaySettingsSync` 是否补 snapshot、message schema、旧版本回退
5. `ReAstralPartyRunSettingsSync` 是否补 run snapshot 建立与 restore
6. `ReAstralPartyModSettingsManager` 是否补本地 getter、`IRunState` getter、总判断 helper
7. 如果是总开关，runtime getter 是否统一收口，而不是只隐藏 UI
8. 如果是通用设置，是否仍保持单份全局值，不会随内容模式切换串到另一套状态

## 商店 / 月球体系 / 特殊商人

固定顺序：

1. 先区分“普通商店自然库存”与“额外追加商店位”
2. 先查 `MerchantInventory.CreateForNormalMerchant(Player)` 的库存生成链
3. 再查 `NMerchantInventory.Initialize(...)` 的 UI 初始化链
4. 如果涉及特殊商人，先确认是否需要整链排除 `NFakeMerchantInventory`
5. 最后才改 UI clone、位置或显示层

固定 checklist：

1. 是否错误地把“月球遗物自然出现”和“月球商品额外 3 位”混成一个开关
2. 如果关闭自然出现，是否用“替换 entry”而不是“删除 entry”
3. fake merchant 是否在 `NMerchantInventory.Initialize(...)` 前缀就提前返回
4. 普通库存过滤是否发生在追加额外 3 位之前
5. 额外 3 位是否只受 `EnableMoonPropShopSlots` 控制，不被普通库存过滤误伤

## 主菜单工具按钮 / 弹窗

固定顺序：

1. 先分清这是主菜单入口还是跑局内 top bar 入口
2. 主菜单若点击后没有稳定打开界面，优先改成 `NModalContainer` 浮窗链
3. 跑局内按钮和主菜单按钮要分开管理，不要混用同一注册路径

固定 checklist：

1. 主菜单按钮位置是否避开 RitsuLib 自带按钮
2. 点击是否走 modal popup，而不是依赖不稳定的 submenu 注册
3. 关闭按钮、`Esc`、返回键是否都能关窗
4. 如果用户要求只保留主菜单入口，是否彻底下线跑局内 top bar 注册而不是只隐藏
