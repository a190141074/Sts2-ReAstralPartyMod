using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropLightFluxPauldron : MoonPropStackableRelicBase
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("CostReductionPercent", GetCostReductionPercentText()),
        new StringVar("FinalDamageReductionPercent", GetFinalDamageReductionPercentText())
    ];

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (Owner == null || card.Owner != Owner)
            return false;
        if (card.EnergyCost.CostsX || originalCost < 0m)
            return false;
        if (originalCost == 0m)
        {
            modifiedCost = 0m;
            return false;
        }

        modifiedCost = decimal.Floor(originalCost * GetFinalDamageMultiplier());
        if (modifiedCost < 1m)
            modifiedCost = 1m;

        return modifiedCost != originalCost;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (amount <= 0m)
            return 1m;
        if (Owner?.Creature == null)
            return 1m;
        if (dealer != Owner.Creature && !(dealer == null && cardSource?.Owner == Owner))
            return 1m;

        return GetFinalDamageMultiplier();
    }

    private decimal GetCostReductionRatio()
    {
        return MoonPropFormulaHelper.GetHalfDecayAccumulatedRatio(GetStacks());
    }

    private decimal GetFinalDamageMultiplier()
    {
        return MoonPropFormulaHelper.GetHalfDecayRatio(GetStacks());
    }

    private string GetCostReductionPercentText()
    {
        return FormatPercent(GetCostReductionRatio());
    }

    private string GetFinalDamageReductionPercentText()
    {
        return FormatPercent(1m - GetFinalDamageMultiplier());
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("CostReductionPercent", GetCostReductionPercentText());
        SetDynamicString("FinalDamageReductionPercent", GetFinalDamageReductionPercentText());
    }
}
