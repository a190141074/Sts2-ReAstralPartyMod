using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class CosmosFreezesPower : AstralPartyPowerModel
{
    private const decimal BaseReductionDenominator = 100m;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override LocString Description
    {
        get
        {
            var description = new LocString("powers",
                "RE_ASTRAL_PARTY_MOD_POWER_COSMOS_FREEZES_POWER.description");
            description.Add("TemporaryStrengthLoss", GetEquivalentStrengthLoss());
            return description;
        }
    }

    protected override string SmartDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_COSMOS_FREEZES_POWER.smartDescription";

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not CosmosFreezesPower)
            return false;
        if (target != Owner)
            return false;
        return false;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return 0m;
        if (amount <= 0m || Amount <= 0m)
            return 0m;

        var reducedDamage = amount * BaseReductionDenominator / (BaseReductionDenominator + Amount);
        var reductionAmount = Math.Clamp(amount - reducedDamage, 0m, amount);
        return -reductionAmount;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;
        if (Amount <= 10m)
            return;
        if (PandaPersonaHelper.HasAttackIntent(Owner))
            return;

        var decayAmount = Math.Max(1m, Math.Floor(Amount * 0.10m));
        var newAmount = Math.Max(Amount - decayAmount, 0m);
        var delta = newAmount - Amount;
        if (delta != 0m)
            await PowerCmd.ModifyAmount(this, delta, Owner, null, true);

        if (newAmount <= 0m)
            await PowerCmd.Remove(this);
    }

    private int GetEquivalentStrengthLoss()
    {
        if (Owner == null || Amount <= 0m)
            return 0;

        var currentStrength = Math.Max(Owner.GetPowerAmount<StrengthPower>(), 0m);
        if (currentStrength <= 0m)
            return 0;

        var reducedStrength = currentStrength * BaseReductionDenominator / (BaseReductionDenominator + Amount);
        var strengthLoss = Math.Clamp(currentStrength - reducedStrength, 0m, currentStrength);
        return (int)Math.Ceiling(strengthLoss);
    }
}
