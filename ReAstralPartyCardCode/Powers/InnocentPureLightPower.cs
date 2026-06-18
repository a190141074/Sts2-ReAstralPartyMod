using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class InnocentPureLightPower : AstralPartyPowerModel
{
    private const decimal BonusDamagePercent = 0.24m;
    private const decimal LowHpThreshold = 0.35m;
    private const decimal HighHpThreshold = 0.85m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override bool ShouldReceiveCombatHooks => true;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner)
            return 0m;
        if (!WarforgeEnchantmentHelper.CountsAsAttack(cardSource))
            return 0m;
        if (target == null || target.Side == Owner.Side || amount <= 0m)
            return 0m;
        if (!IsTargetWithinThreshold(target))
            return 0m;

        return amount * BonusDamagePercent;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    private static bool IsTargetWithinThreshold(Creature target)
    {
        if (target.MaxHp <= 0m)
            return false;

        var hpPercent = target.CurrentHp / target.MaxHp;
        return hpPercent < LowHpThreshold || hpPercent > HighHpThreshold;
    }
}
