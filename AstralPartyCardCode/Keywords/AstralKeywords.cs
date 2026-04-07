using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace AstralPartyMod.AstralPartyCardCode.Keywords;

public static class AstralKeywords
{
    // 自定义枚举的名字。最终会变成{前缀}-{枚举值大写}的形式，例如TEST-UNIQUE
    // 放在原版卡牌描述的位置，这里是卡牌描述的前面
    [CustomEnum("ASTRAL_EVENT")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralEvent;
}