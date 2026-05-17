using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class SkillInvokeSpirits : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override bool IsPlayable => HasOtherLivingPlayerTarget();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FoxfirePower>(),
        HoverTipFactory.FromPower<InvokeSpiritsPower>(),
        HoverTipFactory.FromPower<ExtraAttackPower>()
    ];

    public SkillInvokeSpirits() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.AnyAlly)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var owner = Owner;
        var ownerCreature = owner?.Creature;
        var target = cardPlay.Target;
        var targetPlayer = target?.Player;
        if (owner == null || ownerCreature == null || target == null || targetPlayer == null)
            return;
        if (targetPlayer == owner || !target.IsAlive)
            return;
        if (owner.GetRelic<PersonZhao>() is not { } personZhao)
            return;

        await personZhao.ReplaceInvokeTarget(targetPlayer, this);
        await GrantTemporaryStatsFromTarget(target, ownerCreature);
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

        return HasOtherLivingPlayerTarget() && CurrentTarget?.Player != Owner
            ? (pileType, position)
            : (PileType.Hand, CardPilePosition.Top);
    }

    private async Task GrantTemporaryStatsFromTarget(Creature target, Creature ownerCreature)
    {
        var strengthAmount = RoundUpPercent(target.GetPowerAmount<StrengthPower>(), 0.5m);
        var dexterityAmount = RoundUpPercent(target.GetPowerAmount<DexterityPower>(), 0.5m);
        var vigorAmount = RoundUpPercent(target.GetPowerAmount<VigorPower>(), 0.3m);
        var blockAmount = RoundUpPercent(target.Block, 0.3m);

        if (strengthAmount > 0m)
            await AstralTemporaryStrengthPower.Apply(ownerCreature, strengthAmount, this, ownerCreature, this, true);
        if (dexterityAmount > 0m)
            await AstralTemporaryDexterityPower.Apply(ownerCreature, dexterityAmount, this, ownerCreature, this, true);
        if (vigorAmount > 0m)
            await PowerCmd.Apply<VigorPower>(ownerCreature, vigorAmount, ownerCreature, this, false);
        if (blockAmount > 0m)
            await CreatureCmd.GainBlock(ownerCreature, blockAmount, ValueProp.Move, null);
    }

    private bool HasOtherLivingPlayerTarget()
    {
        var combatState = Owner?.Creature?.CombatState;
        if (Owner == null || combatState == null)
            return false;

        return combatState.PlayerCreatures.Any(creature => creature.IsAlive && creature.Player != Owner);
    }

    private static decimal RoundUpPercent(decimal value, decimal ratio)
    {
        if (value <= 0m)
            return 0m;

        return Math.Ceiling(value * ratio);
    }
}
