using System.Collections.Generic;
using System;
using System.Reflection;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class ReadyToStrikePower : AstralPartyPowerModel
{
    private static readonly FieldInfo? DamagePropsField =
        typeof(AttackCommand).GetField("<DamageProps>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private const decimal InitialTemporaryStrengthAmount = 2m;
    private const decimal DrawnAttackTemporaryStrengthAmount = 1m;
    private const decimal DrawnNonAttackVigorAmount = 1m;

    private sealed class Data
    {
        public bool IsResolvingDraw;
        public int ManualDrawResolutionDepth;
        public decimal DrawnAttackTemporaryStrength;
        public decimal AppliedStrengthBonus;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("TemporaryStrength", 0m),
        new IntVar("Vigor", 0m)
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromCard<SkillMudTruckCrash>(),
        HoverTipFactory.FromPower<FracturePower>()
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        await SyncTemporaryStrength(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (Owner == null)
            return;

        if (power == this)
        {
            await SyncTemporaryStrength(applier, cardSource);
            return;
        }

        if (power is VigorPower && power.Owner == Owner)
            UpdateStatusDisplay(GetDesiredTemporaryStrength(), GetCurrentVigorAmount());
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (Owner?.Player == null)
            return Task.CompletedTask;
        if (command.Attacker != Owner)
            return Task.CompletedTask;
        if (command.TargetSide == Owner.Side)
            return Task.CompletedTask;
        if (command.ModelSource is not CardModel cardSource)
            return Task.CompletedTask;
        if (cardSource.Owner != Owner.Player || cardSource.Type != CardType.Attack)
            return Task.CompletedTask;

        DamagePropsField?.SetValue(command, command.DamageProps | ValueProp.Unblockable);
        return Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (Owner?.Player == null)
            return;
        if (GetInternalData<Data>().IsResolvingDraw)
            return;
        if (GetInternalData<Data>().ManualDrawResolutionDepth > 0)
            return;
        if (card.Owner != Owner.Player)
            return;
        if (card.Pile?.Type != PileType.Hand)
            return;

        await ResolveDrawnCard(choiceContext, card);
    }

    public IDisposable BeginManualDrawResolution()
    {
        GetInternalData<Data>().ManualDrawResolutionDepth++;
        return new ManualDrawResolutionScope(this);
    }

    public async Task ResolveDrawnCard(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (Owner?.Player == null)
            return;
        if (card.Owner != Owner.Player)
            return;
        if (card.Pile?.Type != PileType.Hand)
            return;

        var data = GetInternalData<Data>();
        data.IsResolvingDraw = true;
        try
        {
            Flash();
            if (card.Type == CardType.Attack)
            {
                GetInternalData<Data>().DrawnAttackTemporaryStrength += DrawnAttackTemporaryStrengthAmount;
                await SyncTemporaryStrength(Owner, card);
                await CardCmd.Discard(choiceContext, card);
                return;
            }

            await PowerCmd.Apply<VigorPower>(Owner, DrawnNonAttackVigorAmount, Owner, card, false);
            UpdateStatusDisplay(GetDesiredTemporaryStrength(), GetCurrentVigorAmount());
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom, this);
        }
        finally
        {
            data.IsResolvingDraw = false;
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        var data = GetInternalData<Data>();
        if (oldOwner != null && data.AppliedStrengthBonus != 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -data.AppliedStrengthBonus, oldOwner, null, true);

        data.AppliedStrengthBonus = 0m;
        data.DrawnAttackTemporaryStrength = 0m;
        UpdateStatusDisplay(0m, 0m);
    }

    private async Task SyncTemporaryStrength(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        var desiredStrength = GetDesiredTemporaryStrength();
        var delta = desiredStrength - data.AppliedStrengthBonus;
        UpdateStatusDisplay(desiredStrength, GetCurrentVigorAmount());

        if (delta == 0m)
            return;

        data.AppliedStrengthBonus = desiredStrength;
        await PowerCmd.Apply<StrengthPower>(Owner, delta, applier, cardSource, true);
    }

    private decimal GetDesiredTemporaryStrength()
    {
        var data = GetInternalData<Data>();
        return Amount * InitialTemporaryStrengthAmount + data.DrawnAttackTemporaryStrength;
    }

    private decimal GetCurrentVigorAmount()
    {
        return Owner?.GetPowerAmount<VigorPower>() ?? 0m;
    }

    private void UpdateStatusDisplay(decimal temporaryStrength, decimal vigor)
    {
        DynamicVars["TemporaryStrength"].BaseValue = temporaryStrength;
        DynamicVars["Vigor"].BaseValue = vigor;
        InvokeDisplayAmountChanged();
    }

    private sealed class ManualDrawResolutionScope : IDisposable
    {
        private readonly ReadyToStrikePower _power;
        private bool _disposed;

        public ManualDrawResolutionScope(ReadyToStrikePower power)
        {
            _power = power;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            var data = _power.GetInternalData<Data>();
            data.ManualDrawResolutionDepth = Math.Max(0, data.ManualDrawResolutionDepth - 1);
        }
    }
}
