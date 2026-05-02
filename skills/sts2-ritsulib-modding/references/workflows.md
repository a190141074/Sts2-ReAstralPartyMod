# Workflows

## 项目初始化

先看：

1. `D:\MOD\杀戮尖塔2mod制作\RitsuLib-doc\README.md`
2. `D:\MOD\杀戮尖塔2mod制作\RitsuLib-code\Docs\zh\GettingStarted.md`
3. `B:\Documents\re-astral-party-mod\ReAstralPartyMod.csproj`
4. `B:\Documents\re-astral-party-mod\ReAstralPartyMod.json`
5. `B:\Documents\re-astral-party-mod\Scripts\MainFile.cs`

要确认的事实：

- `csproj` 是否引用 `STS2-RitsuLib`
- manifest 是否声明 `dependencies: ["STS2-RitsuLib"]`
- 是否有 `[ModInitializer(...)]`
- 是否调用 `RitsuLibFramework.EnsureGodotScriptsRegistered(...)`
- 是否调用 `ModTypeDiscoveryHub.RegisterModAssembly(...)`

## 添加卡牌

先看：

1. `RitsuLib-doc\01 - 添加卡牌\README.md`
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

1. `RitsuLib-doc\03 - 添加新遗物\README.md`
2. `RitsuLib-code` 中 `RegisterRelic`、`ModRelicTemplate`
3. 当前仓库 `ReAstralPartyCardCode\Relics`
4. WineFox `Relics`

默认执行顺序：

1. 确认 relic pool 与 rarity。
2. 确认模板基类。
3. 注册属性、图标路径、本地化。
4. 如果需要 starter relic，再看角色绑定或 starter 标记。

## 添加能力

先看：

1. `RitsuLib-doc\05 - 添加新能力\README.md`
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

1. `RitsuLib-doc\06 - 添加新药水\README.md`
2. `RitsuLib-code` 中 `RegisterPotion` 和 `ModPotionTemplate`
3. 当前仓库 `ReAstralPartyCardCode\Potions`

## 添加事件 / 先古之民 / 时间线 / 附魔 / 人物

按对应章节开始：

- 事件: `12 - 添加新事件`
- 先古之民: `07 - 添加先古之民`
- 时间线: `09 - 添加时间线`
- 附魔: `13 - 添加新附魔`
- 人物: `14 - 添加新人物`

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

## 检索脚本使用方式

```powershell
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "RegisterCard"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "ModRelicTemplate"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "EnsureGodotScriptsRegistered"
```

如果只想查两个根目录，显式传 `-Roots`，避免把结果刷得太多。
