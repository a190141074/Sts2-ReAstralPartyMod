这是一个基于 `MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs` 和 `CardSelectCmd.FromSimpleGrid` 的新手教程向设计指南。

在《杀戮尖塔 2》（Slay the Spire 2）的模组开发中，`CardSelectorPrefs` 是控制“选卡界面”行为的核心配置结构体。理解它的参数对于制作自定义遗物、卡牌或事件至关重要。

### 📚 CardSelectorPrefs 核心参数速查表

| 参数名                           | 类型                      | 默认值/来源           | 作用描述                                                          | 新手建议                                                           |
|:------------------------------|:------------------------|:-----------------|:--------------------------------------------------------------|:---------------------------------------------------------------|
| **Prompt**                    | `LocString`             | 构造函数传入           | 显示在选卡界面上方的提示文字（如“选择一张牌”）。                                     | 使用模组自带的本地化字符串，确保多语言支持。                                         |
| **MinSelect**                 | `int`                   | 构造函数传入           | 玩家**最少**需要选择的卡牌数量。                                            | 通常与 `MaxSelect` 相同，除非你想让玩家可选可不选。                               |
| **MaxSelect**                 | `int`                   | 构造函数传入           | 玩家**最多**可以选择的卡牌数量。                                            | 如果只想选1张，设为1。                                                   |
| **Cancelable**                | `bool`                  | `false` (构造逻辑推导) | 是否允许玩家点击“取消”按钮退出选择界面。                                         | **关键！** 如果 `MinSelect > 0`，通常设为 `false` 强制选择；如果允许跳过，设为 `true`。 |
| **RequireManualConfirmation** | `bool`                  | 自动计算             | 是否需要玩家点击额外的“确认”按钮才能提交选择。                                      | 当 `MinSelect != MaxSelect` 时自动为 `true`。单选时通常为 `false`（点击即确认）。  |
| **PretendCardsCanBePlayed**   | `bool`                  | `false`          | **重要：** 如果为 `true`，即使卡牌费用不足或条件不满足，也会显示为“可打出”状态（绿色高亮），且不会检查费用。 | **制作“免费打出某张卡”的效果时必开此项。**                                       |
| **UnpoweredPreviews**         | `bool`                  | `false`          | 是否在悬停预览时忽略某些增益/减益效果的影响。                                       | 一般保持默认 `false`。                                                |
| **ShouldGlowGold**            | `Func<CardModel, bool>` | `null`           | 一个委托函数，返回 `true` 的卡牌会在界面上发出金色光芒。                              | 用于高亮推荐卡牌或特殊目标卡牌。                                               |

---

### 🛠️ 常见场景代码模板

以下是三种最常见的使用场景，你可以直接复制并根据需求修改。

#### 场景 1：标准单选（最常用）

**适用情况**：遗物效果“从3张随机卡中选1张加入手牌”。
**特点**：必须选1张，不能取消，点击即确认。

```csharp
// 1. 准备候选卡牌列表 (List<CardModel>)
var candidates = GetSomeCards(); 

// 2. 创建偏好设置
// 参数1: 提示语 (需本地化)
// 参数2: 选择数量 (Min=1, Max=1)
var prefs = new CardSelectorPrefs(
    new LocString("my_mod_id", "SELECT_ONE_CARD"), 
    1 
)
{
    Cancelable = false, // 不可取消
    PretendCardsCanBePlayed = false // 普通选择，不需要假装可打出
};

// 3. 调用选择界面
var selectedCards = await CardSelectCmd.FromSimpleGrid(
    choiceContext, 
    candidates, 
    Owner, 
    prefs
);

// 4. 处理结果
if (selectedCards.Any())
{
    var chosen = selectedCards.First();
    // 执行后续逻辑...
}
```

#### 场景 2：多选或可选（如“移除1-2张牌”）

**适用情况**：事件“移除1到2张牌”，或者“选择所有你想要升级的牌”。
**特点**：有最小和最大限制，可能需要手动确认。

```csharp
var prefs = new CardSelectorPrefs(
    new LocString("my_mod_id", "REMOVE_CARDS"), 
    1, // MinSelect
    2  // MaxSelect
)
{
    Cancelable = true, // 允许玩家一张都不选直接关闭
    // 注意：因为 Min(1) != Max(2)，RequireManualConfirmation 会自动变为 true
    // 玩家选完后需要点一个“确认”按钮
};

var selectedCards = await CardSelectCmd.FromSimpleGrid(choiceContext, handCards, Owner, prefs);

// 处理选中的多张卡
foreach (var card in selectedCards)
{
    RemoveCard(card);
}
```

