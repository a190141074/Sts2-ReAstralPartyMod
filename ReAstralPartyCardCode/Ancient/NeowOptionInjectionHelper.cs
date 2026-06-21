using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Ancient;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib;
using STS2RitsuLib.Scaffolding.Ancients.Options;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Ancient;

internal static class NeowOptionInjectionHelper
{
    private const string CandidatePoolVersion = "v1";
    private static readonly object SyncLock = new();
    private const string DreamFaceTheShadowTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.DREAM_FACE_THE_SHADOW";
    private const string RingOfSevenCursesTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.RING_OF_SEVEN_CURSES";
    private const string AbsoluteFormTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.ABSOLUTE_FORM";
    private const string ProphecySoulDevourTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.PROPHECY_SOUL_DEVOUR";
    private const string ProphecyReplicantGroupTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.PROPHECY_REPLICANT_GROUP";
    private const string DreamCoinExplosionTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.DREAM_COIN_EXPLOSION";
    private const string DreamDisintegrationClawTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.DREAM_DISINTEGRATION_CLAW";
    private const string TetraWarforgeTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.TETRA_WARFORGE";
    private const string DreamFaceTheShadowIconPath =
        "res://ReAstralPartyMod/images/ancient/dream_face_the_shadow.png";
    private const string RingOfSevenCursesIconPath =
        "res://ReAstralPartyMod/images/relic/enigmatic_seven_curses.png";
    private const string AbsoluteFormIconPath =
        "res://ReAstralPartyMod/images/powers/absolute_form_power.png";
    private const string ProphecySoulDevourIconPath =
        "res://ReAstralPartyMod/images/relic/prophecy_soul_devour.png";
    private const string ProphecyReplicantGroupIconPath =
        "res://ReAstralPartyMod/images/relic/prophecy_replicant_group.png";
    private const string DreamCoinExplosionIconPath =
        "res://ReAstralPartyMod/images/relic/dream_coin_explosion.png";
    private const string DreamDisintegrationClawIconPath =
        "res://ReAstralPartyMod/images/relic/dream_disintegration_claw.png";
    private const string TetraWarforgeIconPath =
        "res://ReAstralPartyMod/images/relic/tetra_holo_sphere.png";
    private static readonly string[] CardCollectionMemberNames =
    [
        "Cards",
        "_cards",
        "CardModels",
        "MasterDeck",
        "StartingDeck"
    ];

    private static readonly string[] RunDeckMemberNames =
    [
        "Deck",
        "Cards",
        "CardModels",
        "MasterDeck",
        "StartingDeck"
    ];

