using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class CounterPower : AstralPartyPowerModel
{
    private static int _activeRetaliations;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        if (Amount <= 0) return;
        if (result.UnblockedDamage <= 0) return;
        if (dealer == null || dealer == Owner || dealer.IsDead) return;
        if (_activeRetaliations > 0) return;

        Flash();

        try
        {
            // Retaliation damage should not recursively trigger other retaliation-style effects.
            _activeRetaliations++;
            var retaliateDamage = result.UnblockedDamage + Owner.GetPowerAmount<StrengthPower>();
            if (retaliateDamage <= 0m)
                return;

            await CreatureCmd.Damage(choiceContext, dealer, retaliateDamage,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner, null);
        }
        finally
        {
            _activeRetaliations--;
        }

        await PowerCmd.Decrement(this);
    }
}