#### 场景 3：自动打出/无视费用（如你的 `EventDeusExMachina`）

**适用情况**：卡牌效果“选择一张牌并**免费**打出它”。
**特点**：必须开启 `PretendCardsCanBePlayed`，否则高费卡会变红且无法选择/打出。

```csharp
var prefs = new CardSelectorPrefs(
    new LocString("my_mod_id", "PLAY_A_CARD_FREE"), 
    1
)
{
    Cancelable = false,
    PretendCardsCanBePlayed = true // 【关键】忽略费用检查，让所有卡看起来都能打
};

var selectedCards = await CardSelectCmd.FromSimpleGrid(choiceContext, allCards, Owner, prefs);

if (selectedCards.Any())
{
    var cardToPlay = selectedCards.First();
    // 使用 AutoPlay 并配合 PretendCardsCanBePlayed=true 的效果
    await CardCmd.AutoPlay(choiceContext, cardToPlay, Owner.Creature, AutoPlayType.Default, false, true);
}
```

#### 场景 4：带高亮提示的选择

**适用情况**：想引导玩家选择特定的卡（例如“选择一张攻击牌”，高亮所有攻击牌）。

```csharp
var prefs = new CardSelectorPrefs(
    new LocString("my_mod_id", "SELECT_AN_ATTACK"), 
    1
)
{
    Cancelable = true,
    // 定义高亮逻辑：如果是攻击牌，则发光
    ShouldGlowGold = (card) => card.HasKeyword(CardKeyword.Attack) 
};
```

---

### ⚠️ 新手常见坑点 (FAQ)

1. **Q: 为什么我选了卡，但是程序没反应？**
	* **A:** 检查 `await`。`CardSelectCmd.FromSimpleGrid` 是异步方法，必须使用 `await`，否则代码会继续向下执行，此时
	  `selectedCards` 还是空的。

2. **Q: 为什么高费卡是红色的，我选不了？**
	* **A:** 你没有设置 `PretendCardsCanBePlayed = true`。默认情况下，选卡界面会检查当前能量是否足够。如果你是想“免费打出”或“单纯选择卡牌对象而不考虑费用”，务必开启此选项。

3. **Q: `MinSelect` 和 `MaxSelect` 有什么区别？**
	* **A:** 如果 `Min == Max`，玩家必须正好选这么多张，且通常点击即确认。如果 `Min < Max`，玩家可以在范围内任意选择，且通常需要额外的确认步骤（
	  `RequireManualConfirmation` 自动变真）。

4. **Q: 如何获取本地化字符串 (`LocString`)？**
	* **A:** 不要硬编码字符串（如 `"Select a card"`）。应该在模组的本地化文件（通常是 JSON 或 CSV）中定义 key，然后使用
	  `new LocString("namespace", "key")`。这样其他语言的玩家能看到翻译。

5. **Q: `FromSimpleGrid` 和 `FromHand` 有什么区别？**
	* **A:** `FromSimpleGrid` 显示一个网格视图，适合展示大量卡牌（如牌库、所有事件卡）。`FromHand` 专门用于手牌选择，UI
	  布局更像原版游戏的手牌交互。对于你的 `EventDeusExMachina`，因为是从“所有事件卡”中选，`FromSimpleGrid` 是正确的选择。

### 💡 针对 `EventDeusExMachina` 的具体优化建议

在你的代码中：

```csharp
var prefs = new CardSelectorPrefs(SelectionPrompt, 1)
{
    Cancelable = false,
    PretendCardsCanBePlayed = true
};
```

这段配置是**完全正确**的。

* `SelectionPrompt`: 提示玩家选择。
* `1`: 只选一张。
* `Cancelable = false`: 必须选一张，符合“天降神兵”必须生效的逻辑。
* `PretendCardsCanBePlayed = true`: 因为你要 `AutoPlay`
  这张卡，且不希望因为费用问题导致逻辑错误（虽然事件卡通常费用为0或特殊，但开启此选项更安全，确保无论什么卡都能被选中并强制打出）。