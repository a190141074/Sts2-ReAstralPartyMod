using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class BoundaryReinforcementPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public bool TookHostileDamage;
    }

    public const decimal MaxDuration = 2m;
    public const decimal StrengthBonus = 3m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override int DisplayAmount => (int)Amount;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || target != Owner)
            return;
        if (result.UnblockedDamage <= 0m)
            return;
        if (!IsHostileSource(dealer, cardSource))
            return;

        GetInternalData<Data>().TookHostileDamage = true;
        Flash();
        await PowerCmd.Remove(this);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;
        if (Amount < MaxDuration)
            return;
        if (GetInternalData<Data>().TookHostileDamage)
            return;

        await PowerCmd.TickDownDuration(this);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (Owner == null || Owner.Player != player)
            return;
        if (Amount != 1m)
            return;

        Flash();
        await ApplyTemporaryStrength(Owner, StrengthBonus, this, Owner, null);
        await PowerCmd.Remove(this);
    }

    public static async Task ApplyTemporaryStrength(Creature owner, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        AbstractModel originModel = cardSource != null ? cardSource : ModelDb.Power<BoundaryReinforcementPower>();
        await ApplyTemporaryStrength(owner, amount, originModel, applier, cardSource);
    }

    public static Task ApplyTemporaryStrength(Creature owner, decimal amount, AbstractModel originModel,
        Creature? applier,
        CardModel? cardSource)
    {
        if (amount <= 0m)
            return Task.CompletedTask;

        return AstralTemporaryStrengthPower.Apply(owner, amount, originModel, applier, cardSource);
    }

    private bool IsHostileSource(Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null)
            return dealer.Side != Owner?.Side;

        var cardOwnerCreature = cardSource?.Owner?.Creature;
        return cardOwnerCreature != null && cardOwnerCreature.Side != Owner?.Side;
    }
}