using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class EnigmaticSynthesisEtheriumArmorRelicBase : AstralPartyRelicModel
{
    private const decimal LowHpDamageTakenMultiplier = 0.5m;
    private const decimal CounterTriggerAmount = 1m;

    protected virtual decimal FullPlatingAmount => 0m;
    protected virtual decimal FullRegenAmount => 0m;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        var platingAmount = EnigmaticSynthesisEtheriumSetHelper.GetEffectiveAmount(
            Owner,
            FullPlatingAmount);
        var regenAmount = EnigmaticSynthesisEtheriumSetHelper.GetEffectiveAmount(Owner, FullRegenAmount);
        if (platingAmount <= 0m && regenAmount <= 0m)
            return;

        Flash();
        if (platingAmount > 0m)
            await PowerCmd.Apply<PlatingPower>(Owner.Creature, platingAmount, Owner.Creature, null, false);

        if (regenAmount > 0m)
            await PowerCmd.Apply<RegenPower>(Owner.Creature, regenAmount, Owner.Creature, null, false);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner?.Creature)
            return 1m;
        if (!EnigmaticSynthesisEtheriumSetHelper.IsSetEnabled(Owner))
            return 1m;
        if (!EnigmaticSynthesisEtheriumSetHelper.IsSetHost(Owner, this))
            return 1m;
        if (!EnigmaticSynthesisEtheriumSetHelper.IsLowHp(target))
            return 1m;

        return LowHpDamageTakenMultiplier;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner?.Creature)
            return;
        if (!EnigmaticSynthesisEtheriumSetHelper.IsSetEnabled(Owner))
            return;
        if (!EnigmaticSynthesisEtheriumSetHelper.IsSetHost(Owner, this))
            return;
        if (dealer == null || dealer.Side == target.Side || dealer.IsDead)
            return;
        if (result.UnblockedDamage <= 0m)
            return;
        if (!EnigmaticSynthesisEtheriumSetHelper.WasLowHpBeforeDamage(target, result))
            return;

        var existingCounterAmount = target.GetPowerAmount<CounterPower>();

        Flash();
        await PowerCmd.Apply<CounterPower>(target, CounterTriggerAmount, target, null, false);
        var didTrigger = await CounterPower.TryTriggerCounter(choiceContext, target, dealer, result.UnblockedDamage, this);
        if (didTrigger)
            return;

        var counterPower = target.GetPower<CounterPower>();
        if (counterPower != null && counterPower.Amount > existingCounterAmount)
            await PowerCmd.Decrement(counterPower);
    }
}
