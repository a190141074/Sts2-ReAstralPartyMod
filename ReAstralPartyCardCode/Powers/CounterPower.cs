using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

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
        await TryTriggerCounter(choiceContext, Owner, dealer, result.UnblockedDamage, this);
    }

    public static async Task<bool> TryTriggerCounter(
        PlayerChoiceContext choiceContext,
        Creature owner,
        Creature? dealer,
        decimal triggeringDamage,
        AbstractModel source)
    {
        if (owner == null || dealer == null)
            return false;

        var counterPower = owner.GetPower<CounterPower>();
        if (counterPower == null || counterPower.Amount <= 0m)
            return false;
        if (triggeringDamage <= 0m)
            return false;
        if (dealer == owner || dealer.IsDead)
            return false;
        if (_activeRetaliations > 0)
            return false;

        counterPower.Flash();

        try
        {
            // Retaliation damage should not recursively trigger other retaliation-style effects.
            _activeRetaliations++;
            decimal retaliateDamage = triggeringDamage + owner.GetPowerAmount<StrengthPower>();
            retaliateDamage += JingRuiPower.GetVigilDamageBonus(owner, retaliateDamage);
            if (owner.Player?.GetRelic<PersonalityDerivativePandaMeng>() != null)
                retaliateDamage += PandaPersonaHelper.GetEnemyAttackIntentSum(owner);
            if (retaliateDamage <= 0m)
                return false;

            await CreatureCmd.Damage(choiceContext, dealer, retaliateDamage,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim, owner, null);

            if (owner.Player?.GetRelic<PersonGunsmithMoses>() != null)
                await MosesCombatHelper.TryGainWeaknessInsight(owner.Player, source);

            if (owner.HasPower<InvokeSpiritsPower>() && dealer.IsAlive)
                await InvokeSpiritsPower.TryTriggerChase(choiceContext, owner, dealer, null, source);
        }
        finally
        {
            _activeRetaliations--;
        }

        await PowerCmd.Decrement(counterPower);
        return true;
    }
}
