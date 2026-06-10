using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropEulogyZero : MoonPropStackableRelicBase
{
    private const int BaseReplaceChancePermille = 140;
    private const int ExtraStackReplaceChancePermille = 70;

    [SavedProperty] public int AstralParty_MoonPropEulogyZeroRollCounter { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("ReplaceChancePercent", GetReplaceChancePercentText())
    ];

    public int GetReplaceChancePermille()
    {
        return BaseReplaceChancePermille + ExtraStackReplaceChancePermille * Math.Max(0, GetStacks() - 1);
    }

    public bool RollShouldReplace(RelicModel originalRelic)
    {
        if (Owner == null)
            return false;

        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            1000,
            MainFile.ModId,
            RelicId,
            nameof(RollShouldReplace),
            Owner.RunState?.Rng.StringSeed ?? "<null_seed>",
            Owner.RunState?.CurrentActIndex ?? -1,
            Owner.RunState?.TotalFloor ?? -1,
            Owner.NetId,
            GetCanonicalRelicId(originalRelic).Entry,
            AstralParty_MoonPropEulogyZeroRollCounter++);
        return roll < GetReplaceChancePermille();
    }

    private string GetReplaceChancePercentText()
    {
        return FormatValue(GetReplaceChancePermille() / 10m) + "%";
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("ReplaceChancePercent", GetReplaceChancePercentText());
    }

    private static ModelId GetCanonicalRelicId(RelicModel relic)
    {
        return (relic.CanonicalInstance ?? relic).Id;
    }
}
