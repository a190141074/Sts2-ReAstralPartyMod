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
public class MoonPropMercurialRachis : MoonPropStackableRelicBase
{
    private const decimal DamageMultiplierPerStack = 1.5m;

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("DamageBonusPercent", GetDamageBonusPercentText())
    ];

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (amount <= 0m)
            return 0m;

        return amount * GetDamageBonusRatio();
    }

    private decimal GetDamageBonusRatio()
    {
        return MoonPropFormulaHelper.GetRepeatedMultiplier(DamageMultiplierPerStack, GetStacks()) - 1m;
    }

    private string GetDamageBonusPercentText()
    {
        return FormatPercent(GetDamageBonusRatio());
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("DamageBonusPercent", GetDamageBonusPercentText());
    }
}
