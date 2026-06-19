# Workflows

## 时机 / Hook 选择

如果问题本质上是：

- “回合开始该挂哪一种”
- “这个效果应该是玩家自己回合，还是整个 side turn”
- “该写模型 override，还是订阅 lifecycle event”
- “是不是应该先用更窄的单卡 / 单对象 hook”

先读 [timing-map.md](timing-map.md)，再继续落代码。

固定 checklist：

1. 目标是玩家自己的回合，还是整个 `CombatSide`
2. 目标是单对象变化，还是横切观察
3. 目标是只服务某个模型，还是框架级订阅
4. 是否会影响多人局同步、RNG、集合遍历
5. 当前仓库是否已经有更窄的成熟 base/helper

## 项目初始化

先看：

1. `D:\MOD\杀戮尖塔2mod制作\RitsuLib-doc\README.md`
2. `D:\MOD\杀戮尖塔2mod制作\RitsuLib-code\Docs\zh\GettingStarted.md`
3. 当前项目的 `*.csproj`
4. 当前项目的 manifest / mod json
5. 当前项目的入口文件
6. 当前仓库的 agent / overlay 文档（如果存在）

要确认的事实：

- `csproj` 是否引用 `STS2-RitsuLib`
- 运行时 `D:\Steam\steamapps\workshop\content\2868840\3747602295\lib\0.107.1\STS2-RitsuLib.dll` 是否存在
- 运行时 `D:\Steam\steamapps\workshop\content\2868840\3747602295\lib\0.107.1\STS2-RitsuLib.xml` 是否存在
- 运行时 `D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.xml` 是否存在
- manifest 是否声明 `min_game_version`
- manifest 是否按当前分支声明依赖：
  - 旧分支可用 `dependencies: ["STS2-RitsuLib"]`
  - 当前 `0.105+` / 新版口径优先用对象写法并补 `min_version`
- 是否有 `[ModInitializer(...)]`
- 是否调用 `RitsuLibFramework.EnsureGodotScriptsRegistered(...)`
- 是否调用 `ModTypeDiscoveryHub.RegisterModAssembly(...)`

## 添加卡牌

先看：

1. `RitsuLib-doc\RitsuLib\01 - 添加基础内容\01 - 添加卡牌\README.md`
2. `RitsuLib-code\Interop\AutoRegistration\RegistrationAttributes.cs`
3. `RitsuLib-code\Scaffolding\Content\ModCardTemplate.cs`
4. `STS2_WineFox-main\Cards`
5. 当前仓库卡牌目录

默认执行顺序：

1. 确认要挂在哪个 pool。
2. 确认是普通卡、starter card、还是 token。
3. 如果 exact public 接口名、override 面或 helper 参数不确定，先查运行时 `STS2-RitsuLib.xml`。
4. 确认应继承 `ModCardTemplate` 还是项目自定义卡牌基类。
5. 加 `[RegisterCard(typeof(...Pool))]`。
6. 补图片、本地化、必要的 starter 绑定。

## 添加遗物

先看：

1. `RitsuLib-doc\RitsuLib\01 - 添加基础内容\03 - 添加新遗物\README.md`
2. `RitsuLib-code` 中 `RegisterRelic`、`ModRelicTemplate`
3. 当前仓库遗物目录
4. WineFox `Relics`

默认执行顺序：

1. 确认 relic pool 与 rarity。
2. 如果 exact public 接口名、override 面或 helper 参数不确定，先查运行时 `STS2-RitsuLib.xml`。
3. 确认模板基类。
4. 先找当前仓库里至少 `1` 个同类已工作的遗物做参照，不要只看基类。
5. 对照参照物，确认注册属性、稳定 entry、资源路径、本地化 key 是否一致。
6. 再补注册属性、图标路径、本地化。
7. 如果需要 starter relic，再看角色绑定或 starter 标记。

## 添加能力

先看：

1. `RitsuLib-doc\RitsuLib\01 - 添加基础内容\05 - 添加新能力\README.md`
2. `RitsuLib-code` 中 Power 相关模板和注册器
3. 当前仓库 power 目录
4. WineFox `Powers`

重点关注：

- 图标路径
- `powers.json`
- 数值显示和动态描述
- 如果 `Power` 的 public 接口名、hook 面或 helper 参数不清楚，先查运行时 `STS2-RitsuLib.xml`
- 是否需要项目内统一 Power 基类

## 添加药水

先看：

1. `RitsuLib-doc\RitsuLib\01 - 添加基础内容\06 - 添加新药水\README.md`
2. `RitsuLib-code` 中 `RegisterPotion` 和 `ModPotionTemplate`
3. 当前仓库药水目录

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

1. 仓库自己的 BaseLib 到 RitsuLib 迁移文档（如果存在）
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
4. manifest 是否缺 `min_game_version`，以及 `dependencies` / `min_version` 是否还停留在旧写法
5. 运行时 `STS2-RitsuLib.dll` / `STS2-RitsuLib.xml` 与 `sts2.dll` / `sts2.xml` 是否都在，且和本地 `csproj` / 参考代码版本没有明显漂移
6. `csproj` 是否指向正确 DLL
7. 如果是 patch apply 失败，先对照 `STS2-RitsuLib.xml` 与 `sts2.xml` 确认 target 签名、参数类型、参数名
8. 特性/模板/ID 是否写错
9. 本地化、图片、场景路径是否缺失
10. 如果核心问题是时机选择，回到 `timing-map.md`
11. 再看 Harmony 与底层原生代码

如果已经需要“进游戏高频实机复现 / 快速发资源 / 快速推进流程 / 脚本化验证”，补看：

1. `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode\README.zh-CN.md`
2. `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode\docs`
3. `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode\manual`
4. `D:\MOD\杀戮尖塔2mod制作\STS2-DevMode\scripts`

适用场景：

- 快速复现卡牌、遗物、事件、能力问题
- 需要游戏内脚本/控制台辅助验证
- 需要把“代码核对”补成“实机可重复测试”

如果当前仓库有自己的 overlay skill，把 repo-specific 的本地化、日志路径、命名链、系统面排错，交给 overlay 继续收窄，不要继续往通用 workflow 里硬塞仓库例外。

## 检索脚本使用方式

```powershell
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "RegisterCard"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "ModRelicTemplate"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "EnsureGodotScriptsRegistered"
```

如果只想查两个根目录，显式传 `-Roots`，避免把结果刷得太多。
