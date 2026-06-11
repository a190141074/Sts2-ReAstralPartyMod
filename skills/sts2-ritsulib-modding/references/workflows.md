# Workflows

## 项目初始化

先看：

1. `D:\MOD\杀戮尖塔2mod制作\RitsuLib-doc\README.md`
2. `D:\MOD\杀戮尖塔2mod制作\RitsuLib-code\Docs\zh\GettingStarted.md`
3. `B:\Documents\re-astral-party-mod\ReAstralPartyMod.csproj`
4. `B:\Documents\re-astral-party-mod\ReAstralPartyMod.json`
5. `B:\Documents\re-astral-party-mod\Scripts\MainFile.cs`
6. `B:\Documents\re-astral-party-mod\doc\AGENT.zh.md`
7. `B:\Documents\re-astral-party-mod\doc\AGENT.md`

要确认的事实：

- `csproj` 是否引用 `STS2-RitsuLib`
- manifest 是否声明 `dependencies: ["STS2-RitsuLib"]`
- 是否有 `[ModInitializer(...)]`
- 是否调用 `RitsuLibFramework.EnsureGodotScriptsRegistered(...)`
- 是否调用 `ModTypeDiscoveryHub.RegisterModAssembly(...)`

## 添加卡牌

先看：

1. `RitsuLib-doc\RitsuLib\01 - 添加基础内容\01 - 添加卡牌\README.md`
2. `RitsuLib-code\Interop\AutoRegistration\RegistrationAttributes.cs`
3. `RitsuLib-code\Scaffolding\Content\ModCardTemplate.cs`
4. `STS2_WineFox-main\Cards`
5. 当前仓库 `ReAstralPartyCardCode\Cards`

默认执行顺序：

1. 确认要挂在哪个 pool。
2. 确认是普通卡、starter card、还是 token。
3. 确认应继承 `ModCardTemplate` 还是项目自定义卡牌基类。
4. 加 `[RegisterCard(typeof(...Pool))]`。
5. 补图片、本地化、必要的 starter 绑定。

## 添加遗物

先看：

1. `RitsuLib-doc\RitsuLib\01 - 添加基础内容\03 - 添加新遗物\README.md`
2. `RitsuLib-code` 中 `RegisterRelic`、`ModRelicTemplate`
3. 当前仓库 `ReAstralPartyCardCode\Relics`
4. WineFox `Relics`

默认执行顺序：

1. 确认 relic pool 与 rarity。
2. 确认模板基类。
3. 先找当前仓库里至少 `1` 个同类已工作的遗物做参照，不要只看基类。
4. 对照参照物，逐项确认：
   - `RegisterRelic(...)` 是否需要 `StableEntryStem`
   - 代码里的 `RelicId`
   - `relics.json` 三语 key
   - 资源路径 / `IconBasePath`
5. 再补注册属性、图标路径、本地化。
6. 如果需要 starter relic，再看角色绑定或 starter 标记。

如果是 `B:\Documents\re-astral-party-mod` 里的 `Person*` / `VariantPerson*` / 月球遗物这类“仓库内已经有一整套同类参照”的内容，额外执行这条检查：

1. 不要假设 `AstralPartyRelicModel` 会自动兜住 public entry、本地化 key、资源命名。
2. 新 relic 完成后，至少搜索一次：
   - 代码里的 `RelicId`
   - 三语 `relics.json` key
   - 运行时日志或存档里出现的 `RELIC.*` / `TextKey`
3. 如果这三者任一不一致，优先修 public entry 或补兼容 key，再看别的 UI 症状。

如果是 `B:\Documents\re-astral-party-mod` 里的“成套内容”（例如 `VariantPerson*` 搭配技能牌和 powers），在上面检查之外再补这一轮：

1. 不要只检查 relic。
2. 必须把同套的 relic / card / power 一起搜一遍显式资源或 id override：
   - `RelicId`
   - `IconBasePath`
   - `CardId`
   - `PortraitBasePath`
   - `FrameBasePath`
   - `PowerId`
   - `ResolveIconPath()`
3. 默认优先信任仓库公共基底的自动命名链：
   - `AstralPartyRelicModel`
   - `AstralPartyCardModel`
   - `AstralPartyPowerModel`
4. 如果没有明确例外理由，不要手写这些 override。
5. 发现手写 override 时，先问自己两件事：
   - 默认链是否其实已经能得到同样结果
   - 这个 override 会不会把某个子件的命名链改得和同套其他文件不一致
6. 只有确认默认链不适用时，才保留显式指定。

## 添加能力

先看：

1. `RitsuLib-doc\RitsuLib\01 - 添加基础内容\05 - 添加新能力\README.md`
2. `RitsuLib-code` 中 Power 相关模板和注册器
3. 当前仓库 `ReAstralPartyCardCode\Powers`
4. WineFox `Powers`

重点关注：

- 图标路径
- `powers.json`
- 数值显示和动态描述
- 是否需要项目内统一 Power 基类

## 添加药水

先看：

