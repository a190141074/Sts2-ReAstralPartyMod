using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillTransfer : AstralPartyCardModel
{
    private const int TransferGoldCost = 5;
    private const int TransferStarLightAmount = 5;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override bool IsPlayable =>
        Owner != null
        && Owner.Gold >= TransferGoldCost
        && HasAnyLivingPlayerTarget();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public SkillTransfer() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AnyPlayer)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var target = cardPlay.Target ?? Owner?.Creature;
        if (!MeetsTransferConditions(target))
            return;

        var ownerCreature = Owner!.Creature!;
        var resolvedTarget = target!;
        await PersonMultiplayerEffectHelper.LoseGoldDeterministic(TransferGoldCost, Owner, GoldLossType.Spent);
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

    private bool HasAnyLivingPlayerTarget()
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null)
            return false;

        return combatState.PlayerCreatures.Any(creature => creature.IsAlive && creature.Player != null);
    }

    private bool MeetsTransferConditions(Creature? target)
    {
        return Owner != null
               && Owner.Creature != null
               && Owner.Gold >= TransferGoldCost
               && target?.Player != null
               && target.IsAlive
               && HasAnyLivingPlayerTarget();
    }
}

