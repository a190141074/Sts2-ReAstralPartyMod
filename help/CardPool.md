# 卡牌池详解

这些是《杀戮尖塔 2》Mod 开发中常见的卡牌池（Card Pool）类型。`PoolAttribute`
会决定一张自定义卡牌被注册到哪个池中，而“会不会进入商店、奖励池、角色牌池”等行为，通常首先由卡牌池决定。

## 卡牌池列表

### `SharedCardPool`

- 共享卡池
- 用于所有角色都可能接触到的普通共享卡牌
- 适合真正想作为通用内容加入游戏的卡牌

### `IroncladCardPool`

- 铁甲战士专属卡池
- 只用于 Ironclad 角色相关卡牌

### `SilentCardPool`

- 静默猎手专属卡池
- 只用于 Silent 角色相关卡牌

### `DefectCardPool`

- 机械体专属卡池
- 只用于 Defect 角色相关卡牌

### `RegentCardPool`

- Regent 专属卡池
- 适合 Regent 的正常角色牌

### `NecrobinderCardPool`

- Necrobinder 专属卡池
- 适合 Necrobinder 的正常角色牌

### `ColorlessCardPool`

- 无色卡池
- 通常会被商店、无色奖励等系统抽取
- 如果一张自定义卡放进这个池，就要默认认为它有机会出现在商店里

### `TokenCardPool`

- 标记卡池
- 用于系统衍生牌、标记牌、临时功能牌等特殊内容
- 一般不适合作为正常可购买卡牌使用

### `EventCardPool`

- 事件卡池
- 适合只能通过事件、特殊生成、脚本发放获得的卡牌
- 如果不希望卡牌出现在普通商店里，这通常是优先考虑的池之一

### `QuestCardPool`

- 任务卡池
- 适合任务流程、阶段目标、特殊规则牌
- 通常不应进入常规商店或普通奖励池

### `StatusCardPool`

- 状态卡池
- 用于伤口、灼伤、眩晕一类状态牌
- 不应用于正常可购买卡牌

### `CurseCardPool`

- 诅咒卡池
- 用于各种诅咒牌
- 不应用于正常可购买卡牌

## 如何选择卡牌池

### 正常角色牌

- 使用对应角色专属池
- 例如 `RegentCardPool`、`NecrobinderCardPool`
- 适合会进入正常角色奖励流程的牌

### 真正的无色牌

- 使用 `ColorlessCardPool`
- 适合允许进入商店、无色奖励和其他普通无色来源的牌

### 事件专属牌

- 使用 `EventCardPool`
- 适合只希望通过事件或脚本生成获得的牌

### 任务或特殊流程牌

- 使用 `QuestCardPool`
- 适合任务奖励、剧情流程、特殊回合机制

### 状态牌、诅咒牌、衍生功能牌

- 使用 `StatusCardPool`、`CurseCardPool` 或 `TokenCardPool`
- 不要放进普通角色池或无色池

## 与商店的关系

- 想让自制无色牌不出现在商店里，最直接的方法就是不要把它放进 `ColorlessCardPool`
- `showInCardLibrary` 只控制是否在卡牌库中显示，不控制商店
- `autoAdd` 只控制是否自动加入内容字典，不控制商店

## 实战建议

### 会进商店的牌

- 放进 `ColorlessCardPool` 或正常角色池

### 不想进商店、只想靠代码发放的牌

- 放进 `EventCardPool` 或 `QuestCardPool`

### 人格牌、事件牌、特殊流程牌

- 不建议继续放在 `ColorlessCardPool`
- 否则它们很容易被商店或其他无色来源抽到

## 备注

- 卡牌池决定“卡会被注册到哪里”
- 卡牌稀有度决定“在对应系统里以什么身份出现”
- 这两个概念通常需要一起设计
- 例如事件牌通常会搭配 `EventCardPool` 和 `CardRarity.Event`
