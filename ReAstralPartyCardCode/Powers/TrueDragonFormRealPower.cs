using System;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class TrueDragonFormRealPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal GrantedScales;
        public bool StrengthApplied;
    }

    protected override string PowerId => "true_dragon_form_real_power";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null) return;
        var data = GetInternalData<Data>();
        if (data.StrengthApplied) return;
        data.StrengthApplied = true;
        await PowerCmd.Apply<StrengthPower>(Owner, 4m, applier, cardSource, true);
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (Owner == null || creature != Owner || delta >= 0m) return;
        var data = GetInternalData<Data>();
        await PowerCmd.Apply<ReversedScalesHolographicPower>(Owner, 1m, Owner, null, false);
        data.GrantedScales += 1m;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (Owner?.Player != player) return;
        var data = GetInternalData<Data>();
        if (data.GrantedScales <= 0m) return;
        var scales = Owner.GetPower<ReversedScalesHolographicPower>();
        if (scales != null)
            await PowerCmd.ModifyAmount(scales, -Math.Min(data.GrantedScales, scales.Amount), Owner, null, true);
        data.GrantedScales = 0m;
    }

    public override async Task AfterRemoved(Creature owner)
    {
        var data = GetInternalData<Data>();
        if (data.StrengthApplied) await PowerCmd.Apply<StrengthPower>(owner, -4m, owner, null, true);
    }
}
