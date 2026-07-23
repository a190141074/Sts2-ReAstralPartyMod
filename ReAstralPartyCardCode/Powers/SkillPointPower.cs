using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class SkillPointPower : CardPointPowerBase
{
    [SavedProperty] public decimal AstralParty_SkillPointPowerLastAcceptedAmount { get; set; }

    public override LocString Title => new("powers", "RE_ASTRAL_PARTY_MOD_POWER_SKILL_POINT_POWER.title");

    public override LocString Description =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_SKILL_POINT_POWER.description");

    protected override string SmartDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_SKILL_POINT_POWER.smartDescription";

    protected override decimal LastAcceptedAmount
    {
        get => AstralParty_SkillPointPowerLastAcceptedAmount;
        set => AstralParty_SkillPointPowerLastAcceptedAmount = value;
    }

    protected override bool QualifiesForPointSpend(CardModel card)
    {
        return WarforgeEnchantmentHelper.CountsAsSkill(card);
    }

    internal static Task GrantFromRelic(Creature target, decimal amount, RelicModel sourceRelic)
    {
        return GrantFromRelic<SkillPointPower>(target, amount, sourceRelic);
    }

    private static bool TryGetShortfallSkillPointModifiedCost(
        decimal originalCost,
        decimal availableEnergy,
        decimal availableSkillPoints,
        out decimal modifiedCost)
    {
        return TryGetShortfallPointModifiedCost(
            originalCost,
            availableEnergy,
            availableSkillPoints,
            out modifiedCost);
    }
}
