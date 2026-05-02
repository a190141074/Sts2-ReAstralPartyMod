using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusiveTimer : AstralPartyRelicModel
{
    private const decimal ExtraModificationOnGain = 1m;
    private const decimal SetModificationPerTurn = 1m;
    private const decimal SetDoomPerTurn = 1m;
    private const int ModificationStacksPerTemporaryStrength = 2;

    [SavedProperty] public int AstralParty_TokenExclusiveTimerModificationProgress { get; set; }
    [SavedProperty] public int AstralParty_TokenExclusiveTimerAppliedStrength { get; set; }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ModificationPower>(),
        HoverTipFactory.FromPower<DoomPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralGhostAlleySetId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ResetCombatTracking();
    }

    public override Task BeforeCombatStart()
    {
        ResetCombatTracking();
        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await RemoveAppliedStrength();
        ResetCombatTracking();
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (Owner?.Creature == null)
            return false;
        if (canonicalPower is not ModificationPower)
            return false;
        if (target != Owner.Creature)
            return false;
        if (amount <= 0m)
            return false;

        modifiedAmount += ExtraModificationOnGain;
        Flash();

        return true;
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || power.Owner != Owner.Creature || power is not ModificationPower)
            return;

        await SyncStrengthBonus();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState?.Encounter == null)
            return;

        var roomType = Owner.Creature.CombatState.Encounter.RoomType;
        if (roomType is not (RoomType.Elite or RoomType.Boss))
            return;

        Flash();
        await PowerCmd.Apply(
            ModelDb.Power<ModificationPower>().ToMutable(),
            Owner.Creature,
            SetModificationPerTurn,
            Owner.Creature,
            null,
            false
        );
        await PowerCmd.Apply<DoomPower>(Owner.Creature, SetDoomPerTurn, Owner.Creature, null, false);
    }

    private async Task SyncStrengthBonus()
    {
        if (Owner?.Creature == null)
            return;

        var desiredStrength = (int)(Owner.Creature.GetPowerAmount<ModificationPower>()
                                    / ModificationStacksPerTemporaryStrength);
        var delta = desiredStrength - AstralParty_TokenExclusiveTimerAppliedStrength;
        if (delta == 0)
            return;

        AstralParty_TokenExclusiveTimerAppliedStrength = desiredStrength;
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, delta, Owner.Creature, null, true);
    }

    private async Task RemoveAppliedStrength()
    {
        if (Owner?.Creature == null || AstralParty_TokenExclusiveTimerAppliedStrength == 0)
            return;

        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature,
            -AstralParty_TokenExclusiveTimerAppliedStrength,
            Owner.Creature,
            null,
            true);
    }

    private void ResetCombatTracking()
    {
        AstralParty_TokenExclusiveTimerModificationProgress = 0;
        AstralParty_TokenExclusiveTimerAppliedStrength = 0;
    }
}