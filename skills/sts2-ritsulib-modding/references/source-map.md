# Source Map

## 路径总表

### 当前仓库

- `B:\Documents\re-astral-party-mod`
  - 当前 RitsuLib 前置 mod 项目。
  - 用于确认真实入口、依赖声明、目录约定、本地化文件位置、现有实现风格。

### 当前仓库补充文档

- `B:\Documents\re-astral-party-mod\doc\从BaseLib 到 RitsuLib.md`
  - BaseLib -> RitsuLib 迁移对照补充。
  - 优先用于替换特性、基类、ID、角色/池、事件和升级映射。

### 旧 BaseLib 教程集合

- `B:\Documents\astral-party-mod\docs`
  - 旧项目携带的 BaseLib 教程与示例。
  - 只在迁移、术语对照、或需要理解旧写法时使用，不作为 RitsuLib 主权威。

### RitsuLib 教程文档

- `D:\MOD\杀戮尖塔2mod制作\RitsuLib-doc`
  - 任务入口文档。
  - 添加卡牌、配置、遗物、能力、药水、先古之民、时间线、怪物、事件、附魔、人物、单例、血条覆盖都优先从这里开始。

### RitsuLib 代码

- `D:\MOD\杀戮尖塔2mod制作\RitsuLib-code`
  - API 与真实符号权威。
  - 用于确认注册器、AutoRegistration 特性、模板基类、内容包构建器、关键词注册、文档中提到的方法签名。

### RitsuLib 实战案例

- `D:\MOD\杀戮尖塔2mod制作\STS2_WineFox-main`
  - 结构参考项目。
  - 用于确认一个完整 RitsuLib mod 如何组织入口、卡牌目录、角色池、关键词、Power、Events、Relics、Commands、Content 等模块。

### 游戏反编译代码

- `D:\MOD\杀戮尖塔2mod制作\Slay-the-Spire-2-gdsdecomp`
  - 原生类型、枚举、场景和行为权威。
  - 当 RitsuLib 文档没有覆盖、或需要确认底层原生行为时再看。

### RitsuLib 运行时 DLL

- `D:\Steam\steamapps\common\Slay the Spire 2\mods\RitsuLib`
  - 当前本机安装的 RitsuLib DLL 所在位置。
  - 用于确认 `csproj` 本地引用路径和实际加载环境。

## 章节路由

- `01 - 添加卡牌`: 新卡牌与 starter/token 相关请求
- `02 - 自定义配置`: 配置系统与设置项
- `03 - 添加新遗物`: 自定义遗物
- `04 - 添加卡牌属性`: 动态变量或卡牌属性
- `05 - 添加新能力`: Power
- `06 - 添加新药水`: Potion
- `07 - 添加先古之民`: 先古之民
- `08 - 添加充能球`: 充能球/球体
- `09 - 添加时间线`: Timeline
- `11 - 添加新怪物`: Monster
- `12 - 添加新事件`: Event
- `13 - 添加新附魔`: Enchantment
- `14 - 添加新人物`: Character
- `15 - 添加单例`: Singleton
- `16 - 血条覆盖`: 血条覆盖/相关 UI

## 使用顺序

1. 先从 `RitsuLib-doc` 找章节。
2. 再去 `RitsuLib-code` 核对真实 API、特性、模板与签名。
3. 再看当前仓库是否已有接近实现。
4. 需要完整结构样例时看 WineFox。
5. 仍然不够时再看反编译代码。
