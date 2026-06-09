using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropCorpsebloom : MoonPropStackableRelicBase
{
    private const decimal BaseHealBonusPerStack = 1m;
    private const decimal BaseCombatHealCapRatio = 0.10m;

    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("HealBonusPercent", GetHealBonusPercentText()),
        new StringVar("CombatHealCapPercent", GetCombatHealCapPercentText())
    ];

    public decimal GetHealingMultiplier()
    {
        return 1m + BaseHealBonusPerStack * GetStacks();
    }

    public decimal GetCombatHealCapRatio()
    {
        return BaseCombatHealCapRatio * MoonPropFormulaHelper.GetHalfDecayRatio(Math.Max(GetStacks() - 1, 0));
    }

    public decimal GetModifiedHealAmount(decimal amount)
    {
        return amount <= 0m ? amount : amount * GetHealingMultiplier();
    }

    public decimal GetCombatHealCap(Creature creature)
    {
        return creature.MaxHp * GetCombatHealCapRatio();
    }

    private string GetHealBonusPercentText()
    {
        return FormatPercent(BaseHealBonusPerStack * GetStacks());
    }

    private string GetCombatHealCapPercentText()
    {
        return FormatPercent(GetCombatHealCapRatio());
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("HealBonusPercent", GetHealBonusPercentText());
        SetDynamicString("CombatHealCapPercent", GetCombatHealCapPercentText());
    }
}
