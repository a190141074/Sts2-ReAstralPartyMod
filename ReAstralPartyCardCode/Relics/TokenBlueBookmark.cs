using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenBlueBookmark : AstralPartyRelicModel
{
    private const decimal DamageAmount = 1m;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Card.Owner != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Skill || cardPlay.Card.Pool.IsColorless)
            return;

        var target = TerraprismaHelper.FindNearestLivingEnemy(Owner);
        if (target == null)
            return;

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            target,
            DamageAmount,
            ValueProp.Unpowered,
            Owner.Creature,
            cardPlay.Card);
    }
}
