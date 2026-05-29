using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class PendingExtraTurnQueuePower : AstralPartyPowerModel
{
    private const decimal ExtraTurnEnergyAmount = 2m;

    [SavedProperty] public int AstralParty_PendingExtraTurnQueueSaraPendingCount { get; set; }
    [SavedProperty] public int AstralParty_PendingExtraTurnQueueNightSkinPendingCount { get; set; }
    [SavedProperty] public int AstralParty_PendingExtraTurnQueuePendingEnergyCount { get; set; }
    [SavedProperty] public int AstralParty_PendingExtraTurnQueueReadyEnergyCount { get; set; }
    [SavedProperty] public int AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => 0;

    protected override bool IsVisibleInternal => false;

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return player.Creature == Owner && GetPendingCount() > 0;
    }

    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player.Creature != Owner)
            return;
        if (GetPendingCount() <= 0)
            return;

        if (AstralParty_PendingExtraTurnQueueSaraPendingCount > 0)
        {
            AstralParty_PendingExtraTurnQueueSaraPendingCount--;
            if (AstralParty_PendingExtraTurnQueuePendingEnergyCount > 0)
            {
                AstralParty_PendingExtraTurnQueuePendingEnergyCount--;
                AstralParty_PendingExtraTurnQueueReadyEnergyCount++;
            }

            if (AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount > 0)
                AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount--;
        }
        else if (AstralParty_PendingExtraTurnQueueNightSkinPendingCount > 0)
        {
            AstralParty_PendingExtraTurnQueueNightSkinPendingCount--;
        }

        await SyncState();
        MainFile.Logger.Info(
            $"[PendingExtraTurnQueue] Consumed extra turn | owner={player.NetId} | saraPending={AstralParty_PendingExtraTurnQueueSaraPendingCount} | nightSkinPending={AstralParty_PendingExtraTurnQueueNightSkinPendingCount} | pendingEnergy={AstralParty_PendingExtraTurnQueuePendingEnergyCount} | readyEnergy={AstralParty_PendingExtraTurnQueueReadyEnergyCount} | pendingShatter={AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount}");
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;
        if (AstralParty_PendingExtraTurnQueueReadyEnergyCount <= 0)
            return;

        AstralParty_PendingExtraTurnQueueReadyEnergyCount--;
        await PlayerCmd.GainEnergy(ExtraTurnEnergyAmount, player);
        await SyncState();
        MainFile.Logger.Info(
            $"[PendingExtraTurnQueue] Granted extra-turn energy | owner={player.NetId} | remainingReadyEnergy={AstralParty_PendingExtraTurnQueueReadyEnergyCount}");
    }

    public static async Task EnqueueSaraChargeExtraTurn(Player owner, bool grantEnergyOnExtraTurnStart = true)
    {
        var queue = await GetOrCreate(owner);
        if (queue == null)
            return;

        queue.AstralParty_PendingExtraTurnQueueSaraPendingCount++;
        if (grantEnergyOnExtraTurnStart)
            queue.AstralParty_PendingExtraTurnQueuePendingEnergyCount++;

        await queue.SyncState();
    }

    public static async Task EnqueueSaraShatterStarExtraTurn(Player owner)
    {
        var queue = await GetOrCreate(owner);
        if (queue == null)
            return;

        queue.AstralParty_PendingExtraTurnQueueSaraPendingCount++;
        queue.AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount++;
        await queue.SyncState();
    }

    public static async Task EnqueueNightSkinExtraTurn(Player owner)
    {
        var queue = await GetOrCreate(owner);
        if (queue == null)
            return;

        queue.AstralParty_PendingExtraTurnQueueNightSkinPendingCount++;
        await queue.SyncState();
    }

    public static async Task RestoreSaraShatterFallbackAtCombatStart(Player owner)
    {
        var sara = owner.GetRelic<VariantPersonSara>();
        if (sara == null || sara.AstralParty_VariantPersonSaraPendingShatterStarFallbackCount <= 0)
            return;

        var queue = await GetOrCreate(owner);
        if (queue == null)
            return;

        var fallbackCount = sara.AstralParty_VariantPersonSaraPendingShatterStarFallbackCount;
        sara.AstralParty_VariantPersonSaraPendingShatterStarFallbackCount = 0;
        queue.AstralParty_PendingExtraTurnQueueSaraPendingCount += fallbackCount;
        queue.AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount += fallbackCount;
        await queue.SyncState();
        MainFile.Logger.Info(
            $"[PendingExtraTurnQueue] Restored Sara shatter fallback for combat start | owner={owner.NetId} | restored={fallbackCount} | pending={queue.GetPendingCount()}");
    }

    public static async Task ConvertSaraUnresolvedShatterToFallbackAtCombatEnd(Player owner)
    {
        var queue = GetExisting(owner);
        var sara = owner.GetRelic<VariantPersonSara>();
        if (queue == null || sara == null)
            return;

        queue.AstralParty_PendingExtraTurnQueueReadyEnergyCount = 0;
        var fallbackCount = Math.Min(
            queue.AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount,
            queue.AstralParty_PendingExtraTurnQueueSaraPendingCount);
        if (fallbackCount > 0)
        {
            sara.AstralParty_VariantPersonSaraPendingShatterStarFallbackCount += fallbackCount;
            queue.AstralParty_PendingExtraTurnQueueSaraPendingCount -= fallbackCount;
            queue.AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount -= fallbackCount;
        }

        await queue.SyncState();
        MainFile.Logger.Info(
            $"[PendingExtraTurnQueue] Converted unresolved Sara shatter turns to fallback at combat end | owner={owner.NetId} | fallbackAdded={fallbackCount} | pending={queue.GetPendingCount()} | fallbackTotal={sara.AstralParty_VariantPersonSaraPendingShatterStarFallbackCount}");
    }

    public static int GetPendingCount(Player? owner)
    {
        var queue = owner?.Creature?.GetPower<PendingExtraTurnQueuePower>();
        return queue?.GetPendingCount() ?? 0;
    }

    private int GetPendingCount()
    {
        return Math.Max(AstralParty_PendingExtraTurnQueueSaraPendingCount, 0)
               + Math.Max(AstralParty_PendingExtraTurnQueueNightSkinPendingCount, 0);
    }

    private static PendingExtraTurnQueuePower? GetExisting(Player owner)
    {
        if (owner.Creature == null)
            return null;

        return owner.Creature.GetPower<PendingExtraTurnQueuePower>();
    }

    private static async Task<PendingExtraTurnQueuePower?> GetOrCreate(Player owner)
    {
        if (owner.Creature == null)
            return null;

        var existing = owner.Creature.GetPower<PendingExtraTurnQueuePower>();
        if (existing != null)
            return existing;

        await PowerCmd.Apply<PendingExtraTurnQueuePower>(owner.Creature, 1m, owner.Creature, null, false);
        var created = owner.Creature.GetPower<PendingExtraTurnQueuePower>();
        if (created == null)
            return null;

        created.AstralParty_PendingExtraTurnQueueSaraPendingCount = 0;
        created.AstralParty_PendingExtraTurnQueueNightSkinPendingCount = 0;
        created.AstralParty_PendingExtraTurnQueuePendingEnergyCount = 0;
        created.AstralParty_PendingExtraTurnQueueReadyEnergyCount = 0;
        created.AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount = 0;
        return created;
    }

    private async Task SyncState()
    {
        AstralParty_PendingExtraTurnQueueSaraPendingCount = Math.Max(AstralParty_PendingExtraTurnQueueSaraPendingCount, 0);
        AstralParty_PendingExtraTurnQueueNightSkinPendingCount = Math.Max(AstralParty_PendingExtraTurnQueueNightSkinPendingCount, 0);
        AstralParty_PendingExtraTurnQueuePendingEnergyCount = Math.Max(
            Math.Min(AstralParty_PendingExtraTurnQueuePendingEnergyCount, AstralParty_PendingExtraTurnQueueSaraPendingCount),
            0);
        AstralParty_PendingExtraTurnQueueReadyEnergyCount = Math.Max(AstralParty_PendingExtraTurnQueueReadyEnergyCount, 0);
        AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount = Math.Max(
            Math.Min(AstralParty_PendingExtraTurnQueuePendingShatterStarThisCombatCount, AstralParty_PendingExtraTurnQueueSaraPendingCount),
            0);

        var ownerPlayer = Owner?.Player;
        if (ownerPlayer == null || Owner == null)
            return;

        var desiredCarrierAmount = GetDesiredCarrierAmount();
        var delta = desiredCarrierAmount - Amount;
        if (delta != 0m)
            await PowerCmd.ModifyAmount(this, delta, Owner, null, true);

        await AstralMoveAgainDisplayHelper.Sync(ownerPlayer);
    }

    private decimal GetDesiredCarrierAmount()
    {
        if (GetPendingCount() > 0)
            return GetPendingCount();
        if (AstralParty_PendingExtraTurnQueueReadyEnergyCount > 0)
            return 1m;

        return 0m;
    }

}
