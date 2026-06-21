using System.Text.Json;
using System.Threading;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonStokov : PersonRelicBase
{
    private static readonly AsyncLocal<int> CardPropagationDepth = new();
    private static readonly AsyncLocal<int> RelicPropagationDepth = new();

    private HashSet<string> _trackedStarterRelicIds = new(StringComparer.Ordinal);
    private HashSet<string> _trackedStarterCardIds = new(StringComparer.Ordinal);

    protected override string RelicId => "variant_person_stokov";

    [SavedProperty]
    private string AstralParty_VariantPersonStokovTrackedStarterRelicIds
    {
        get => StokovStarterBundleHelper.SerializeIdSet(_trackedStarterRelicIds);
        set => _trackedStarterRelicIds = StokovStarterBundleHelper.DeserializeIdSet(value);
    }

    [SavedProperty]
    private string AstralParty_VariantPersonStokovTrackedStarterCardIds
    {
        get => StokovStarterBundleHelper.SerializeIdSet(_trackedStarterCardIds);
        set => _trackedStarterCardIds = StokovStarterBundleHelper.DeserializeIdSet(value);
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await StokovStarterBundleHelper.GrantStarterBundleAsync(this);
    }

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
            return false;

        return StokovCardRewardRefreshHelper.TryEnableStandardCombatRewardReroll(player, rewards, room);
    }

    public override bool TryModifyCardRewardAlternatives(
        Player player,
        CardReward cardReward,
        List<CardRewardAlternative> alternatives)
    {
        if (player != Owner)
            return false;

        return StokovCardRewardRefreshHelper.TryReplaceRerollAlternative(player, cardReward, alternatives);
    }

    internal IReadOnlySet<string> GetTrackedStarterRelicIds()
    {
        return _trackedStarterRelicIds;
    }

    internal IReadOnlySet<string> GetTrackedStarterCardIds()
    {
        return _trackedStarterCardIds;
    }

    internal void SetTrackedStarterRelicIds(IEnumerable<string> ids)
    {
        _trackedStarterRelicIds = ids.ToHashSet(StringComparer.Ordinal);
    }

    internal void SetTrackedStarterCardIds(IEnumerable<string> ids)
    {
        _trackedStarterCardIds = ids.ToHashSet(StringComparer.Ordinal);
    }

    internal static bool TryEnterCardPropagation()
    {
        if (CardPropagationDepth.Value > 0)
            return false;

        CardPropagationDepth.Value++;
        return true;
    }

    internal static void ExitCardPropagation()
    {
        CardPropagationDepth.Value = Math.Max(0, CardPropagationDepth.Value - 1);
    }

    internal static bool TryEnterRelicPropagation()
    {
        if (RelicPropagationDepth.Value > 0)
            return false;

        RelicPropagationDepth.Value++;
        return true;
    }

    internal static void ExitRelicPropagation()
    {
        RelicPropagationDepth.Value = Math.Max(0, RelicPropagationDepth.Value - 1);
    }
}
