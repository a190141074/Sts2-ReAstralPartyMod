using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Glory))]
public sealed class DreamSparklingAndPurifyingWell : AstralPartyEventModel
{
    private const decimal MinimumGoldRequired = 50m;

    private static readonly LocString UpgradeSelectionPrompt =
        new("events", "RE_ASTRAL_PARTY_MOD_EVENT_DREAM_SPARKLING_AND_PURIFYING_WELL.select_upgrade");

    private static readonly LocString PurifySelectionPrompt =
        new("events", "RE_ASTRAL_PARTY_MOD_EVENT_DREAM_SPARKLING_AND_PURIFYING_WELL.select_purify");

    private static readonly LocString SparkleSelectionPrompt =
        new("events", "RE_ASTRAL_PARTY_MOD_EVENT_DREAM_SPARKLING_AND_PURIFYING_WELL.select_sparkle");

    protected override string EventId => "dream_sparkling_and_purifying_well";

    public override bool IsAllowed(IRunState runState)
    {
        var alreadyVisited = runState is RunState concreteRunState
                             && concreteRunState.VisitedEventIds.Contains(ModelDb.GetId<DreamSparklingAndPurifyingWell>());

        return !alreadyVisited
               && runState.CurrentActIndex == 2
               && runState.Players.All(player => player.Gold >= MinimumGoldRequired);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, UpgradeOneCard, InitialOptionKey("UPGRADE")),
            new EventOption(this, EnterPurifyingWell, InitialOptionKey("PURIFY"))
        ];
    }

    private async Task UpgradeOneCard()
    {
        ArgumentNullException.ThrowIfNull(Owner);

        var selectableCards = EventDeckCardHelper.GetUpgradeableUnupgradedCards(Owner);
        if (selectableCards.Count == 0)
        {
            SetEventFinished(PageDescription("NO_UPGRADEABLE_CARDS"));
            return;
        }

        var selectedCards = await SelectDeckCards(
            Owner,
            new CardSelectorPrefs(UpgradeSelectionPrompt, 1)
            {
                Cancelable = false,
                RequireManualConfirmation = true
            },
            static card => card.CurrentUpgradeLevel == 0 && card.IsUpgradable);
        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard == null)
        {
            SetEventFinished(PageDescription("NO_UPGRADEABLE_CARDS"));
            return;
        }

        await EventDeckCardMutationHelper.Upgrade(
            Owner,
            [selectedCard],
            "dream_sparkling_and_purifying_well.upgrade_one");
        SetEventFinished(PageDescription("UPGRADE_FINISH"));
    }

    private async Task EnterPurifyingWell()
    {
        ArgumentNullException.ThrowIfNull(Owner);

        var upgradedCards = EventDeckCardHelper.GetUpgradedCards(Owner);
        if (upgradedCards.Count == 0)
        {
            SetEventFinished(PageDescription("NO_PURIFIABLE_CARDS"));
            return;
        }

        var selectedCards = await SelectDeckCards(
            Owner,
            new CardSelectorPrefs(PurifySelectionPrompt, 1, upgradedCards.Count)
            {
                Cancelable = false,
                RequireManualConfirmation = true
            },
            static card => card.CurrentUpgradeLevel > 0);
        var selectedCount = selectedCards.Count;
        if (selectedCount <= 0)
        {
            SetEventFinished(PageDescription("NO_PURIFIABLE_CARDS"));
            return;
        }

        await EventDeckCardMutationHelper.Downgrade(
            Owner,
            selectedCards,
            "dream_sparkling_and_purifying_well.purify");

        var sparkleCount = selectedCount - (selectedCount / 9);
        if (sparkleCount <= 0)
        {
            SetEventFinished(PageDescription("PURIFY_ONLY_FINISH"));
            return;
        }

        var upgradeableCards = EventDeckCardHelper.GetUpgradeableUnupgradedCards(Owner);
        if (upgradeableCards.Count < sparkleCount)
        {
            SetEventFinished(PageDescription("INSUFFICIENT_SPARKLE_TARGETS"));
            return;
        }

        SetEventState(
            PageDescription("SPARKLING"),
            [new EventOption(this, () => ResolveSparklingWell(sparkleCount), ModOptionKey("SPARKLING", "SPARKLE"))]);
    }

    private async Task ResolveSparklingWell(int sparkleCount)
    {
        ArgumentNullException.ThrowIfNull(Owner);

        var upgradeableCards = EventDeckCardHelper.GetUpgradeableUnupgradedCards(Owner);
        if (upgradeableCards.Count < sparkleCount)
        {
            SetEventFinished(PageDescription("INSUFFICIENT_SPARKLE_TARGETS"));
            return;
        }

        var selectedCards = await SelectDeckCards(
            Owner,
            new CardSelectorPrefs(SparkleSelectionPrompt, sparkleCount, sparkleCount)
            {
                Cancelable = false,
                RequireManualConfirmation = true
            },
            static card => card.CurrentUpgradeLevel == 0 && card.IsUpgradable);
        if (selectedCards.Count != sparkleCount)
        {
            SetEventFinished(PageDescription("INSUFFICIENT_SPARKLE_TARGETS"));
            return;
        }

        await EventDeckCardMutationHelper.Upgrade(
            Owner,
            selectedCards,
            $"dream_sparkling_and_purifying_well.sparkle.{sparkleCount}");

        SetEventFinished(PageDescription("SPARKLE_FINISH"));
    }

    private static async Task<List<CardModel>> SelectDeckCards(
        Player owner,
        CardSelectorPrefs prefs,
        Func<CardModel, bool> filter)
    {
        var selectedCards = await CardSelectCmd.FromDeckGeneric(owner, prefs, filter);
        return selectedCards.ToList();
    }
}
