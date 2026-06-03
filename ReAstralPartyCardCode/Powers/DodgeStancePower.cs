using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class DodgeStancePower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public bool PendingWeaknessInsightGain;
        public bool HadCounterBeforeDodge;
        public decimal PendingCounterDamage;
        public decimal PendingDodgeBlockGain;
        public Creature? PendingCounterDealer;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Player == null || target != Owner)
            return amount;
        if (amount <= 0m || dealer == null || dealer.Side == Owner.Side)
            return amount;
        if (!dealer.HasPower<ExposedFlawPower>())
            return amount;

        var nodeValue = Math.Max(0, (int)Owner.GetPowerAmount<MosesNodePower>());
        var exposedFlaw = Math.Max(0, (int)dealer.GetPowerAmount<ExposedFlawPower>());
        if (nodeValue < exposedFlaw)
            return amount;

        var data = GetInternalData<Data>();
        data.PendingWeaknessInsightGain = true;
        data.HadCounterBeforeDodge = Owner.GetPowerAmount<CounterPower>() > 0m;
        data.PendingCounterDamage = amount;
        data.PendingDodgeBlockGain = Math.Ceiling(amount * 0.5m);
        data.PendingCounterDealer = dealer;
        Flash();
        return 0m;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Player == null || target != Owner)
            return;
        var data = GetInternalData<Data>();
        if (!data.PendingWeaknessInsightGain)
            return;

        data.PendingWeaknessInsightGain = false;
        if (data.HadCounterBeforeDodge)
            await CounterPower.TryTriggerCounter(choiceContext, Owner, data.PendingCounterDealer, data.PendingCounterDamage, this);

        if (data.PendingDodgeBlockGain > 0m)
            await CreatureCmd.GainBlock(Owner, data.PendingDodgeBlockGain, ValueProp.Move, null);

        data.HadCounterBeforeDodge = false;
        data.PendingCounterDamage = 0m;
        data.PendingDodgeBlockGain = 0m;
        data.PendingCounterDealer = null;
        await PowerCmd.Apply<CounterPower>(Owner, 1m, Owner, null, false);
        await MosesCombatHelper.TryGainWeaknessInsight(
            Owner.Player,
            (AbstractModel?)Owner.Player.GetRelic<PersonGunsmithMoses>() ?? this);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Player == null || Owner.Side != side)
            return;

        var ownerPlayer = Owner.Player;
        var currentBlock = Math.Max(0m, Owner.Block);
        if (currentBlock > 0m)
            await CreatureCmd.LoseBlock(Owner, Math.Ceiling(currentBlock * 0.5m));

        await MosesCombatHelper.EnsureNodeCarrier(ownerPlayer);
        var nextNode = MosesCombatHelper.RollDodgeNodeValue(ownerPlayer, this);
        await PowerCmd.SetAmount<MosesNodePower>(Owner, nextNode, Owner, null);
        var data = GetInternalData<Data>();
        data.PendingWeaknessInsightGain = false;
        data.HadCounterBeforeDodge = false;
        data.PendingCounterDamage = 0m;
        data.PendingDodgeBlockGain = 0m;
        data.PendingCounterDealer = null;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || Owner.Player != player)
            return;

        var data = GetInternalData<Data>();
        if (data.PendingWeaknessInsightGain)
        {
            data.PendingWeaknessInsightGain = false;
            if (data.HadCounterBeforeDodge)
                await CounterPower.TryTriggerCounter(choiceContext, Owner, data.PendingCounterDealer, data.PendingCounterDamage, this);

            if (data.PendingDodgeBlockGain > 0m)
                await CreatureCmd.GainBlock(Owner, data.PendingDodgeBlockGain, ValueProp.Move, null);

            data.HadCounterBeforeDodge = false;
            data.PendingCounterDamage = 0m;
            data.PendingDodgeBlockGain = 0m;
            data.PendingCounterDealer = null;
            await PowerCmd.Apply<CounterPower>(Owner, 1m, Owner, null, false);
            await MosesCombatHelper.TryGainWeaknessInsight(
                player,
                (AbstractModel?)player.GetRelic<PersonGunsmithMoses>() ?? this);
        }

        await PowerCmd.Remove(this);
    }
}
