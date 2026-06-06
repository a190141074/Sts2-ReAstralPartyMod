using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool), StableEntryStem = "enigmatic_strikes_etherium_scythe")]
public class EnigmaticStrikeEtheriumScythe : AstralPartyCardModel
{
    private const decimal BaseDamage = 9m;

    protected override string CardId => "enigmatic_strikes_etherium_scythe";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move)
    ];

    public EnigmaticStrikeEtheriumScythe()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies, showInCardLibrary: false)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(3m);
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return CommonActions.AttackAllEnemies(choiceContext, this, 2);
    }
}
