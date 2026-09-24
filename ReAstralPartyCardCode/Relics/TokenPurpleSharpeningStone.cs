using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenPurpleSharpeningStone : AstralPartyRelicModel
{
    [SavedProperty] public decimal AstralParty_TokenPurpleSharpeningStoneAppliedStrength { get; set; }
    [SavedProperty] public decimal AstralParty_TokenPurpleSharpeningStoneAppliedScales { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<ReversedScalesHolographicPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        AstralParty_TokenPurpleSharpeningStoneAppliedStrength = 0m;
        AstralParty_TokenPurpleSharpeningStoneAppliedScales = 0m;
        await SyncHealthMode();
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner?.Creature || creature.CombatState == null)
            return;

        await SyncHealthMode();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetAppliedState();
        return Task.CompletedTask;
    }

    public override async Task AfterRemoved()
    {
        var oldOwner = Owner?.Creature;
        if (oldOwner != null)
            await CleanupAppliedState(oldOwner);
        else
            ResetAppliedState();
    }

    private async Task CleanupAppliedState(Creature oldOwner)
    {
        if (AstralParty_TokenPurpleSharpeningStoneAppliedStrength != 0m)
            await PowerCmd.Apply<StrengthPower>(
                oldOwner,
                -AstralParty_TokenPurpleSharpeningStoneAppliedStrength,
                oldOwner,
                null,
                true);

        if (AstralParty_TokenPurpleSharpeningStoneAppliedScales != 0m)
            await PowerCmd.Apply<ReversedScalesHolographicPower>(
                oldOwner,
                -AstralParty_TokenPurpleSharpeningStoneAppliedScales,
                oldOwner,
                null,
                true);

        ResetAppliedState();
    }

    private async Task SyncHealthMode()
    {
        var creature = Owner?.Creature;
        if (creature == null || creature.MaxHp <= 0m || !creature.IsAlive)
            return;

        var belowThirtyPercent = creature.CurrentHp * 100m < creature.MaxHp * 30m;
        var desiredStrength = belowThirtyPercent
            ? 4m
            : creature.CurrentHp * 100m < creature.MaxHp * 65m
                ? 2m
                : 0m;
        var desiredScales = belowThirtyPercent ? 1m : 0m;
        var strengthDelta = desiredStrength - AstralParty_TokenPurpleSharpeningStoneAppliedStrength;
        var scalesDelta = desiredScales - AstralParty_TokenPurpleSharpeningStoneAppliedScales;

        if (strengthDelta == 0m && scalesDelta == 0m)
            return;

        Flash();
        AstralParty_TokenPurpleSharpeningStoneAppliedStrength = desiredStrength;
        AstralParty_TokenPurpleSharpeningStoneAppliedScales = desiredScales;

        if (strengthDelta != 0m)
            await PowerCmd.Apply<StrengthPower>(creature, strengthDelta, creature, null, true);

        if (scalesDelta != 0m)
            await PowerCmd.Apply<ReversedScalesHolographicPower>(creature, scalesDelta, creature, null, true);
    }

    private void ResetAppliedState()
    {
        AstralParty_TokenPurpleSharpeningStoneAppliedStrength = 0m;
        AstralParty_TokenPurpleSharpeningStoneAppliedScales = 0m;
    }
}
