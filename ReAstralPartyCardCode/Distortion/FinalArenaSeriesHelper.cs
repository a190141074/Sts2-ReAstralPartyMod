using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

public enum FinalArenaDistortionComponent
{
    HumanAiDuel,
    RefereeAttentionOne,
    RefereeAttentionTwo,
    PirateKingsOrder,
    Thundercloud,
    Rainstorm,
    Seasickness,
    EnergyDisorder,
    CopyCard,
    WideAreaMark,
    Robbery,
    Gale,
    Dexterity,
    Overload,
    Arbitrage,
    PreciseSelection,
    FlashOfInsight,
    MysteriousTreasure,
    UnexpectedWealth,
    Baton
}

public static class FinalArenaSeriesHelper
{
    private const string DistortionComponentsKey = "final_arena_distortion_components";
    private const string HoverTipRoot = "RE_ASTRAL_PARTY_MOD_DISTORTION_COMPONENT";
    private const int StunGunInitialMind = 1;

    private static readonly FinalArenaDistortionComponent[] RequiredAttentionComponents =
    [
        FinalArenaDistortionComponent.RefereeAttentionOne,
        FinalArenaDistortionComponent.RefereeAttentionTwo
    ];

    private static readonly FinalArenaDistortionComponent[] OptionalComponents =
    [
        FinalArenaDistortionComponent.PirateKingsOrder,
        FinalArenaDistortionComponent.Thundercloud,
        FinalArenaDistortionComponent.Rainstorm,
        FinalArenaDistortionComponent.Seasickness,
        FinalArenaDistortionComponent.EnergyDisorder,
        FinalArenaDistortionComponent.CopyCard,
        FinalArenaDistortionComponent.WideAreaMark,
        FinalArenaDistortionComponent.Robbery,
        FinalArenaDistortionComponent.Gale,
        FinalArenaDistortionComponent.Dexterity,
        FinalArenaDistortionComponent.Overload,
        FinalArenaDistortionComponent.Arbitrage,
        FinalArenaDistortionComponent.PreciseSelection,
        FinalArenaDistortionComponent.FlashOfInsight,
        FinalArenaDistortionComponent.MysteriousTreasure,
        FinalArenaDistortionComponent.UnexpectedWealth,
        FinalArenaDistortionComponent.Baton
    ];

