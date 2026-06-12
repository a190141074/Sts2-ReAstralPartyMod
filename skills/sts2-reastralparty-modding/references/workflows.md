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
