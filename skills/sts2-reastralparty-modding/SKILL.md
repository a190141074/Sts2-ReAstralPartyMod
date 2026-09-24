---
name: sts2-reastralparty-modding
description: 用于 B:\Documents\re-astral-party-mod 的仓库专属开发，覆盖 RitsuLib 内容、战斗、本地化、联机同步、启动链和导出。
---

# ReAstralParty 仓库层

先加载 `sts2-ritsulib-modding`，再加载本仓库层。每次代码、排错、迁移、联机、本地化或导出任务都必须先读通用层的
`references/experience-ledger.md`，再按需读取本仓库专题。`doc/AGENT.zh.md` 和 `doc/AGENT.md` 是仓库政策，不能代替实际代码和日志取证。

## 工作循环

1. 从用户点名的源文件、本地化 key、存档、日志、bundle 或截图开始。
2. 找仓库内最接近且已经正常工作的实现，追完整套链：注册、模型/基类、资源、本地化、生成路径和观察者。
3. 玩家文案按下方“玩家可见描述”规则处理：先参考同类内容，再严格保留用户给定文本与排版；不得自行扩写、润色或推断。
4. 选择最窄的现有 base/helper/Hook；时机问题读 [timing-map.md](references/timing-map.md)，功能路线读 [workflows.md](references/workflows.md)。
5. 修改后搜索旧 ID/文本在代码、本地化、文档、资源和生成描述中的所有残留，验证 JSON/UTF-8 与 `git diff --check`。

## 玩家可见描述

- 编辑筹码、遗物或人格遗物描述前，先查看仓库内同类、同品质的现行描述作为风格参照。处理人格遗物时，额外参考其他人格遗物的句式、分行和信息密度；参考风格不等于复制其机制。
- 用户给出的玩家文案、效果顺序、数字、触发时机、条件、作用对象、标签和换行都是硬边界。默认逐字沿用；不得添加解释、例子、服务项目、隐藏条件或其他用户未要求的效果，也不得把原句扩写成自认为更清楚的版本。
- 只有用户明确要求改写，或原句存在必须处理的明显歧义时，才调整措辞。此时先保留原意与排版，并把确实无法从仓库确定的语义问题交还用户，不自行补机制。
- 遗物描述中的效果/机制关键词一律使用 `[gold]...[/gold]`；数值使用 `[blue]...[/blue]`。不得用 `[green]`、`[red]` 或其他颜色标记遗物关键词。沿用项目既有术语和标签，不自行改色。
- 用户提供或原文表达了分开的效果句时，描述必须写入显式 `\n`，不能依赖游戏 UI 自动折行。中文句子照用户原话保留，包括用户指定的表达方式；不要为润色替换成更长的说法。
- 提交前逐项对照需求与最终三语描述：效果、数字、时机、条件、目标、顺序和换行不得漂移；检查命名关键词是否全为金色、数字是否为蓝色，并确认 README/提取数据没有过时文案。遇到本地化差异，优先修正描述，不改动机制来迁就文案。

## ReAstralParty 不变量

- 使用 `CooldownPersonRelicBase` 的标准人格遗物，在获得遗物后的首场战斗首个自身回合立即发开场牌；之后按该遗物自己的冷却推进。
  保留 `PendingCombatStartCard`、`CounterValue` 和基类的 `AfterTurnEnd`/`AfterCombatEnd` 流程，只在 `BeforeCombatStart` 重置战斗内标记。
  自定义时序必须有用户明确要求，并把例外写在对应实现旁边。
- 人格生成的技能牌沿用仓库的自动冷却附魔约定，除非需求明确取消。
- 模型自己的描述不要重复自动关键词或 hover 文本；改动内容要检查标题、description、smart/remote description、flavor 和三语本地化。
- 联机或会修改战斗集合的逻辑使用官方命令链、稳定排序、单一权威同步状态；发命令前先快照集合。
- 构建不是行为证明。分别报告 source/static、build/export、runtime log、in-game 证据；没有实机证据时写出最小待测场景。
- 未被需求授权时，不要修改启动/人格注册、设置或无关功能链。

## 专题入口

- 仓库任务顺序、本地化、内容族、战斗、设置和商店：[references/workflows.md](references/workflows.md)
- Hook 和生命周期时机：[references/timing-map.md](references/timing-map.md)
- 回退、启动/人格/设置、商店/月球、跨玩家牌组、VFX/临时牌、Final Arena、版本 bundle：[references/specialized-workflows.md](references/specialized-workflows.md)
- 已建立的中英文术语：[references/bilingual-glossary-template.md](references/bilingual-glossary-template.md)

## 构建与导出

代码改动使用 `dotnet build ReAstralPartyMod.csproj`。导出任务必须确认游戏目录 DLL/PCK 已变化；行为排错先看最新相关日志中的第一个硬错误。
PCK 文本扫描只能作为诊断，不能证明运行时行为。