1. `RitsuLib-doc\RitsuLib\01 - 添加基础内容\06 - 添加新药水\README.md`
2. `RitsuLib-code` 中 `RegisterPotion` 和 `ModPotionTemplate`
3. 当前仓库 `ReAstralPartyCardCode\Potions`

## 添加事件 / 先古之民 / 时间线 / 附魔 / 人物

按对应章节开始：

- 事件: `RitsuLib\01 - 添加基础内容\12 - 添加新事件`
- 先古之民: `RitsuLib\01 - 添加基础内容\07 - 添加先古之民`
- 时间线: `RitsuLib\01 - 添加基础内容\09 - 添加时间线`
- 附魔: `RitsuLib\01 - 添加基础内容\13 - 添加新附魔`
- 人物: `RitsuLib\01 - 添加基础内容\14 - 添加新人物`

流程固定：

1. 文档确认推荐写法。
2. 代码确认注册属性和模板。
3. WineFox 或当前仓库对照真实组织形式。
4. 需要时再看反编译代码确认原生类型和枚举。

## BaseLib -> RitsuLib 迁移

先看：

1. `B:\Documents\re-astral-party-mod\doc\从BaseLib 到 RitsuLib.md`
2. 本 skill 的 `migration-map.md`
3. `RitsuLib-code\Docs\zh\ContentPacksAndRegistries.md`
4. `RitsuLib-code\Docs\zh\LocalizationAndKeywords.md`

默认迁移顺序：

1. 替换入口初始化
2. 替换注册属性
3. 替换内容基类
4. 替换 ID 与关键词规则
5. 替换角色、卡池、starter 绑定
6. 替换事件/先古之民/升级映射

## 排错

按这个顺序排：

1. 入口函数有没有运行
2. `RegisterModAssembly` 有没有调用
3. `EnsureGodotScriptsRegistered` 有没有保留
4. `dependencies` 是否缺 `STS2-RitsuLib`
5. `csproj` 是否指向正确 DLL
6. 特性/模板/ID 是否写错
7. 本地化、图片、场景路径是否缺失
8. 再看 Harmony 与底层原生代码

如果症状落在 `B:\Documents\re-astral-party-mod` 的“显示英文 / 原始 ID / 查看时报本地化异常”，先补这份本地化 checklist：

1. 先查 `logs\saves\mod_data\DevMode\instances\*\session.log`
2. 再查 `logs\godot*.log`
3. 先分清：
   - `GetRawText: Key '...' not found` = 缺 key、ID 不匹配、table 写错、locale 回退
   - `Localization formatting error` = key 找到了，但占位符变量没注入
4. 新增内容不要只看主描述，至少同时核对 `zhs / eng / jpn` 三语里的这些 key：
   - `title`
   - `description`
   - `smartDescription`
   - `remoteDescription`
   - `flavor`
   - `stats_*`
   - `select_prompt`
5. 对 `PowerModel` 优先检查 `SmartDescriptionLocKey` 对应文案，默认把带 `{Amount}`、`{Threshold}`、`{Energy}` 之类占位符的 `smartDescription` 视为高风险项
6. 只有确认当前模型在该调用链里显式注入了这些变量，`smartDescription` 才是安全的；否则先改文案或改注值链，不要只补翻译文本
7. 如果对象是新加的 `VariantPerson*` / `Person*` / 其他仓库内已有完整同类模板的 relic，先对照一个正常同类实现，再核对：
   - `RegisterRelic(...)` 是否锁了稳定 public entry
   - `RelicId` 与三语 key 是否真的是同一个 entry
   - 运行日志 / 存档里的真实 `RELIC.*` 是否和你写入的本地化 key 对得上
8. 如果对象还会通过 `HoverTipFactory.FromPower<T>()` 暴露 power 描述，额外检查：
   - 该 power 的 `Description`
   - `SmartDescriptionLocKey`
   - `GetDescriptionLocKey()`
   - `GetSmartDescriptionLocKey()`
   这些路径里是否直接读取了 `Owner` / `CombatState`
9. 只要这些查看链函数会在 canonical power 上访问 mutable 状态，就优先修这个，再看别的 UI 症状。
10. 只要运行时真实 entry 和本地化 key 有一处拼写分段不一致，就把它视为优先级最高的问题；不要先怀疑翻译内容或 UI 组件。

仓库里这两类问题经常共存，所以“UI 显示英文”不要先怀疑翻译内容本身，先判断到底是回退还是格式化崩了。

如果已经需要“进游戏高频复现 / 快速发资源 / 快速推进流程 / 脚本化验证”，补看：

1. `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode\README.zh-CN.md`
2. `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode\docs`
3. `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode\manual`
4. `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode\scripts`

适用场景：

- 快速复现卡牌、遗物、事件、能力问题
- 需要游戏内脚本/控制台辅助验证
- 需要把“代码核对”补成“实机可重复测试”

## 检索脚本使用方式

```powershell
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "RegisterCard"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "ModRelicTemplate"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "EnsureGodotScriptsRegistered"
```

如果只想查两个根目录，显式传 `-Roots`，避免把结果刷得太多。