    private static readonly MethodInfo? DoneMethod = typeof(AncientEventModel)
        .GetMethod("Done", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly IReadOnlyList<NeowOptionCandidateDefinition> CandidateDefinitions =
    [
        new(
            "dream_face_the_shadow",
            DreamFaceTheShadowTextKey,
            DreamFaceTheShadowIconPath,
            CreateForgottenRoarHoverTips,
            ChooseDreamFaceTheShadow),
        new(
            "ring_of_seven_curses",
            RingOfSevenCursesTextKey,
            RingOfSevenCursesIconPath,
            CreateRingOfSevenCursesHoverTips,
            ChooseRingOfSevenCurses),
        new(
            "absolute_form",
            AbsoluteFormTextKey,
            AbsoluteFormIconPath,
            CreateAbsoluteFormHoverTips,
            ChooseAbsoluteForm),
        new(
            "prophecy_soul_devour",
            ProphecySoulDevourTextKey,
            ProphecySoulDevourIconPath,
            CreateProphecySoulDevourHoverTips,
            ChooseProphecySoulDevour),
        new(
            "prophecy_replicant_group",
            ProphecyReplicantGroupTextKey,
            ProphecyReplicantGroupIconPath,
            CreateProphecyReplicantGroupHoverTips,
            ChooseProphecyReplicantGroup),
        new(
            "dream_coin_explosion",
            DreamCoinExplosionTextKey,
            DreamCoinExplosionIconPath,
            CreateDreamCoinExplosionHoverTips,
            ChooseDreamCoinExplosion),
        new(
            "dream_disintegration_claw",
            DreamDisintegrationClawTextKey,
            DreamDisintegrationClawIconPath,
            CreateDreamDisintegrationClawHoverTips,
            ChooseDreamDisintegrationClaw),
        new(
            "tetra_warforge",
            TetraWarforgeTextKey,
            TetraWarforgeIconPath,
            CreateTetraWarforgeHoverTips,
            ChooseTetraHoloSphere)
    ];
    private static readonly Dictionary<string, string> SelectedCandidateKeysByRun = [];

    public static void Register()
    {
        MainFile.Logger.Info(
            "[NeowOptionInjectionHelper] Registering randomized custom Neow option pool through RitsuLib ancient-option registry.");
        foreach (var candidate in CandidateDefinitions)
        {
            RitsuLibFramework.RegisterAncientOption<Neow>(
                MainFile.ModId,
                ModAncientOptionRule.Single(
                    ancient => candidate.CreateOption(ancient),
                    ancient => IsCandidateSelected(ancient, candidate.StableKey)));
        }
    }

    internal static IReadOnlyList<EventOption> EnsureSelectedCustomOptionPresent(
        AncientEventModel ancient,
        IEnumerable<EventOption>? currentOptions,
        string sourceTag)
    {
        var options = currentOptions?.ToList() ?? [];
        var selectedCandidate = ResolveSelectedCandidateDefinition(ancient);
        if (selectedCandidate == null)
            return options;

        if (options.Any(option =>
                string.Equals(option.TextKey, selectedCandidate.TextKey, StringComparison.OrdinalIgnoreCase)))
            return options;

        // Ready-page injection can snapshot Neow before RitsuLib's ancient-option list is fully materialized.
        // Re-append the already-selected custom option so restoring the original page doesn't drop it.
        options.Add(selectedCandidate.CreateOption(ancient));
        MainFile.Logger.Info(
            $"[NeowOptionInjectionHelper] Re-appended selected custom Neow option | runKey={GetRunKey(ancient.Owner?.RunState as RunState, ancient.Owner)} | source={sourceTag} | key={selectedCandidate.StableKey} | optionCount={options.Count}.");
        return options;
    }

    public static string? ResolveCustomNeowOptionIconPath(string? textKey)
    {
        foreach (var candidate in CandidateDefinitions)
        {
            if (string.Equals(textKey, candidate.TextKey, StringComparison.OrdinalIgnoreCase))
                return candidate.IconPath;
        }

        return null;
    }

    private static bool IsCandidateSelected(AncientEventModel ancient, string stableKey)
    {
        return string.Equals(ResolveSelectedCandidateKey(ancient), stableKey, StringComparison.Ordinal);
    }

    private static NeowOptionCandidateDefinition? ResolveSelectedCandidateDefinition(AncientEventModel ancient)
    {
        var selectedKey = ResolveSelectedCandidateKey(ancient);
        if (string.IsNullOrWhiteSpace(selectedKey))
            return null;

        return CandidateDefinitions.FirstOrDefault(candidate =>
            string.Equals(candidate.StableKey, selectedKey, StringComparison.Ordinal));
    }

    private static string? ResolveSelectedCandidateKey(AncientEventModel ancient)
    {
        var runState = ancient.Owner?.RunState as RunState;
        var configuredSelectionMode = ReAstralPartyModSettingsManager.GetNeowExtraOptionSelectionMode(runState);
        if (configuredSelectionMode != NeowExtraOptionSelectionMode.DefaultRandom)
        {
            var forcedKey = ReAstralPartyModSettingsManager.TryGetForcedNeowExtraOptionStableKey(configuredSelectionMode);
            if (!string.IsNullOrWhiteSpace(forcedKey))
            {
                var forcedRunKey = GetRunKey(runState, ancient.Owner);
                lock (SyncLock)
                    SelectedCandidateKeysByRun[forcedRunKey] = forcedKey;
                return forcedKey;
            }
        }

        var eligibleCandidates = GetEligibleCandidates(runState);
        if (eligibleCandidates.Count == 0)
            return null;

        var runKey = GetRunKey(runState, ancient.Owner);
        lock (SyncLock)
        {
            if (SelectedCandidateKeysByRun.TryGetValue(runKey, out var cachedKey))
            {
                if (eligibleCandidates.Any(candidate => string.Equals(candidate.StableKey, cachedKey, StringComparison.Ordinal)))
                    return cachedKey;

                SelectedCandidateKeysByRun.Remove(runKey);
            }

            var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                eligibleCandidates.Count,
                MainFile.ModId,
                nameof(Neow),
                "custom_option_pool",
                CandidatePoolVersion,
                ancient.Owner?.RunState?.Rng.StringSeed ?? "<null_seed>",
                ancient.Owner?.RunState?.CurrentActIndex ?? -1,
                ancient.Owner?.NetId.ToString() ?? "<null_owner>");
            var selectedCandidate = eligibleCandidates[selectedIndex];
            SelectedCandidateKeysByRun[runKey] = selectedCandidate.StableKey;
            MainFile.Logger.Info(
                $"[NeowOptionInjectionHelper] Selected custom Neow option for run | runKey={runKey} | key={selectedCandidate.StableKey} | poolSize={eligibleCandidates.Count} | selectedIndex={selectedIndex}.");
            return selectedCandidate.StableKey;
        }
    }

