using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
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

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenExclusiveTimer : AstralPartyRelicModel
{
    private const decimal ExtraModificationOnGain = 1m;
    private const decimal SetModificationPerTurn = 1m;
    private const decimal SetDoomPerTurn = 1m;
    private const int ModificationStacksPerTemporaryStrength = 2;

    private int _pendingTemporaryStrength;
    private bool _flushScheduled;

    [SavedProperty] public int AstralParty_TokenExclusiveTimerModificationProgress { get; set; }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ModificationPower>(),
        HoverTipFactory.FromPower<DoomPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralGhostAlleySet)
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

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetCombatTracking();
        return Task.CompletedTask;
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
        var gainedStacks = (int)decimal.Floor(modifiedAmount);

        if (gainedStacks > 0)
        {
            Flash();
            RecordModificationGain(gainedStacks);
        }

        return true;
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

    private void RecordModificationGain(int gainedStacks)
    {
        var totalStacks = AstralParty_TokenExclusiveTimerModificationProgress + gainedStacks;
        _pendingTemporaryStrength += totalStacks / ModificationStacksPerTemporaryStrength;
        AstralParty_TokenExclusiveTimerModificationProgress =
            totalStacks % ModificationStacksPerTemporaryStrength;

        if (_pendingTemporaryStrength > 0)
            ScheduleFlush();
    }

    private void ScheduleFlush()
    {
        if (_flushScheduled)
            return;

        _flushScheduled = true;
        _ = FlushPendingTemporaryStrengthAsync();
    }

    private async Task FlushPendingTemporaryStrengthAsync()
    {
        await Task.Yield();

        if (Owner?.Creature == null || _pendingTemporaryStrength <= 0)
        {
            _flushScheduled = false;
            return;
        }

        var pendingTemporaryStrength = _pendingTemporaryStrength;
        _pendingTemporaryStrength = 0;
        _flushScheduled = false;

        await BoundaryReinforcementPower.ApplyTemporaryStrength(
            Owner.Creature,
            pendingTemporaryStrength,
            Owner.Creature,
            null
        );
    }

    private void ResetCombatTracking()
    {
        AstralParty_TokenExclusiveTimerModificationProgress = 0;
        _pendingTemporaryStrength = 0;
        _flushScheduled = false;
    }
}