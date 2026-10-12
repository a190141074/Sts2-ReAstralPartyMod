---
name: sts2-reastralparty-modding
description: 用于 B:\Documents\re-astral-party-mod 的仓库专属开发，覆盖 RitsuLib 内容、战斗、本地化、联机同步、启动链和导出。
---

# ReAstralParty 仓库层

## 项目边界（硬规则）

`re-astral-party-mod` 是一个运行在《杀戮尖塔 2》中的游戏 Mod，`STS2-RitsuLib` 是其前置库。仓库代码只能通过游戏公开运行时、RitsuLib
注册链、生命周期事件和已验证的兼容补丁扩展游戏；游戏内部的 `CombatRoom`、战斗状态对象、房间切换和其他引擎对象属于游戏本体，不是本
Mod 自己提供的“创建功能”。描述、排错和实现时必须区分“本 Mod 的 Hook/事件处理器”和“游戏本体正在执行的生命周期”。不得把本体构造函数、日志阶段或
RitsuLib 转发事件改写成 Mod 拥有对应游戏系统。

先加载 `sts2-ritsulib-modding`，再加载本仓库层。每次代码、排错、迁移、联机、本地化或导出任务都必须先读通用层的
`references/experience-ledger.md`，再按需读取本仓库专题。`doc/AGENT.zh.md` 和 `doc/AGENT.md` 是仓库政策，不能代替实际代码和日志取证。

## 工作循环

1. 从用户点名的源文件、本地化 key、存档、日志、bundle 或截图开始。
2. 找仓库内最接近且已经正常工作的实现，追完整套链：注册、模型/基类、资源、本地化、生成路径和观察者。
3. 玩家文案按下方“玩家可见描述”规则处理：先参考同类内容，再严格保留用户给定文本与排版；不得自行扩写、润色或推断。
4. 选择最窄的现有 base/helper/Hook；时机问题读 [timing-map.md](references/timing-map.md)
   ，功能路线读 [workflows.md](references/workflows.md)。
5. 修改后搜索旧 ID/文本在代码、本地化、文档、资源和生成描述中的所有残留，验证 JSON/UTF-8 与 `git diff --check`。

人格选择相关代码每次修改都必须同步更新通用层的 [经验账本](../sts2-ritsulib-modding/references/experience-ledger.md)
（错题本），从本次起执行，不追溯旧提交。每处改动分别写明修改位置、原错误或风险、日志或同类实现依据、修正方式和下次修改前自检项；完成前逐项对照代码
diff，不能用一句笼统结论代替。

## 玩家可见描述

- 品质映射固定为：蓝色 = `common` = 普通；紫色 = `uncommon` = 罕见；金色 = `rare` = 稀有。注册品质、本地化品质名称和玩家描述必须按此对应，不能混用“罕见/稀有”。
- 编辑筹码、遗物或人格遗物描述前，先查看仓库内同类、同品质的现行描述作为风格参照。处理人格遗物时，额外参考其他人格遗物的句式、分行和信息密度；参考风格不等于复制其机制。
- 用户给出的玩家文案、效果顺序、数字、触发时机、条件、作用对象、标签和换行都是硬边界。默认逐字沿用；不得添加解释、例子、服务项目、隐藏条件或其他用户未要求的效果，也不得把原句扩写成自认为更清楚的版本。
- 只有用户明确要求改写，或原句存在必须处理的明显歧义时，才调整措辞。此时先保留原意与排版，并把确实无法从仓库确定的语义问题交还用户，不自行补机制。
- 遗物描述中的效果/机制关键词一律使用 `[gold]...[/gold]`；数值使用 `[blue]...[/blue]`。不得用 `[green]`、`[red]`
  或其他颜色标记遗物关键词。沿用项目既有术语和标签，不自行改色。
- 用户提供或原文表达了分开的效果句时，描述必须写入显式 `\n`，不能依赖游戏 UI 自动折行。中文句子照用户原话保留，包括用户指定的表达方式；不要为润色替换成更长的说法。
- 冷却人格发放人格技能牌时，统一使用 `· 每[blue]X[/blue]回合开始时，将[blue]1[/blue]张[gold]XX[/gold]加入手中。`
  的句式；三语只翻译语言，不改变“回合开始时”、数量、技能牌名称或显式换行。
