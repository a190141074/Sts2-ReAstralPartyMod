using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenExclusiveAdaptiveCore : AstralPartyRelicModel
{
    private const decimal MindThreshold = 3m;
    private const decimal StrengthAmount = 1m;
    private const decimal ReversedScalesAmount = 1m;

    private decimal _appliedStrengthAmount;
    private decimal _appliedReversedScalesAmount;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MindPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<ReversedScalesHolographicPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFinalArenaSeriesId)
    ];

    public override async Task BeforeCombatStart()
    {
        ResetAppliedState();
        await SyncMindMode(Owner?.Creature?.GetPowerAmount<MindPower>() ?? 0m, Owner?.Creature, null);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetAppliedState();
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;
        if (power.Owner != Owner.Creature)
            return;
        if (power is not MindPower)
            return;

        await SyncMindMode(amount, applier, cardSource);
    }

    private async Task SyncMindMode(decimal currentMind, Creature? applier, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;

        var wantsReversedScales = currentMind <= MindThreshold;
        var desiredStrength = wantsReversedScales ? 0m : StrengthAmount;
        var desiredReversedScales = wantsReversedScales ? ReversedScalesAmount : 0m;
        var strengthDelta = desiredStrength - _appliedStrengthAmount;
        var reversedScalesDelta = desiredReversedScales - _appliedReversedScalesAmount;

        if (strengthDelta != 0m)
        {
            _appliedStrengthAmount = desiredStrength;
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, strengthDelta, applier ?? Owner.Creature, cardSource,
                true);
        }

        if (reversedScalesDelta != 0m)
        {
            _appliedReversedScalesAmount = desiredReversedScales;
            await PowerCmd.Apply<ReversedScalesHolographicPower>(
                Owner.Creature,
                reversedScalesDelta,
                applier ?? Owner.Creature,
                cardSource,
                true);
        }
    }

    private void ResetAppliedState()
    {
        _appliedStrengthAmount = 0m;
        _appliedReversedScalesAmount = 0m;
    }
}
