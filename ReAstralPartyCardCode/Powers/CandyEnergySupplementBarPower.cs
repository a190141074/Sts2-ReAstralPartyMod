using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class CandyEnergySupplementBarPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal ProcessedAmount;
        public decimal PendingAddedAmount;
        public int RemainingTurns = 2;
        public bool GrantedThisTurn;
        public decimal AppliedStrengthThisTurn;
        public decimal AppliedDexterityThisTurn;
    }

    private const decimal StrengthPerStack = 3m;
    private const decimal DexterityPerStack = 2m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override int DisplayAmount => (int)Amount;

    public override LocString Title =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_CANDY_ENERGY_SUPPLEMENT_BAR_POWER.title");

    public override LocString Description =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_CANDY_ENERGY_SUPPLEMENT_BAR_POWER.description");

    protected override string SmartDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_CANDY_ENERGY_SUPPLEMENT_BAR_POWER.smartDescription";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not CandyEnergySupplementBarPower || target != Owner || amount <= 0m)
            return false;

        var data = GetInternalData<Data>();
        data.PendingAddedAmount += amount;
        data.RemainingTurns = 2;
        return false;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || Amount <= 0m)
            return;

        var data = GetInternalData<Data>();
        var addedStacks = data.PendingAddedAmount > 0m
            ? data.PendingAddedAmount
            : Amount - data.ProcessedAmount;

        data.PendingAddedAmount = 0m;
        data.ProcessedAmount = Amount;
        data.RemainingTurns = 2;

        if (addedStacks <= 0m)
            return;

        await GrantForStacks(addedStacks, applier, cardSource);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Player != player || Amount <= 0m)
            return;

        var data = GetInternalData<Data>();
        if (data.RemainingTurns <= 0 || data.GrantedThisTurn)
            return;

        await GrantForStacks(Amount, Owner, null);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        var data = GetInternalData<Data>();
        if (data.AppliedStrengthThisTurn > 0m)
            await PowerCmd.Apply<StrengthPower>(Owner, -data.AppliedStrengthThisTurn, Owner, null, true);

        if (data.AppliedDexterityThisTurn > 0m)
            await PowerCmd.Apply<DexterityPower>(Owner, -data.AppliedDexterityThisTurn, Owner, null, true);

        data.AppliedStrengthThisTurn = 0m;
        data.AppliedDexterityThisTurn = 0m;
        data.RemainingTurns--;
        data.GrantedThisTurn = false;

        if (data.RemainingTurns <= 0)
            await PowerCmd.Remove(this);
    }

    private async Task GrantForStacks(decimal stacks, Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || stacks <= 0m)
            return;

        var data = GetInternalData<Data>();
        var strengthAmount = stacks * StrengthPerStack;
        var dexterityAmount = stacks * DexterityPerStack;

        data.GrantedThisTurn = true;
        data.AppliedStrengthThisTurn += strengthAmount;
        data.AppliedDexterityThisTurn += dexterityAmount;

        await PowerCmd.Apply<StrengthPower>(Owner, strengthAmount, applier, cardSource, true);
        await PowerCmd.Apply<DexterityPower>(Owner, dexterityAmount, applier, cardSource, true);
    }

    public static async Task Apply(Creature owner, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount <= 0m)
            return;

        await PowerCmd.Apply(
            ModelDb.Power<CandyEnergySupplementBarPower>().ToMutable(),
            owner,
            amount,
            applier,
            cardSource,
            false);
    }
}
