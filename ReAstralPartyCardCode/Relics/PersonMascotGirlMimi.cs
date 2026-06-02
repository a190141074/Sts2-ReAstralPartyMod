using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonMascotGirlMimi : CooldownPersonaRelicBase
{
    private const int DrawsPerTokenMemoryChoice = 25;
    private const int TokenChoiceCount = 3;
    private const int TokenChoiceRerolls = 1;
    private const int PreferredBankCardWeight = 3;

    [SavedProperty] public int AstralParty_PersonMascotGirlMimiCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonMascotGirlMimiPendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonMascotGirlMimiCounter;
        set => AstralParty_PersonMascotGirlMimiCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonMascotGirlMimiPendingCombatStartCard;
        set => AstralParty_PersonMascotGirlMimiPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillProductRestocking>(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress = 0;
        await EnsureTokenMemoryRelic();
    }

    public async Task HandleProductRestockingDraw(
        PlayerChoiceContext choiceContext,
        CardModel sourceCard,
        int cardsDrawn)
    {
        if (Owner?.Creature?.CombatState == null || cardsDrawn <= 0)
            return;

        var memoryRelic = await EnsureTokenMemoryRelic();
        if (memoryRelic == null)
            return;

        AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress += cardsDrawn;
        memoryRelic.RefreshProgressDisplay();

        while (AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress >= DrawsPerTokenMemoryChoice)
        {
            var granted = await TryGrantTokenAbilityChoice(choiceContext, sourceCard, memoryRelic);
            if (!granted)
                break;

            AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress -= DrawsPerTokenMemoryChoice;
            memoryRelic.RefreshProgressDisplay();
        }
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillProductRestocking>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }

    private async Task<PersonalityDerivativeMascotGirlMimiTokenMemory?> EnsureTokenMemoryRelic()
    {
        if (Owner == null)
            return null;

        var memoryRelic = MascotGirlMimiTokenMemoryHelper.GetMemoryRelic(Owner);
        if (memoryRelic != null)
            return memoryRelic;

        await PersonaMultiplayerEffectHelper
            .ObtainDerivativeRelicIfMissing<PersonalityDerivativeMascotGirlMimiTokenMemory>(
                Owner);

        return MascotGirlMimiTokenMemoryHelper.GetMemoryRelic(Owner);
    }

    private async Task<bool> TryGrantTokenAbilityChoice(
        PlayerChoiceContext choiceContext,
        CardModel sourceCard,
        PersonalityDerivativeMascotGirlMimiTokenMemory memoryRelic)
    {
        if (Owner?.Creature == null)
            return false;

        var availableTokenRelics = MascotGirlMimiTokenMemoryHelper
            .GetBridgeableUnownedTokenRelics(Owner, Owner.Creature, false)
            .ToList();

        if (availableTokenRelics.Count == 0)
            return false;

        var selectionOptions = BuildTokenChoiceOptions(sourceCard.Id.Entry, 0, new HashSet<ModelId>());
        var selectionTitle = new LocString("relics", $"{memoryRelic.Id.Entry}.selectionScreenHeader").GetRawText();
        var memoryGain = memoryRelic.PeekNextSelectionMemoryGain();

        using var _ = RelicSelectionHeaderContext.Push(selectionTitle);

        var selectionResult = await DeterministicMultiplayerChoiceHelper.SelectRefreshableRelicForPlayer(
            Owner,
            selectionOptions,
            TokenChoiceRerolls,
            selectionTitle,
            GetTokenChoiceSubtitlePrefix(),
            GetTokenChoiceHintText(memoryGain),
            $"{Id.Entry}.token-memory",
            (_, rerollOrdinal, historicalIds) =>
                BuildTokenChoiceOptions(sourceCard.Id.Entry, rerollOrdinal + 1, historicalIds),
            rerollHistory => RebuildTokenChoiceOptions(sourceCard.Id.Entry, rerollHistory));
        var selectedRelic = selectionResult.SelectedRelic;
        if (selectedRelic == null)
            return false;

        var bridgePower = await TokenRelicBridgeHelper.ApplyTokenRelicPower(
            Owner.Creature,
            selectedRelic.Id,
            Owner.Creature,
            sourceCard,
            false,
            TokenRelicBridgeInitializationMode.RunAfterObtainedSkipOneTimeRewards);

        if (bridgePower == null)
            return false;

        Flash();
        memoryRelic.Flash();
        memoryRelic.RecordTemporaryTokenGain(selectedRelic.Id, memoryGain);
        memoryRelic.MarkSuccessfulSelection();
        return true;
    }

    private static string GetTokenChoiceSubtitlePrefix()
    {
        return new LocString("relics", $"{ModelDb.Relic<PersonMascotGirlMimi>().Id.Entry}.selectionScreenSubtitlePrefix")
            .GetRawText();
    }

    private static string GetTokenChoiceHintText(int memoryGain)
    {
        var template = new LocString("relics", $"{ModelDb.Relic<PersonMascotGirlMimi>().Id.Entry}.selectionScreenHint")
            .GetRawText();
        return string.Format(template, memoryGain);
    }

    private IReadOnlyList<RelicModel> BuildTokenChoiceOptions(
        string sourceCardEntry,
        int rerollOrdinal,
        IReadOnlySet<ModelId> historicalIds)
    {
        if (Owner?.Creature == null)
            return [];

        var allCandidates = MascotGirlMimiTokenMemoryHelper
            .GetBridgeableUnownedTokenRelics(Owner, Owner.Creature, false)
            .ToList();
        if (allCandidates.Count == 0)
            return [];

        var filteredCandidates = allCandidates
            .Where(relic => !historicalIds.Contains(TokenRewardSelectionHelper.GetCanonicalId(relic)))
            .ToList();
        var candidates = filteredCandidates.Count >= TokenChoiceCount
            ? filteredCandidates
            : allCandidates;

        return PersonaMultiplayerEffectHelper.CreateWeightedDeterministicRelicChoiceOptions(
            candidates,
            TokenChoiceCount,
            relic => TokenRelicRegistry.IsBankCardTokenRelic(relic) ? PreferredBankCardWeight : 1,
            MainFile.ModId,
            Id.Entry,
            "token-memory-reroll",
            Owner.RunState.Rng.StringSeed,
            Owner.RunState.CurrentActIndex,
            Owner.NetId,
            AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress,
            sourceCardEntry,
            rerollOrdinal,
            string.Join(",", historicalIds
                .OrderBy(id => id.Entry, StringComparer.Ordinal)
                .Select(id => id.Entry)));
    }

    private IReadOnlyList<RelicModel> RebuildTokenChoiceOptions(
        string sourceCardEntry,
        IReadOnlyList<int> rerollHistory)
    {
        var options = BuildTokenChoiceOptions(sourceCardEntry, 0, new HashSet<ModelId>());
        if (options.Count == 0)
            return [];

        var seenIds = options
            .Select(TokenRewardSelectionHelper.GetCanonicalId)
            .ToHashSet();

        for (var rerollOrdinal = 0; rerollOrdinal < rerollHistory.Count; rerollOrdinal++)
        {
            options = BuildTokenChoiceOptions(sourceCardEntry, rerollOrdinal + 1, seenIds);
            if (options.Count == 0)
                break;

            seenIds.UnionWith(options.Select(TokenRewardSelectionHelper.GetCanonicalId));
        }

        return options;
    }
}
