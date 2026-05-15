using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class AdherentMucusPower : AstralPartyPowerModel
{
    private const decimal MaxStacks = 3m;

    private sealed class Data
    {
        public decimal AppliedStrengthPenalty;
        public bool DealtUnblockedDamageToBoundSlimeThisTurn;
    }

    [SavedProperty] public string AstralParty_AdherentMucusBoundSlimePlayerNetIdRaw { get; set; } = string.Empty;

    public ulong AstralParty_AdherentMucusBoundSlimePlayerNetId
    {
        get => ulong.TryParse(AstralParty_AdherentMucusBoundSlimePlayerNetIdRaw, out var value) ? value : 0UL;
        set => AstralParty_AdherentMucusBoundSlimePlayerNetIdRaw = value.ToString();
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Math.Max((int)Amount, 0);

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not AdherentMucusPower)
            return false;
        if (target != Owner)
            return false;
        if (amount <= 0m)
            return false;

        modifiedAmount = Math.Clamp(amount, 0m, Math.Max(MaxStacks - Amount, 0m));
        return true;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await SyncStrengthPenalty(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this)
            return;

        await SyncStrengthPenalty(applier, cardSource);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        var data = GetInternalData<Data>();
        if (oldOwner != null && data.AppliedStrengthPenalty != 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -data.AppliedStrengthPenalty, oldOwner, null, true);

        data.AppliedStrengthPenalty = 0m;
        data.DealtUnblockedDamageToBoundSlimeThisTurn = false;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Player != player && Owner?.PetOwner != player)
            return Task.CompletedTask;

        GetInternalData<Data>().DealtUnblockedDamageToBoundSlimeThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner)
            return Task.CompletedTask;
        if (result.UnblockedDamage <= 0m)
            return Task.CompletedTask;
        if (target != GetBoundSlimeCreature())
            return Task.CompletedTask;

        GetInternalData<Data>().DealtUnblockedDamageToBoundSlimeThisTurn = true;
        return Task.CompletedTask;
    }

    public static async Task Apply(Creature target, Player slimeOwner, Creature? applier, CardModel? cardSource)
    {
        if (target == null || slimeOwner == null)
            return;

        var power = (AdherentMucusPower)ModelDb.Power<AdherentMucusPower>().ToMutable();
        power.AstralParty_AdherentMucusBoundSlimePlayerNetId = slimeOwner.NetId;
        await PowerCmd.Apply(power, target, 1m, applier, cardSource, false);
    }

    public static Task ResetRoundHitFlagForBoundSlime(CombatState combatState, ulong slimePlayerNetId)
    {
        if (combatState == null || slimePlayerNetId == 0)
            return Task.CompletedTask;

        foreach (var power in GetPowersBoundToSlime(combatState, slimePlayerNetId))
            power.GetInternalData<Data>().DealtUnblockedDamageToBoundSlimeThisTurn = false;

        return Task.CompletedTask;
    }

    public static async Task DecayAllForBoundSlimeIfMissed(CombatState combatState, ulong slimePlayerNetId)
    {
        if (combatState == null || slimePlayerNetId == 0)
            return;

        var boundPowers = GetPowersBoundToSlime(combatState, slimePlayerNetId).ToList();
        if (boundPowers.Count == 0)
            return;
        if (boundPowers.Any(power => power.GetInternalData<Data>().DealtUnblockedDamageToBoundSlimeThisTurn))
            return;

        foreach (var power in boundPowers)
            await power.TickDownOneStack();
    }

    private Creature? GetBoundSlimeCreature()
    {
        var owner = Owner;
        var combatState = owner?.CombatState;
        if (combatState == null || AstralParty_AdherentMucusBoundSlimePlayerNetId == 0)
            return null;

        return combatState.Players
            .FirstOrDefault(player => player.NetId == AstralParty_AdherentMucusBoundSlimePlayerNetId)
            ?.Creature;
    }

    private async Task SyncStrengthPenalty(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        var desiredPenalty = -Math.Clamp(Amount, 0m, MaxStacks);
        var delta = desiredPenalty - data.AppliedStrengthPenalty;
        if (delta == 0m)
            return;

        data.AppliedStrengthPenalty = desiredPenalty;
        await PowerCmd.Apply<StrengthPower>(Owner, delta, applier, cardSource, true);
    }

    private async Task TickDownOneStack()
    {
        if (Owner == null || Amount <= 0m)
            return;

        Flash();
        if (Amount <= 1m)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(this, -1m, Owner, null, true);
    }

    private static IEnumerable<AdherentMucusPower> GetPowersBoundToSlime(CombatState combatState,
        ulong slimePlayerNetId)
    {
        return combatState.Creatures
            .Select(creature => creature.GetPower<AdherentMucusPower>())
            .Where(power => power != null && power.AstralParty_AdherentMucusBoundSlimePlayerNetId == slimePlayerNetId)!;
    }
}