    private static RunSavedData<FinalArenaSeriesRunData> _runData = null!;
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            _runData = RitsuLibFramework
                .GetRunSavedDataStore(MainFile.ModId)
                .Register(
                    DistortionComponentsKey,
                    static () => new FinalArenaSeriesRunData(),
                    new RunSavedDataOptions
                    {
                        WritePolicy = RunSavedDataWritePolicy.WhenNonDefault
                    });
        }

        _registered = true;
    }

    public static async Task EnsureInitializedAndAppliedAsync(
        RunState runState,
        bool broadcastIfHost = true,
        bool allowGenerateIfMissing = true)
    {
        if (!IsFinalArenaOpen(runState))
            return;

        var runData = GetOrCreateRunData(runState);
        if (!TryDeserializeComponents(runData.ComponentsJson, out var components) || components.Count == 0)
        {
            if (!allowGenerateIfMissing)
                return;

            components = GenerateComponents(runState);
            runData.ComponentsJson = SerializeComponents(components);
            _runData.Set(runState, runData);
        }

        FinalArenaDistortionComponentModifierInstaller.EnsureInstalled(runState, loadedRun: true);
        await ApplyOneTimeComponents(runState, runData, components);

        if (broadcastIfHost)
            FinalArenaDistortionComponentSync.BroadcastState(runState);
    }

    public static bool EnsureInitializedForUi(
        RunState runState,
        bool allowGenerateIfMissing = true)
    {
        if (!IsFinalArenaOpen(runState))
            return false;

        var runData = GetOrCreateRunData(runState);
        if (TryDeserializeComponents(runData.ComponentsJson, out var components) && components.Count > 0)
            return true;

        if (!allowGenerateIfMissing)
            return false;

        components = GenerateComponents(runState);
        runData.ComponentsJson = SerializeComponents(components);
        _runData.Set(runState, runData);
        return true;
    }

    public static bool TryGetActiveComponents(
        IRunState? runState,
        out IReadOnlyList<FinalArenaDistortionComponent> components)
    {
        components = [];
        if (!IsFinalArenaOpen(runState) || runState is not RunState concreteRunState)
            return false;

        if (!_registered)
            Register();

        if (!_runData.TryGet(concreteRunState, out var runData) || runData == null)
            return false;

        if (!TryDeserializeComponents(runData.ComponentsJson, out var parsed) || parsed.Count == 0)
            return false;

        components = parsed;
        return true;
    }

    public static bool HasComponent(IRunState? runState, FinalArenaDistortionComponent component)
    {
        return TryGetActiveComponents(runState, out var components) && components.Contains(component);
    }

    public static IReadOnlyList<IHoverTip> BuildDistortionHoverTips(IRunState? runState, Texture2D? icon)
    {
        if (!TryGetActiveComponents(runState, out var components))
            return [];

        var tips = new List<IHoverTip>
        {
            new HoverTip(
                new LocString("relics", $"{HoverTipRoot}.title"),
                new LocString("relics", $"{HoverTipRoot}.description"),
                icon)
        };

        foreach (var component in components)
        {
            var title = new LocString("relics", $"{HoverTipRoot}.{GetLocStem(component)}.title");
            title.Add("Difficulty", GetDifficulty(component));
            tips.Add(new HoverTip(
                title,
                new LocString("relics", $"{HoverTipRoot}.{GetLocStem(component)}.description"),
                icon)
            {
                Id = $"reastralparty.final_arena_distortion_component.{component}",
                IsInstanced = true
            });
        }

        return tips;
    }

    public static bool ShouldKeepInitialPointCostMinimum(IRunState? runState)
    {
        return HasComponent(runState, FinalArenaDistortionComponent.Robbery);
    }

    public static bool TryGetRunDataSnapshot(
        RunState runState,
        out IReadOnlyList<FinalArenaDistortionComponent> components,
        out FinalArenaSeriesRunData runData)
    {
        runData = GetOrCreateRunData(runState);
        if (!TryDeserializeComponents(runData.ComponentsJson, out var parsed))
            parsed = [];

        components = parsed;
        return parsed.Count > 0;
    }

    public static void ApplySyncedComponents(
        RunState runState,
        IReadOnlyList<FinalArenaDistortionComponent> components,
        bool humanAiDuelApplied,
        bool robberyGoldGranted,
        bool robberyNextCombatPending,
        bool mysteriousTreasureInitialized,
        bool mysteriousTreasureNextCombatPending,
        int mysteriousTreasureCombatCounter,
        int arbitrageLastFloor,
        int globalPlayerTurnCounter)
    {
        var runData = GetOrCreateRunData(runState);
        runData.ComponentsJson = SerializeComponents(components);
        runData.HumanAiDuelApplied = humanAiDuelApplied;
        runData.RobberyGoldGranted = robberyGoldGranted;
        runData.RobberyNextCombatStunPending = robberyNextCombatPending;
        runData.MysteriousTreasureInitialized = mysteriousTreasureInitialized;
        runData.MysteriousTreasureNextCombatPending = mysteriousTreasureNextCombatPending;
        runData.MysteriousTreasureCombatCounter = mysteriousTreasureCombatCounter;
        runData.ArbitrageLastFloor = arbitrageLastFloor;
        runData.GlobalPlayerTurnCounter = globalPlayerTurnCounter;
        _runData.Set(runState, runData);
        FinalArenaDistortionComponentModifierInstaller.EnsureInstalled(runState, loadedRun: true);
    }

    public static int AdvanceGlobalPlayerTurnCounter(RunState runState)
    {
        var runData = GetOrCreateRunData(runState);
        runData.GlobalPlayerTurnCounter = Math.Max(0, runData.GlobalPlayerTurnCounter) + 1;
        _runData.Set(runState, runData);
        return runData.GlobalPlayerTurnCounter;
    }

    public static bool ConsumeRobberyNextCombatStun(RunState runState)
    {
        var runData = GetOrCreateRunData(runState);
        if (!runData.RobberyNextCombatStunPending)
            return false;

        runData.RobberyNextCombatStunPending = false;
        _runData.Set(runState, runData);
        return true;
    }

    public static bool ShouldApplyMysteriousTreasureThisCombat(RunState runState)
    {
        if (!HasComponent(runState, FinalArenaDistortionComponent.MysteriousTreasure))
            return false;

        var runData = GetOrCreateRunData(runState);
        if (runData.MysteriousTreasureNextCombatPending)
        {
            runData.MysteriousTreasureNextCombatPending = false;
            runData.MysteriousTreasureCombatCounter = 1;
            _runData.Set(runState, runData);
            return true;
        }

        runData.MysteriousTreasureCombatCounter++;
        var shouldApply = runData.MysteriousTreasureCombatCounter >= 5;
        if (shouldApply)
            runData.MysteriousTreasureCombatCounter = 0;
        _runData.Set(runState, runData);
        return shouldApply;
    }

    public static int GetAndUpdateArbitrageFloorBaseline(RunState runState)
    {
        var runData = GetOrCreateRunData(runState);
        if (runData.ArbitrageLastFloor < 0)
            runData.ArbitrageLastFloor = Math.Max(0, runState.TotalFloor);
        _runData.Set(runState, runData);
        return runData.ArbitrageLastFloor;
    }

    public static void SetArbitrageFloorBaseline(RunState runState, int floor)
    {
        var runData = GetOrCreateRunData(runState);
        runData.ArbitrageLastFloor = floor;
        _runData.Set(runState, runData);
    }

    internal static int GetDifficulty(FinalArenaDistortionComponent component)
    {
        return component switch
        {
            FinalArenaDistortionComponent.HumanAiDuel => 0,
            FinalArenaDistortionComponent.RefereeAttentionOne => -1,
            FinalArenaDistortionComponent.RefereeAttentionTwo => -2,
            FinalArenaDistortionComponent.PirateKingsOrder => -2,
            FinalArenaDistortionComponent.Thundercloud => -2,
            FinalArenaDistortionComponent.Rainstorm => -1,
            FinalArenaDistortionComponent.Seasickness => -1,
            FinalArenaDistortionComponent.EnergyDisorder => 0,
            FinalArenaDistortionComponent.CopyCard => 0,
            FinalArenaDistortionComponent.WideAreaMark => 0,
            FinalArenaDistortionComponent.Robbery => 1,
            FinalArenaDistortionComponent.Gale => 1,
            FinalArenaDistortionComponent.Dexterity => 1,
            FinalArenaDistortionComponent.Overload => 0,
            FinalArenaDistortionComponent.Arbitrage => 1,
            FinalArenaDistortionComponent.PreciseSelection => 1,
            FinalArenaDistortionComponent.FlashOfInsight => 2,
            FinalArenaDistortionComponent.MysteriousTreasure => 2,
            FinalArenaDistortionComponent.UnexpectedWealth => 2,
            FinalArenaDistortionComponent.Baton => 2,
            _ => 0
        };
    }

    private static async Task ApplyOneTimeComponents(
        RunState runState,
        FinalArenaSeriesRunData runData,
        IReadOnlyList<FinalArenaDistortionComponent> components)
    {
        if (components.Contains(FinalArenaDistortionComponent.HumanAiDuel) && !runData.HumanAiDuelApplied)
        {
            foreach (var player in runState.Players.OrderBy(static player => player.NetId))
                await GrantHumanAiDuelStunGun(player);

            runData.HumanAiDuelApplied = true;
        }

        if (components.Contains(FinalArenaDistortionComponent.Robbery) && !runData.RobberyGoldGranted)
        {
            foreach (var player in runState.Players.OrderBy(static player => player.NetId))
                await PlayerCmd.GainGold(60m, player);

            runData.RobberyGoldGranted = true;
            runData.RobberyNextCombatStunPending = true;
        }

        if (components.Contains(FinalArenaDistortionComponent.MysteriousTreasure) &&
            !runData.MysteriousTreasureInitialized)
        {
            runData.MysteriousTreasureInitialized = true;
            runData.MysteriousTreasureNextCombatPending = true;
            runData.MysteriousTreasureCombatCounter = 0;
        }

        _runData.Set(runState, runData);
    }

    private static async Task GrantHumanAiDuelStunGun(Player player)
    {
        if (player == null)
            return;

        var obtainedNow = false;
        var stunGun = player.GetRelic<TokenExclusiveStunGun>();
        if (stunGun == null)
        {
            var canonical = ModelDb.Relic<TokenExclusiveStunGun>().CanonicalInstance
                            ?? ModelDb.Relic<TokenExclusiveStunGun>();
            stunGun = await RelicCmd.Obtain(canonical.ToMutable(), player) as TokenExclusiveStunGun;
            if (stunGun == null)
            {
                MainFile.Logger.Warn("[FinalArena] HumanAiDuel failed to obtain TokenExclusiveStunGun during startup bootstrap.");
                return;
            }

            obtainedNow = true;
        }

        if (obtainedNow)
            stunGun.AstralParty_TokenExclusiveStunGunMindStacks = StunGunInitialMind;
        else
            stunGun.EnsureMinimumMindStacks(StunGunInitialMind);

        stunGun.NotifyDisplayAmountChanged();

        if (player.Creature?.CombatState != null)
            await PowerCmd.Apply<MindPower>(player.Creature, StunGunInitialMind, player.Creature, null, false);
    }

    private static bool IsFinalArenaOpen(IRunState? runState)
    {
        return TokenSeriesAvailabilityHelper.TryGetState(runState, out var state) &&
               state.OpenSeries.Contains(TokenSeriesAvailabilityHelper.TokenSeries.FinalArena);
    }

    private static IReadOnlyList<FinalArenaDistortionComponent> GenerateComponents(RunState runState)
    {
        var seed = runState.Rng.StringSeed;
        var attentionIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            RequiredAttentionComponents.Length,
            MainFile.ModId,
            DistortionComponentsKey,
            seed,
            "attention");
        var attention = RequiredAttentionComponents[attentionIndex];

        var validCombinations = BuildValidOptionalCombinations(attention)
            .OrderBy(combo => DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                int.MaxValue,
                MainFile.ModId,
                DistortionComponentsKey,
                seed,
                "optional",
                string.Join(",", combo.Select(static item => item.ToString()))))
            .ThenBy(combo => string.Join(",", combo.Select(static item => item.ToString())), StringComparer.Ordinal)
            .ToList();

        if (validCombinations.Count == 0)
            throw new InvalidOperationException("No valid Final Arena distortion component combination exists.");

        return new[]
            {
                FinalArenaDistortionComponent.HumanAiDuel,
                attention
            }
            .Concat(validCombinations[0])
            .ToArray();
    }

    private static IEnumerable<IReadOnlyList<FinalArenaDistortionComponent>> BuildValidOptionalCombinations(
        FinalArenaDistortionComponent attention)
    {
        for (var i = 0; i < OptionalComponents.Length - 2; i++)
        for (var j = i + 1; j < OptionalComponents.Length - 1; j++)
        for (var k = j + 1; k < OptionalComponents.Length; k++)
        {
            var combo = new[]
            {
                OptionalComponents[i],
                OptionalComponents[j],
                OptionalComponents[k]
            };
            var totalDifficulty = GetDifficulty(FinalArenaDistortionComponent.HumanAiDuel)
                                  + GetDifficulty(attention)
                                  + combo.Sum(GetDifficulty);
            if (totalDifficulty is 0 or -1)
                yield return combo;
        }
    }

    private static FinalArenaSeriesRunData GetOrCreateRunData(RunState runState)
    {
        if (!_registered)
            Register();

        if (_runData.TryGet(runState, out var runData) && runData != null)
            return runData;

        runData = new FinalArenaSeriesRunData();
        _runData.Set(runState, runData);
        return runData;
    }

    private static string SerializeComponents(IReadOnlyList<FinalArenaDistortionComponent> components)
    {
        return JsonSerializer.Serialize(components.Select(static component => component.ToString()).ToArray());
    }

    private static bool TryDeserializeComponents(
        string? json,
        out IReadOnlyList<FinalArenaDistortionComponent> components)
    {
        components = [];
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            components = (JsonSerializer.Deserialize<string[]>(json) ?? [])
                .Select(static value => Enum.TryParse<FinalArenaDistortionComponent>(value, out var component)
                    ? component
                    : (FinalArenaDistortionComponent?)null)
                .Where(static component => component.HasValue)
                .Select(static component => component!.Value)
                .Distinct()
                .ToArray();
            return components.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    internal static string GetLocStem(FinalArenaDistortionComponent component)
    {
        return component switch
        {
            FinalArenaDistortionComponent.HumanAiDuel => "human_ai_duel",
            FinalArenaDistortionComponent.RefereeAttentionOne => "referee_attention_1",
            FinalArenaDistortionComponent.RefereeAttentionTwo => "referee_attention_2",
            FinalArenaDistortionComponent.PirateKingsOrder => "pirate_kings_order",
            FinalArenaDistortionComponent.Thundercloud => "thundercloud",
            FinalArenaDistortionComponent.Rainstorm => "rainstorm",
            FinalArenaDistortionComponent.Seasickness => "seasickness",
            FinalArenaDistortionComponent.EnergyDisorder => "energy_disorder",
            FinalArenaDistortionComponent.CopyCard => "copy_card",
            FinalArenaDistortionComponent.WideAreaMark => "wide_area_mark",
            FinalArenaDistortionComponent.Robbery => "robbery",
            FinalArenaDistortionComponent.Gale => "gale",
            FinalArenaDistortionComponent.Dexterity => "dexterity",
            FinalArenaDistortionComponent.Overload => "overload",
            FinalArenaDistortionComponent.Arbitrage => "arbitrage",
            FinalArenaDistortionComponent.PreciseSelection => "precise_selection",
            FinalArenaDistortionComponent.FlashOfInsight => "flash_of_insight",
            FinalArenaDistortionComponent.MysteriousTreasure => "mysterious_treasure",
            FinalArenaDistortionComponent.UnexpectedWealth => "unexpected_wealth",
            FinalArenaDistortionComponent.Baton => "baton",
            _ => component.ToString()
        };
    }
}

public sealed class FinalArenaSeriesRunData
{
    public string ComponentsJson { get; set; } = string.Empty;
    public bool HumanAiDuelApplied { get; set; }
    public bool RobberyGoldGranted { get; set; }
    public bool RobberyNextCombatStunPending { get; set; }
    public bool MysteriousTreasureInitialized { get; set; }
    public bool MysteriousTreasureNextCombatPending { get; set; }
    public int MysteriousTreasureCombatCounter { get; set; }
    public int ArbitrageLastFloor { get; set; } = -1;
    public int GlobalPlayerTurnCounter { get; set; }
}
