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
        if (nodeValue <= exposedFlaw)
            return amount;

        GetInternalData<Data>().PendingWeaknessInsightGain = true;
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
        if (!GetInternalData<Data>().PendingWeaknessInsightGain)
            return;

        GetInternalData<Data>().PendingWeaknessInsightGain = false;
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
            await CreatureCmd.LoseBlock(Owner, currentBlock);

        await MosesCombatHelper.EnsureNodeCarrier(ownerPlayer);
        var nextNode = MosesCombatHelper.RollDodgeNodeValue(ownerPlayer, this);
        await PowerCmd.SetAmount<MosesNodePower>(Owner, nextNode, Owner, null);
        GetInternalData<Data>().PendingWeaknessInsightGain = false;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || Owner.Player != player)
            return;

        if (GetInternalData<Data>().PendingWeaknessInsightGain)
        {
            GetInternalData<Data>().PendingWeaknessInsightGain = false;
            await PowerCmd.Apply<CounterPower>(Owner, 1m, Owner, null, false);
            await MosesCombatHelper.TryGainWeaknessInsight(
                player,
                (AbstractModel?)player.GetRelic<PersonGunsmithMoses>() ?? this);
        }

        await PowerCmd.Remove(this);
    }
}
