using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Tags;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

public abstract class BaseAbilityCardModel : AstralPartyCardModel
{
    protected BaseAbilityCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true)
        : base(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
    {
    }

    protected override HashSet<CardTag> CanonicalTags => [AstralCardTags.BaseAbility];
}