- 人格技能牌默认同时具备[gold]唯一[/gold]和[gold]冷却附魔[/gold]：代码必须让 `CanonicalKeywords` 包含
  `AstralKeywords.AstralUnique`，并让 `ShouldAutoApplyCooldownEnchantment` 返回 `true`；只有用户明确要求时才允许取消其中一项。
- 提交前逐项对照需求与最终三语描述：效果、数字、时机、条件、目标、顺序和换行不得漂移；检查命名关键词是否全为金色、数字是否为蓝色，并确认
  README/提取数据没有过时文案。遇到本地化差异，优先修正描述，不改动机制来迁就文案。

## ReAstralParty 不变量

- **联机优先（强制）**
  ：本仓库是联机模组。设计、排错和实现一律先以多人联机为目标；单机可用、构建通过或主机本地状态正确都不能作为完成标准。每条涉及游戏状态的链都必须确认：哪个端执行、哪一端是权威、客户端如何应用最终状态、重复/重试如何处理，以及读档和中途加入是否涉及该状态。
- **联机 API 与上下文核对（强制）**：修改前先读传入的 `PlayerChoiceContext`、当前运行时 `sts2.xml`/`STS2-RitsuLib.xml`
  和项目实际引用的 DLL；不得从日志调用栈、旧 XML 或字段名推断可用重载。上下文型 Hook 中的 context 不得无理由丢弃；无上下文
  Hook 中若要修改联机状态，必须找到当前版本已验证的权威执行及同步路径，不能直接套用主机本地命令。
- **联机同类证据（强制）**
  ：必须搜索仓库中已工作的同类完整链，并逐端说明触发、命令、持久化和同步顺序。若只有静态代码或单机结果，结论要标为未验证；涉及联机行为时至少给出最小双人复现场景，三人范围按影响再扩展。实现后核对各端最终值与
  checksum，不把“命令调用成功”视作同步成功。
- 使用 `CooldownPersonRelicBase` 的标准人格遗物，在获得遗物后的首场战斗第1个自身回合立即发开场牌；若描述为“每[blue]
  3[/blue]回合”，之后应在第4、7、10……个持有者自身回合开始时发牌。计数只推进持有者自身回合，不按全局、其他玩家或敌方回合计数。
  保留 `PendingCombatStartCard`、`CounterValue` 和基类的 `AfterTurnEnd`/`AfterCombatEnd` 流程，只在 `BeforeCombatStart`
  重置战斗内标记。
  自定义时序必须有用户明确要求，并把例外写在对应实现旁边。
- 人格生成的技能牌沿用仓库的自动冷却附魔约定，除非需求明确取消。
- 模型自己的描述不要重复自动关键词或 hover 文本；改动内容要检查标题、description、smart/remote description、flavor 和三语本地化。
- 联机或会修改战斗集合的逻辑使用官方命令链、稳定排序、单一权威同步状态；发命令前先快照集合。
- 构建不是行为证明。分别报告 source/static、build/export、runtime log、in-game 证据；没有实机证据时写出最小待测场景。
- 未被需求授权时，不要修改启动/人格注册、设置或无关功能链。

## 专题入口

- 仓库任务顺序、本地化、内容族、战斗、设置和商店：[references/workflows.md](references/workflows.md)
- Hook 和生命周期时机：[references/timing-map.md](references/timing-map.md)
- 回退、启动/人格/设置、商店/月球、跨玩家牌组、VFX/临时牌、Final Arena、版本
  bundle：[references/specialized-workflows.md](references/specialized-workflows.md)
- 已建立的中英文术语：[references/bilingual-glossary-template.md](references/bilingual-glossary-template.md)

## 构建与导出

每次执行构建前，先将仓库根目录 `ReAstralPartyMod.json` 的补丁版本号递增 `1`；不追踪构建过程内部是否额外导致版本变化，只保证本次构建操作主动递增一次。构建验证时同时检查
manifest 版本和游戏目录中的 DLL/PCK 是否更新。

代码改动使用 `dotnet build ReAstralPartyMod.csproj`。导出任务必须确认游戏目录 DLL/PCK 已变化；行为排错先看最新相关日志中的第一个硬错误。
PCK 文本扫描只能作为诊断，不能证明运行时行为。
