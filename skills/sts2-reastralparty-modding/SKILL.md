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
3. 保留需求中的玩法含义和玩家文案。不能从另一个人格或一次历史修复推断缺失的数字、时机、归属或范围。
4. 选择最窄的现有 base/helper/Hook；时机问题读 [timing-map.md](references/timing-map.md)，功能路线读 [workflows.md](references/workflows.md)。
5. 修改后搜索旧 ID/文本在代码、本地化、文档、资源和生成描述中的所有残留，验证 JSON/UTF-8 与 `git diff --check`。

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
