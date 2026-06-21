using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class BaseAbilityYouHaveMeToo : BaseAbilityCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override bool IsPlayable => BaseAbilityHelper.HasOtherLivingPlayerTarget(Owner);

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BaseAbilityYouHaveMeToo() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || !BaseAbilityHelper.IsOtherLivingPlayerTarget(Owner, cardPlay.Target))
            return;

        var targetPlayer = cardPlay.Target!.Player!;
        await BaseAbilityHelper.GrantDeterministicBaseAbilityToHand(
            Owner,
            targetPlayer,
            this,
            MainFile.ModId,
            Id.Entry,
            "target",
            Owner.RunState.Rng.StringSeed,
            Owner.NetId,
            targetPlayer.NetId,
            Owner.Creature!.CombatState!.RoundNumber);
    }
}