    private static IReadOnlyList<NeowOptionCandidateDefinition> GetEligibleCandidates(RunState? runState)
    {
        if (!ReAstralPartyModSettingsManager.GetEnableNeowExtraOption(runState))
            return [];

        var eligible = new List<NeowOptionCandidateDefinition>(CandidateDefinitions.Count);
        foreach (var candidate in CandidateDefinitions)
        {
            if (string.Equals(candidate.StableKey, "dream_face_the_shadow", StringComparison.Ordinal)
                && !ReAstralPartyModSettingsManager.GetEnableDreamSeriesEvents(runState))
                continue;

            if (string.Equals(candidate.StableKey, "dream_coin_explosion", StringComparison.Ordinal)
                && !ReAstralPartyModSettingsManager.GetEnableDreamSeriesEvents(runState))
                continue;

            if (string.Equals(candidate.StableKey, "dream_disintegration_claw", StringComparison.Ordinal)
                && !ReAstralPartyModSettingsManager.GetEnableDreamSeriesEvents(runState))
                continue;

            if (string.Equals(candidate.StableKey, "ring_of_seven_curses", StringComparison.Ordinal)
                && (!ReAstralPartyModSettingsManager.GetEnableEnigmaticSeriesEvents(runState)
                    || ReAstralPartyModSettingsManager.GetEnableStartingRingOfSevenCurses(runState)))
                continue;

            eligible.Add(candidate);
        }

        return eligible;
    }

    private static string GetRunKey(RunState? runState, Player? owner)
    {
        if (runState != null)
            return StartingPersonRelicSelectionPatch.GetRunKey(runState);

        return $"{owner?.RunState?.Rng.StringSeed ?? "<null_seed>"}|{owner?.NetId.ToString() ?? "<null_owner>"}";
    }

    private static async Task ChooseDreamFaceTheShadow(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Dream Face the Shadow was chosen.");

        await AddDreamFaceTheShadowCardToDeck(owner);
        CompleteAncient(ancient);
    }

    private static async Task ChooseRingOfSevenCurses(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Ring of Seven Curses was chosen.");

        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(owner, ModelDb.Relic<EnigmaticSevenCurses>());
        CompleteAncient(ancient);
    }

    private static async Task ChooseAbsoluteForm(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Absolute Form was chosen.");

        await AddAbsoluteFormCardToDeck(owner);
        CompleteAncient(ancient);
    }

    private static async Task ChooseProphecySoulDevour(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Soul Devour was chosen.");

        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(owner, ModelDb.Relic<ProphecySoulDevour>());
        CompleteAncient(ancient);
    }

    private static async Task ChooseProphecyReplicantGroup(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Prophecy Replicant Group was chosen.");

        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(
            owner,
            ModelDb.Relic<ProphecyReplicantGroup>());
        var relic = owner.GetRelic<ProphecyReplicantGroup>();
        if (relic != null)
        {
            var initialStacks = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                4,
                11,
                MainFile.ModId,
                nameof(Neow),
                "prophecy_replicant_group_initial_stacks",
                owner.RunState?.Rng.StringSeed ?? "<null_seed>",
                owner.RunState?.CurrentActIndex ?? -1,
                owner.NetId.ToString());
            relic.InitializeStacks(initialStacks);
        }
        else
        {
            MainFile.Logger.Warn(
                $"[NeowOptionInjectionHelper] Failed to resolve live ProphecyReplicantGroup after deterministic obtain | owner={owner.NetId}.");
        }

