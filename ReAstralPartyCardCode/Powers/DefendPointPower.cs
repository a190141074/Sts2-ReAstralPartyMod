using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class DefendPointPower : CardPointPowerBase
{
    [SavedProperty] public decimal AstralParty_DefendPointPowerLastAcceptedAmount { get; set; }

    public override LocString Title => new("powers", "RE_ASTRAL_PARTY_MOD_POWER_DEFEND_POINT_POWER.title");

    public override LocString Description =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_DEFEND_POINT_POWER.description");

    protected override string SmartDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_DEFEND_POINT_POWER.smartDescription";

    protected override decimal LastAcceptedAmount
    {
        get => AstralParty_DefendPointPowerLastAcceptedAmount;
        set => AstralParty_DefendPointPowerLastAcceptedAmount = value;
    }

    protected override bool QualifiesForPointSpend(CardModel card)
    {
        return DefendPointCardQualificationHelper.CountsAsDefendPointCard(card);
    }

    internal static Task GrantFromRelic(Creature target, decimal amount, RelicModel sourceRelic)
    {
        return GrantFromRelic<DefendPointPower>(target, amount, sourceRelic);
    }
}
