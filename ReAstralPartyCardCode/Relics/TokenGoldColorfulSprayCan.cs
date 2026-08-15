using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldColorfulSprayCan : AstralPartyRelicModel
{
    private const decimal BonusDamage = 5m;

    private readonly Dictionary<CardModel, Creature> _pendingTargetsByCard = [];

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>()
    ];

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        var owner = Owner;
        var ownerCreature = owner?.Creature;
        var target = cardPlay.Target;
        if (ownerCreature == null || target == null)
            return;
        if (_pendingTargetsByCard.ContainsKey(cardPlay.Card))
            return;
        if (cardPlay.Card.Owner != owner || !WarforgeEnchantmentHelper.CountsAsSkill(cardPlay.Card))
            return;
        if (target.Side == ownerCreature.Side)
            return;

        var mark = target.GetPower<MarkLockPower>();
        if (mark == null || mark.Amount <= 0m)
            return;

        await PowerCmd.Decrement(mark);
        _pendingTargetsByCard.TryAdd(cardPlay.Card, target);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_pendingTargetsByCard.Remove(cardPlay.Card, out var target))
            return;
        if (Owner?.Creature == null || !target.IsAlive)
            return;

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            target,
            BonusDamage,
            ValueProp.Unpowered,
            Owner.Creature,
            null);
    }
}
