using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Tags;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class CollectorsCardYouHaveMeToo : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override HashSet<CardTag> CanonicalTags => [AstralCardTags.Collectors];

    protected override bool IsPlayable => BaseAbilityHelper.HasOtherLivingPlayerTarget(Owner);

    public CollectorsCardYouHaveMeToo() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
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
        var roundNumber = Owner.Creature!.CombatState!.RoundNumber;
        await BaseAbilityHelper.GrantDeterministicBaseAbilityToHand(
            Owner,
            Owner,
            this,
            MainFile.ModId,
            Id.Entry,
            "recipient",
            Owner.RunState.Rng.StringSeed,
            Owner.NetId,
            Owner.NetId,
            targetPlayer.NetId,
            roundNumber);
        await BaseAbilityHelper.GrantDeterministicBaseAbilityToHand(
            Owner,
            targetPlayer,
            this,
            MainFile.ModId,
            Id.Entry,
            "recipient",
            Owner.RunState.Rng.StringSeed,
            Owner.NetId,
            targetPlayer.NetId,
            targetPlayer.NetId,
            roundNumber);
    }
}
