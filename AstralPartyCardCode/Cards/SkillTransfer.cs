using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Relics;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class SkillTransfer : AstralPartyCardModel
{
    private const int TransferGoldCost = 5;
    private const int TransferStarLightAmount = 5;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [AstralPartyMod.AstralPartyCardCode.Keywords.AstralKeywords.AstralCooldown];

    protected override bool IsPlayable =>
        Owner != null
        && Owner.Gold >= TransferGoldCost
        && HasOtherLivingPlayerTarget();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public SkillTransfer() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AnyAlly)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target ?? Owner?.Creature;
        if (!MeetsTransferConditions(target))
            return;

        var ownerCreature = Owner!.Creature!;
        var resolvedTarget = target!;
        await PlayerCmd.LoseGold(TransferGoldCost, Owner, GoldLossType.Spent);
        Owner.GetRelic<TokenGoldStarCoinHammer>()?.RefreshDisplayedBonusDamage();
        Owner.GetRelic<PersonalityDerivativeProprietressWealthism>()?.RecordTransferSpend(TransferGoldCost);
        await PowerCmd.Apply(
            ModelDb.Power<StarLightPower>().ToMutable(),
            resolvedTarget,
            TransferStarLightAmount,
            ownerCreature,
            this,
            false
        );

        Owner.GetRelic<PersonalityDerivativeProprietressWealthism>()?.IncreaseWealthCounter(1);
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (card != this)
            return (pileType, position);

        // Safety net: if runtime conditions are no longer valid, keep the card in hand.
        if (!MeetsTransferConditions(CurrentTarget))
            return (PileType.Hand, CardPilePosition.Top);

        return (pileType, position);
    }

    private bool HasOtherLivingPlayerTarget()
    {
        var combatState = Owner?.Creature?.CombatState;
        if (Owner == null || combatState == null)
            return false;

        return combatState.PlayerCreatures.Any(creature => creature.IsAlive && creature.Player != Owner);
    }

    private bool MeetsTransferConditions(Creature? target)
    {
        return Owner != null
               && Owner.Creature != null
               && Owner.Gold >= TransferGoldCost
               && target?.Player != null
               && target.Player != Owner
               && target.IsAlive
               && HasOtherLivingPlayerTarget();
    }
}
