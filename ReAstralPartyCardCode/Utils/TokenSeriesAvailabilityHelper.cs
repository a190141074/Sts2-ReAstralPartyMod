using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class TokenSeriesAvailabilityHelper
{
    public const string RunSelectionKey = "ReAstralPartyMod.OpenTokenSeries";

    public enum TokenSeries
    {
        Dreamship,
        SpiritFestival,
        WaterTown,
        MagicAcademy,
        DragonPalace,
        GhostAlley
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
        [TokenSeries.GhostAlley] = "ASTRAL_GHOST_ALLEY_SET"
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
        [ModelDb.GetId<TokenExclusiveTimer>()] = "【计时器】"
    };

    public static bool TryGetState(IRunState? runState, out TokenSeriesAvailabilityState state)
    {
        state = default;
        if (runState == null)
            return false;

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

        state = new TokenSeriesAvailabilityState(
            [AllSeries[rolls[0]], AllSeries[rolls[1]]]).Normalize();
        return true;
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
        return RelicIdsBySeries.TryGetValue(series, out var relicIds)
            ? relicIds.Select(ModelDb.GetById<RelicModel>).ToList()
            : [];
    }

    public static IReadOnlyList<IHoverTip> BuildHoverTips(IRunState? runState, Texture2D? icon)
    {
        if (!TryGetState(runState, out var state))
        {
            MainFile.Logger.Warn($"Open token series hover tips fell back to uninitialized text. {BuildDebugSummary(runState)}");
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
        return STS2RitsuLib.Content.ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, SeriesKeywordStemBySeries[series]);
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
            _ => series.ToString()
        };
    }

    private static IReadOnlyList<string> GetSeriesRelicTitles(TokenSeries series)
    {
        return GetSeriesRelics(series)
            .Select(ResolveRelicTitle)
            .ToList();
    }

    private static HoverTip BuildSeriesHoverTip(TokenSeries series, int extensionIndex, Texture2D? icon)
    {
        var titleKey = extensionIndex == 1
            ? "RE_ASTRAL_PARTY_MOD_TOPBAR_OPEN_TOKEN_SERIES.extension1_title"
            : "RE_ASTRAL_PARTY_MOD_TOPBAR_OPEN_TOKEN_SERIES.extension2_title";
        var seriesTitle = ResolveSeriesTitle(series);
        var title = LocString.GetIfExists("relics", titleKey) ?? new LocString("relics", "RE_ASTRAL_PARTY_MOD_TOPBAR_OPEN_TOKEN_SERIES.title");
        title.Add("SeriesName", seriesTitle);

        var descriptionLines = GetSeriesRelicTitles(series).Select(relicTitle => $"-{relicTitle}");
        var description = LocString.Exists("relics", titleKey)
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

    public readonly record struct TokenSeriesAvailabilityState(IReadOnlyList<TokenSeries> OpenSeries)
    {
        public TokenSeriesAvailabilityState Normalize()
        {
            return new TokenSeriesAvailabilityState(OpenSeries
                .Distinct()
                .OrderBy(series => (int)series)
                .Take(2)
                .ToArray());
        }
    }
}
