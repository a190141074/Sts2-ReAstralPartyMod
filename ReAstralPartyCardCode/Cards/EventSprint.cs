using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class EventSprint : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new("Dexterity", 3m)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<AstralTemporaryDexterityPower>()];

    public EventSprint() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
            return;

        foreach (var player in CombatState.Players)
        {
            await AstralTemporaryDexterityPower.Apply(player.Creature, DynamicVars["Dexterity"].BaseValue, this, null, this);

            var bionicJasmine = player.GetRelic<PersonBionicJasmine>();
            if (bionicJasmine == null)
                continue;
            //茉莉获得随机步数
            var randomSteps = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                1,
                7,
                MainFile.ModId,
                Id.Entry,
                Owner.RunState.Rng.StringSeed,
                Owner.NetId,
                player.NetId,
                CombatState.RoundNumber,
                bionicJasmine.AstralParty_PersonBionicJasmineSteps);
            bionicJasmine.AddSteps(randomSteps);
        }
    }
}
