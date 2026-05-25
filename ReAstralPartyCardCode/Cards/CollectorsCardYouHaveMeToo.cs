using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class CollectorsCardYouHaveMeToo : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override bool IsPlayable => BaseAbilityHelper.HasOtherLivingPlayerTarget(Owner);

    public CollectorsCardYouHaveMeToo() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
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
            Owner,
            this,
            MainFile.ModId,
            Id.Entry,
            "self",
            Owner.RunState.Rng.StringSeed,
            Owner.NetId,
            Owner.Creature!.CombatState!.RoundNumber);
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
