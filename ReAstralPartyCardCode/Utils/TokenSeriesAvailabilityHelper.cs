using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class TokenSeriesAvailabilityHelper
{
    public const string RunSelectionKey = "ReAstralPartyMod.OpenTokenSeries";
    private const string ManualOverrideKey = "manual_open_token_series";

    private static RunSavedData<TokenSeriesManualOverrideRunData> _manualOverrideRunData = null!;
    private static bool _registered;

    public enum TokenSeries
    {
        Dreamship,
        SpiritFestival,
        WaterTown,
        MagicAcademy,
        DragonPalace,
        GhostAlley,
        FinalArena
    }

    private static readonly TokenSeries[] AllSeries =
    [
        TokenSeries.Dreamship,
        TokenSeries.SpiritFestival,
        TokenSeries.WaterTown,
        TokenSeries.MagicAcademy,
        TokenSeries.DragonPalace,
        TokenSeries.GhostAlley
    ];

    private static readonly Dictionary<TokenSeries, string> SeriesKeywordStemBySeries = new()
    {
        [TokenSeries.Dreamship] = "ASTRAL_DREAMSHIP_SERIES",
        [TokenSeries.SpiritFestival] = "ASTRAL_SPIRIT_FESTIVAL_SERIES",
        [TokenSeries.WaterTown] = "ASTRAL_WATER_TOWN_SERIES",
        [TokenSeries.MagicAcademy] = "ASTRAL_MAGIC_ACADEMY_SERIES",
        [TokenSeries.DragonPalace] = "ASTRAL_DRAGON_PALACE_SERIES",
        [TokenSeries.GhostAlley] = "ASTRAL_GHOST_ALLEY_SET",
        [TokenSeries.FinalArena] = "ASTRAL_FINAL_ARENA_SERIES"
    };

    private static readonly Dictionary<TokenSeries, IReadOnlyList<ModelId>> RelicIdsBySeries = new()
    {
        [TokenSeries.Dreamship] =
        [
            ModelDb.GetId<TokenBlueGiantAnchor>(),
            ModelDb.GetId<TokenExclusiveDreamshipModel>(),
            ModelDb.GetId<TokenExclusiveTrident>()
        ],
        [TokenSeries.SpiritFestival] =
        [
            ModelDb.GetId<TokenExclusiveCursedSword>(),
            ModelDb.GetId<TokenExclusivePiercingGun>(),
            ModelDb.GetId<TokenExclusiveVengeanceHalberd>()
        ],
        [TokenSeries.WaterTown] =
        [
            ModelDb.GetId<TokenExclusiveBronzeGong>()
        ],
        [TokenSeries.MagicAcademy] =
        [
            ModelDb.GetId<TokenExclusiveAncientWand>(),
            ModelDb.GetId<TokenExclusiveBoutiqueSwordShield>(),
            ModelDb.GetId<TokenExclusiveZuoTeaCake>()
        ],
        [TokenSeries.DragonPalace] =
        [
            ModelDb.GetId<TokenExclusiveCrossedTwinCarp>(),
            ModelDb.GetId<TokenExclusiveInfiniteSnake>(),
            ModelDb.GetId<TokenExclusiveLittleCarpDoll>(),
            ModelDb.GetId<TokenExclusiveLittleSnakeDoll>(),
            ModelDb.GetId<TokenExclusivePsychedelicSeafoodSoup>(),
            ModelDb.GetId<TokenExclusiveStormTalisman>()
        ],
        [TokenSeries.GhostAlley] =
        [
            ModelDb.GetId<TokenExclusiveCandyMembershipCard>(),
            ModelDb.GetId<TokenExclusiveTimer>()
        ],
        [TokenSeries.FinalArena] =
        [
            ModelDb.GetId<TokenExclusiveFitnessTracker>(),
            ModelDb.GetId<TokenExclusiveStunGun>(),
            ModelDb.GetId<TokenExclusiveTimePocketWatch>()
        ]
    };

    private static readonly Dictionary<ModelId, TokenSeries> RelicSeriesByRelicId = RelicIdsBySeries
        .SelectMany(pair => pair.Value.Select(id => new KeyValuePair<ModelId, TokenSeries>(id, pair.Key)))
        .ToDictionary(pair => pair.Key, pair => pair.Value);

    private static readonly Dictionary<ModelId, string> FallbackRelicTitles = new()
    {
        [ModelDb.GetId<TokenBlueGiantAnchor>()] = "【大铁锚】",
        [ModelDb.GetId<TokenExclusiveDreamshipModel>()] = "【梦想号模型】",
        [ModelDb.GetId<TokenExclusiveTrident>()] = "【三叉戟】",
        [ModelDb.GetId<TokenExclusiveCursedSword>()] = "【诅咒之剑】",
        [ModelDb.GetId<TokenExclusivePiercingGun>()] = "【贯穿之铳】",
        [ModelDb.GetId<TokenExclusiveVengeanceHalberd>()] = "【复仇之戟】",
        [ModelDb.GetId<TokenExclusiveBronzeGong>()] = "【大铜锣】",
        [ModelDb.GetId<TokenExclusiveAncientWand>()] = "【古老法杖】",
        [ModelDb.GetId<TokenExclusiveBoutiqueSwordShield>()] = "【精品剑盾】",
        [ModelDb.GetId<TokenExclusiveZuoTeaCake>()] = "【佐茶蛋糕】",
        [ModelDb.GetId<TokenExclusiveCrossedTwinCarp>()] = "【交错双鲤】",
        [ModelDb.GetId<TokenExclusiveInfiniteSnake>()] = "【无限之蛇】",
        [ModelDb.GetId<TokenExclusiveLittleCarpDoll>()] = "【小鲤鱼玩偶】",
        [ModelDb.GetId<TokenExclusiveLittleSnakeDoll>()] = "【小蛇玩偶】",
        [ModelDb.GetId<TokenExclusivePsychedelicSeafoodSoup>()] = "【迷幻海鲜汤】",
        [ModelDb.GetId<TokenExclusiveStormTalisman>()] = "【惊涛御守】",
        [ModelDb.GetId<TokenExclusiveCandyMembershipCard>()] = "【糖果会员卡】",
        [ModelDb.GetId<TokenExclusiveTimer>()] = "【计时器】",
        [ModelDb.GetId<TokenExclusiveFitnessTracker>()] = "【运动手环】",
        [ModelDb.GetId<TokenExclusiveStunGun>()] = "【电击枪】",
        [ModelDb.GetId<TokenExclusiveTimePocketWatch>()] = "【时间怀表】"
    };

    private static readonly Dictionary<string, TokenSeries> SeriesAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["dreamship"] = TokenSeries.Dreamship,
            ["dream_ship"] = TokenSeries.Dreamship,
            ["梦想号"] = TokenSeries.Dreamship,
            ["series_dreamship"] = TokenSeries.Dreamship,
            ["spiritfestival"] = TokenSeries.SpiritFestival,
            ["spirit_festival"] = TokenSeries.SpiritFestival,
            ["御魂庆典"] = TokenSeries.SpiritFestival,
            ["series_spirit_festival"] = TokenSeries.SpiritFestival,
            ["watertown"] = TokenSeries.WaterTown,
            ["water_town"] = TokenSeries.WaterTown,
            ["水乡古镇"] = TokenSeries.WaterTown,
            ["series_water_town"] = TokenSeries.WaterTown,
            ["magicacademy"] = TokenSeries.MagicAcademy,
            ["magic_academy"] = TokenSeries.MagicAcademy,
            ["魔法学院"] = TokenSeries.MagicAcademy,
            ["series_magic_academy"] = TokenSeries.MagicAcademy,
            ["dragonpalace"] = TokenSeries.DragonPalace,
            ["dragon_palace"] = TokenSeries.DragonPalace,
            ["龙宫游乐园"] = TokenSeries.DragonPalace,
            ["series_dragon_palace"] = TokenSeries.DragonPalace,
            ["ghostalley"] = TokenSeries.GhostAlley,
            ["ghost_alley"] = TokenSeries.GhostAlley,
            ["幽魂暗巷"] = TokenSeries.GhostAlley,
            ["series_ghost_alley"] = TokenSeries.GhostAlley,
            ["finalarena"] = TokenSeries.FinalArena,
            ["final_arena"] = TokenSeries.FinalArena,
            ["决赛大赛场"] = TokenSeries.FinalArena,
            ["series_final_arena"] = TokenSeries.FinalArena
        };

    public static void Register()
    {
        if (_registered)
            return;

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            _manualOverrideRunData = RitsuLibFramework
                .GetRunSavedDataStore(MainFile.ModId)
                .Register(
                    ManualOverrideKey,
                    static () => new TokenSeriesManualOverrideRunData(),
                    new RunSavedDataOptions
                    {
                        WritePolicy = RunSavedDataWritePolicy.WhenNonDefault
                    });
        }

        _registered = true;
    }

    public static bool TryGetState(IRunState? runState, out TokenSeriesAvailabilityState state)
    {
        state = default;
        if (runState == null)
            return false;

        if (ReAstralPartyModSettingsManager.GetEnableMechTideMode(runState))
        {
            state = new TokenSeriesAvailabilityState([TokenSeries.FinalArena]);
            return true;
        }

        if (TryGetManualOverrideState(runState, out state))
            return true;

        var tokenSeriesMode = ReAstralPartyModSettingsManager.GetTokenSeriesMode(runState);

        switch (tokenSeriesMode)
        {
            case TokenSeriesMode.All:
                state = new TokenSeriesAvailabilityState(AllSeries).Normalize();
                return true;
            case TokenSeriesMode.Disabled:
                state = new TokenSeriesAvailabilityState([]);
                return true;
        }

        var seed = runState.Rng.StringSeed;
        if (string.IsNullOrWhiteSpace(seed))
            return false;

        var rolls = DeterministicSelectionHelper.PickDistinctIndices(
            2,
            AllSeries.Length,
            MainFile.ModId,
            RunSelectionKey,
            seed);
        if (rolls.Count < 2)
            return false;

        var rolledSeries = new[] { AllSeries[rolls[0]], AllSeries[rolls[1]] };
        state = NormalizeSeries(rolledSeries);
        return true;
    }

    public static IReadOnlyList<TokenSeries> GetMutableOpenSeriesForCommand(RunState runState)
    {
        if (TryGetState(runState, out var state))
            return state.OpenSeries.ToArray();

        return [];
    }

    public static void SetManualOpenSeries(RunState runState, IEnumerable<TokenSeries> openSeries)
    {
        if (!_registered)
            Register();

        var state = NormalizeSeries(openSeries);
        _manualOverrideRunData.Set(runState, new TokenSeriesManualOverrideRunData
        {
            IsOverrideActive = true,
            OpenSeries = state.OpenSeries.Select(static series => series.ToString()).ToList()
        });
    }

    public static bool TryParseSeries(string? input, out TokenSeries series)
    {
        series = default;
        var normalized = NormalizeSeriesInput(input);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (SeriesAliases.TryGetValue(normalized, out series))
            return series != TokenSeries.FinalArena;

        if (!Enum.TryParse(normalized, true, out series) || !Enum.IsDefined(series))
            return false;

        return series != TokenSeries.FinalArena;
    }

    public static string BuildSeriesListSummary(IReadOnlyList<TokenSeries> openSeries)
    {
        if (openSeries.Count == 0)
            return "open_token_series=[]";

        var sections = openSeries
            .Select((series, index) => $"Extension{index + 1}={ResolveSeriesTitle(series)}")
            .ToArray();
        return $"open_token_series=[{string.Join(" | ", sections)}]";
    }

    public static IReadOnlyList<string> GetSeriesCompletionAliases()
    {
        return
        [
            "dreamship",
            "spirit_festival",
            "water_town",
            "magic_academy",
            "dragon_palace",
            "ghost_alley"
        ];
    }

    public static string BuildDebugSummary(IRunState? runState)
    {
        if (!TryGetState(runState, out var state))
            return "open_token_series=uninitialized";

        var sections = state.OpenSeries
            .Select((series, index) =>
                $"Extension{index + 1}={ResolveSeriesTitle(series)}[{string.Join(", ", GetSeriesRelicTitles(series))}]")
            .ToArray();
        var seed = runState?.Rng.StringSeed ?? "<null>";
        return $"open_token_series=[{string.Join(" | ", sections)}] seed={seed}";
    }

    private static bool TryGetManualOverrideState(IRunState runState, out TokenSeriesAvailabilityState state)
    {
        state = default;
        if (runState is not RunState concreteRunState)
            return false;

        if (!_registered)
            Register();

        if (!_manualOverrideRunData.TryGet(concreteRunState, out var runData) ||
            runData?.IsOverrideActive != true)
            return false;

        var openSeries = new List<TokenSeries>();
        foreach (var serializedSeries in runData.OpenSeries)
        {
            if (TryParseSeries(serializedSeries, out var series))
                openSeries.Add(series);
        }

        state = NormalizeSeries(openSeries);
        return true;
    }

    public static bool IsSeriesTokenRelic(RelicModel relic)
    {
        var relicId = relic.CanonicalInstance?.Id ?? relic.Id;
        return RelicSeriesByRelicId.ContainsKey(relicId);
    }

    public static bool TryGetSeriesForRelic(RelicModel relic, out TokenSeries series)
    {
        var relicId = relic.CanonicalInstance?.Id ?? relic.Id;
        return RelicSeriesByRelicId.TryGetValue(relicId, out series);
    }

    public static bool IsRelicAvailableForRun(IRunState? runState, RelicModel relic)
    {
        if (!IsSeriesTokenRelic(relic))
            return true;
        if (!TryGetState(runState, out var state))
            return false;
        if (!TryGetSeriesForRelic(relic, out var series))
            return false;

        return state.OpenSeries.Contains(series);
    }

    public static IReadOnlyList<RelicModel> FilterAvailableForRun(IRunState? runState, IEnumerable<RelicModel> relics)
    {
        return relics
            .Where(relic => IsRelicAvailableForRun(runState, relic))
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetSeriesRelics(TokenSeries series)
    {
        if (!RelicIdsBySeries.TryGetValue(series, out var relicIds))
            return [];

        var relics = new List<RelicModel>(relicIds.Count);
        foreach (var relicId in relicIds)
        {
            if (!TryResolveSeriesRelic(relicId, series, out var relic))
                continue;

            relics.Add(relic);
        }

        return relics;
    }

    public static IReadOnlyList<IHoverTip> BuildHoverTips(IRunState? runState, Texture2D? icon)
    {
        if (!TryGetState(runState, out var state))
        {
            MainFile.Logger.Warn(
                $"Open token series hover tips fell back to uninitialized text. {BuildDebugSummary(runState)}");
            return
            [
                new HoverTip(
                    new LocString("relics", "RE_ASTRAL_PARTY_MOD_TOPBAR_OPEN_TOKEN_SERIES.title"),
                    new LocString("relics", "RE_ASTRAL_PARTY_MOD_TOPBAR_OPEN_TOKEN_SERIES.description_uninitialized"),
                    icon)
            ];
        }

        return state.OpenSeries
            .Select((series, index) => BuildSeriesHoverTip(series, index + 1, icon))
            .Cast<IHoverTip>()
            .ToList();
    }

    private static string GetQualifiedKeywordId(TokenSeries series)
    {
        return STS2RitsuLib.Content.ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId,
            SeriesKeywordStemBySeries[series]);
    }

    private static TokenSeriesAvailabilityState NormalizeSeries(IEnumerable<TokenSeries> openSeries)
    {
        var distinct = openSeries
            .Where(static series => Enum.IsDefined(series))
            .Distinct()
            .ToArray();
        return new TokenSeriesAvailabilityState(
            distinct.Where(static series => series != TokenSeries.FinalArena).ToArray()).Normalize();
    }

    private static string NormalizeSeriesInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim()
            .Replace("【", string.Empty)
            .Replace("】", string.Empty)
            .Replace("系列·", string.Empty)
            .Replace("系列-", string.Empty)
            .Replace("系列_", string.Empty)
            .Replace("-", "_")
            .Replace(" ", "_");
        return normalized;
    }

    private static string ResolveSeriesTitle(TokenSeries series)
    {
        var key = $"{GetQualifiedKeywordId(series)}.title";
        var loc = LocString.GetIfExists("card_keywords", key) ?? new LocString("card_keywords", key);
        var text = loc.GetRawText();
        if (!string.IsNullOrWhiteSpace(text))
            return text;

        return series switch
        {
            TokenSeries.Dreamship => "【系列·梦想号】",
            TokenSeries.SpiritFestival => "【系列·御魂庆典】",
            TokenSeries.WaterTown => "【系列·水乡古镇】",
            TokenSeries.MagicAcademy => "【系列·魔法学院】",
            TokenSeries.DragonPalace => "【系列·龙宫游乐园】",
            TokenSeries.GhostAlley => "【系列·幽魂暗巷】",
            TokenSeries.FinalArena => "【系列·决赛大赛场】",
            _ => series.ToString()
        };
    }

    private static IReadOnlyList<string> GetSeriesRelicTitles(TokenSeries series)
    {
        if (!RelicIdsBySeries.TryGetValue(series, out var relicIds))
            return [];

        var titles = new List<string>(relicIds.Count);
        foreach (var relicId in relicIds)
        {
            if (TryResolveSeriesRelic(relicId, series, out var relic))
            {
                titles.Add(ResolveRelicTitle(relic));
                continue;
            }

            if (FallbackRelicTitles.TryGetValue(relicId, out var fallbackTitle))
                titles.Add(fallbackTitle);
        }

        return titles;
    }

    private static HoverTip BuildSeriesHoverTip(TokenSeries series, int extensionIndex, Texture2D? icon)
    {
        var titleKey = extensionIndex switch
        {
            1 => "RE_ASTRAL_PARTY_MOD_TOPBAR_OPEN_TOKEN_SERIES.extension1_title",
            2 => "RE_ASTRAL_PARTY_MOD_TOPBAR_OPEN_TOKEN_SERIES.extension2_title",
            _ => null
        };
        var seriesTitle = ResolveSeriesTitle(series);
        var title = !string.IsNullOrWhiteSpace(titleKey)
            ? LocString.GetIfExists("relics", titleKey)
              ?? new LocString("relics", "RE_ASTRAL_PARTY_MOD_TOPBAR_OPEN_TOKEN_SERIES.title")
            : new LocString("relics", "RE_ASTRAL_PARTY_MOD_TOPBAR_OPEN_TOKEN_SERIES.title");
        title.Add("SeriesName", seriesTitle);

        var descriptionLines = GetSeriesRelicTitles(series).Select(relicTitle => $"-{relicTitle}");
        var description = titleKey != null && LocString.Exists("relics", titleKey)
            ? string.Join("\n", descriptionLines)
            : $"【拓展{extensionIndex}：{seriesTitle}】\n{string.Join("\n", descriptionLines)}";
        return new HoverTip(title, description, icon)
        {
            Id = $"reastralparty.open_token_series.extension_{extensionIndex}_{series}",
            IsInstanced = true
        };
    }

    private static string ResolveRelicTitle(RelicModel relic)
    {
        var relicId = relic.CanonicalInstance?.Id ?? relic.Id;
        var locKey = $"{relicId.Entry}.title";
        var loc = LocString.GetIfExists("relics", locKey);
        if (loc != null)
        {
            var text = loc.GetRawText();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return FallbackRelicTitles.TryGetValue(relicId, out var fallback)
            ? fallback
            : relicId.Entry;
    }

    private static bool TryResolveSeriesRelic(ModelId relicId, TokenSeries series, out RelicModel relic)
    {
        try
        {
            relic = ModelDb.GetById<RelicModel>(relicId);
            return true;
        }
        catch (Exception ex)
        {
            relic = null!;
            MainFile.Logger.Warn(
                $"Open token series skipped missing relic model | series={series} relicId={relicId.Entry} error={ex.Message}");
            return false;
        }
    }

    public readonly record struct TokenSeriesAvailabilityState(IReadOnlyList<TokenSeries> OpenSeries)
    {
        public TokenSeriesAvailabilityState Normalize()
        {
            return new TokenSeriesAvailabilityState(OpenSeries
                .Distinct()
                .OrderBy(series => (int)series)
                .ToArray());
        }
    }
}

internal sealed class TokenSeriesManualOverrideRunData
{
    public bool IsOverrideActive { get; set; }

    public List<string> OpenSeries { get; set; } = [];
}
