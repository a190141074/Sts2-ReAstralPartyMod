---
name: sts2-ritsulib-modding
description: 用于开发或排错依赖 RitsuLib 的《杀戮尖塔2》模组，覆盖 RitsuLib 注册、模板、生命周期 Hook、联机状态、BaseLib 迁移和运行时加载问题；不用于无关的 STS2 或通用 C# 任务。
---

# STS2 RitsuLib 模组开发

这是 RitsuLib 通用层。若仓库有 overlay（例如 `sts2-reastralparty-modding`），先加载本文件，
再加载仓库层。overlay 负责仓库专属路径和行为；本 skill 负责 RitsuLib 约定和证据要求。

## 工作循环

1. 先分类：初始化/加载、内容制作、Hook/时机、迁移、联机、兼容性或运行时排错。
2. 每次代码、排错、迁移、联机、本地化或导出任务都先读 [references/experience-ledger.md](references/experience-ledger.md)。
   然后只读当前问题需要的一份专题参考，不要默认加载全部参考。
3. 先在当前仓库找能正常工作的同类实现，对照它的注册链、基类、Hook、本地化和输出路径，再写新代码。
4. 符号、重载、生命周期目标、参数名或版本有任何不确定时，检查当前运行时的
   `STS2-RitsuLib.xml` 和 `sts2.xml`，必要时再用反射或反编译。禁止凭记忆或旧分支解决 API 不确定性。
5. 使用现有 RitsuLib、模板和 helper 能表达需求时，采用最小改动。只有公开/窄入口无法表达需求时，才使用 Harmony 或宽范围 patch。
6. 分层验证：源代码/静态检查、构建/导出、运行时日志、玩家可见行为。分别报告实际通过的层级；编译成功不等于运行时正确。

## 不变量

- 注册链是功能的一部分：先检查 manifest、依赖版本、`ModInitializer`、`EnsureGodotScriptsRegistered`、
  `ModTypeDiscoveryHub.RegisterModAssembly` 和必要的 Harmony 初始化，再改下游玩法。
- 联机和局内状态只保留一份权威的持久化/同步状态。使用官方命令链和稳定排序；调用可能修改集合的命令前先快照集合。
- 当前运行时证据优先于旧资料。若 API 漂移只影响一条功能链，将兼容逻辑限制在该功能内，不要扩成全局抽象。
- 需要跨存档、联机或显示稳定的概率/百分比状态，优先使用稳定的整型刻度，不要默认持久化 `float` 或 `decimal`。
- 玩家文案和实现备注分开。保留用户给出的文字、占位符、标签和模型归属；不要重复自动生成的关键词或 hover 文本。
- 不要把一次 bug、一个例子或一个用户特例写成通用规则；例外写在对应模型或专题参考旁边。
- 经验账本记录的是反复失败后的高价值约束，匹配任务时必须执行；若用户明确需求或当前运行时证据与账本冲突，先按用户/当前证据取证，再把冲突作为局部例外记录，不要悄悄覆盖账本。
- 未被需求授权时，不要修改启动链、设置链或无关功能链。

## 专题入口

- 初始化、内容类型、排错和构建顺序：[references/workflows.md](references/workflows.md)
- Hook/生命周期选择：[references/timing-map.md](references/timing-map.md)
- 运行时和源码权威路径：[references/source-map.md](references/source-map.md)
- BaseLib 到 RitsuLib 迁移：[references/migration-map.md](references/migration-map.md)
- 项目组织案例：[references/project-patterns.md](references/project-patterns.md)
- 原版内容查询（仅用于查卡牌、遗物、怪物和事件）：[references/spire-codex.md](references/spire-codex.md)
- 反复失败经验及验收闸门：[references/experience-ledger.md](references/experience-ledger.md)

需要跨 RitsuLib 文档、源码、WineFox、反编译代码和当前仓库查符号时，使用
`scripts/find-ritsulib-symbol.ps1`，不要每次手写一套长搜索命令。

仓库副本与 `C:\Users\LuoYe\.codex\skills` 安装副本必须保持一致。修改后运行
`skills/sync-codex-skills.ps1`；提交前运行 `skills/sync-codex-skills.ps1 -Check`。

除经验账本外，只读取能回答当前问题的最小专题。参考文件是索引和决策依据，不是整套 RitsuLib 手册的复制品。

## 完成标准

每次执行本仓库构建前，先将 `ReAstralPartyMod.json` 的补丁版本号递增 `1`；不追踪构建过程内部是否额外导致版本变化，只保证本次构建操作主动递增一次，并在构建验证中检查 manifest 版本和导出 DLL/PCK。

运行 `git diff --check` 和与改动匹配的最小验证；代码改动再运行正常项目构建。导出任务必须确认游戏目录中的 DLL/PCK 时间戳或哈希已变化。
涉及行为的结论必须有运行时或实机证据；没有时要明确写出仍需执行的最小场景。