        CompleteAncient(ancient);
    }

    private static async Task ChooseDreamCoinExplosion(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Dream Coin Explosion was chosen.");

        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(owner, ModelDb.Relic<DreamCoinExplosion>());
        CompleteAncient(ancient);
    }

    private static async Task ChooseDreamDisintegrationClaw(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Dream Disintegration Claw was chosen.");

        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(owner, ModelDb.Relic<DreamDisintegrationClaw>());
        CompleteAncient(ancient);
    }

    private static async Task ChooseTetraHoloSphere(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Tetra Warforge was chosen.");

        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(owner, ModelDb.Relic<TetraHoloSphere>());
        CompleteAncient(ancient);
    }

    private static Task AddDreamFaceTheShadowCardToDeck(Player owner)
    {
        return CardGainAttribution.RunWithSource(null, async () =>
        {
            var mutableCard = ModelDb.Card<UltimateSkillForgottenRoar>().ToMutable();
            mutableCard.FloorAddedToDeck = 1;
            if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, mutableCard))
                throw new InvalidOperationException(
                    $"Failed to add Forgotten Roar to run deck for player {owner.NetId}.");
        });
    }

    private static Task AddAbsoluteFormCardToDeck(Player owner)
    {
        return CardGainAttribution.RunWithSource(null, async () =>
        {
            var mutableCard = ModelDb.Card<UltimateSkillAbsoluteForm>().ToMutable();
            mutableCard.FloorAddedToDeck = 1;
            if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, mutableCard))
                throw new InvalidOperationException(
                    $"Failed to add Absolute Form to run deck for player {owner.NetId}.");
        });
    }

    private static void CompleteAncient(AncientEventModel ancient)
    {
        if (DoneMethod == null)
            throw new InvalidOperationException("Failed to resolve AncientEventModel.Done via reflection.");

        DoneMethod.Invoke(ancient, null);
    }

    private static IReadOnlyList<IHoverTip> CreateForgottenRoarHoverTips()
    {
        return [HoverTipFactory.FromCard<UltimateSkillForgottenRoar>()];
    }

    private static IReadOnlyList<IHoverTip> CreateRingOfSevenCursesHoverTips()
    {
        return [.. HoverTipFactory.FromRelic<EnigmaticSevenCurses>()];
    }

    private static IReadOnlyList<IHoverTip> CreateAbsoluteFormHoverTips()
    {
        return [HoverTipFactory.FromCard<UltimateSkillAbsoluteForm>()];
    }

    private static IReadOnlyList<IHoverTip> CreateProphecySoulDevourHoverTips()
    {
        return [.. HoverTipFactory.FromRelic<ProphecySoulDevour>()];
    }

    private static IReadOnlyList<IHoverTip> CreateProphecyReplicantGroupHoverTips()
    {
        return [.. HoverTipFactory.FromRelic<ProphecyReplicantGroup>()];
    }

    private static IReadOnlyList<IHoverTip> CreateDreamCoinExplosionHoverTips()
    {
        return [.. HoverTipFactory.FromRelic<DreamCoinExplosion>()];
    }

    private static IReadOnlyList<IHoverTip> CreateDreamDisintegrationClawHoverTips()
    {
        return [.. HoverTipFactory.FromRelic<DreamDisintegrationClaw>()];
    }

    private static IReadOnlyList<IHoverTip> CreateTetraWarforgeHoverTips()
    {
        return CreateSafeRelicHoverTips<TetraHoloSphere>("tetra_warforge");
    }

    private static IReadOnlyList<IHoverTip> CreateSafeRelicHoverTips<TRelic>(string candidateKey)
        where TRelic : RelicModel
    {
        try
        {
            return [.. HoverTipFactory.FromRelic<TRelic>()];
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[NeowOptionInjectionHelper] Failed to build relic hover tips for custom Neow option '{candidateKey}'; continuing without hover tips. {ex}");
            return [];
        }
    }

    private sealed record NeowOptionCandidateDefinition(
        string StableKey,
        string TextKey,
        string IconPath,
        Func<IReadOnlyList<IHoverTip>> HoverTipFactory,
        Func<AncientEventModel, Task> OnChosen,
        Func<AncientEventModel, bool>? Condition = null)
    {
        public EventOption CreateOption(AncientEventModel ancient)
        {
            return new EventOption(ancient, () => OnChosen(ancient), TextKey)
            {
                HoverTips = CreateHoverTipsSafely()
            };
        }

        private IReadOnlyList<IHoverTip> CreateHoverTipsSafely()
        {
            try
            {
                return HoverTipFactory();
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn(
                    $"[NeowOptionInjectionHelper] Failed to build hover tips for custom Neow option '{StableKey}'; continuing without hover tips. {ex}");
                return [];
            }
        }
    }
}
