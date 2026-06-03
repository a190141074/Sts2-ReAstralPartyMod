using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Combat.Rewards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Rewards;

public enum EnigmaticUniqueMaterialKind
{
    EtheriumIngot = 0,
    NefariousEssence = 1,
    NetheriteIngot = 2,
    RedstoneDust = 3,
    GhastTear = 4,
    BlazePowder = 5,
    EnderEye = 6,
    EarthHeart = 7,
    TwistedHeart = 8
}

internal sealed record EnigmaticUniqueMaterialConfig(
    EnigmaticUniqueMaterialKind Kind,
    string RelicIdEntry,
    Func<RelicModel> RelicResolver,
    Func<Player, int, Task<EnigmaticUniqueMaterialRelicBase?>> GrantStacksAsync,
    RelicRarity Rarity,
    int MinRewardAmount,
    int MaxRewardAmount,
    int RewardPoolWeight,
    bool IncludeInSevenBlessingsPool)
{
    public RelicModel Relic => RelicResolver();

    public string RewardKey(int amount)
    {
        return $"{RelicIdEntry}:{amount}";
    }

    public string RewardTypeEntry(int amount)
    {
        return $"{RelicIdEntry}_{amount}";
    }
}

internal static class EnigmaticRewardRegistry
{
    private static bool _registered;
    private static readonly IReadOnlyDictionary<EnigmaticUniqueMaterialKind, EnigmaticUniqueMaterialConfig> MaterialConfigs =
        new Dictionary<EnigmaticUniqueMaterialKind, EnigmaticUniqueMaterialConfig>
        {
            [EnigmaticUniqueMaterialKind.EtheriumIngot] = new(
                EnigmaticUniqueMaterialKind.EtheriumIngot,
                "enigmatic_etherium_ingot",
                static () => ModelDb.Relic<EnigmaticEtheriumIngot>(),
                GrantMaterialStacks<EnigmaticEtheriumIngot>,
                RelicRarity.Common,
                0,
                3,
                1,
                true),
            [EnigmaticUniqueMaterialKind.NefariousEssence] = new(
                EnigmaticUniqueMaterialKind.NefariousEssence,
                "enigmatic_nefarious_essence",
                static () => ModelDb.Relic<EnigmaticNefariousEssence>(),
                GrantMaterialStacks<EnigmaticNefariousEssence>,
                RelicRarity.Common,
                0,
                4,
                1,
                true),
            [EnigmaticUniqueMaterialKind.NetheriteIngot] = new(
                EnigmaticUniqueMaterialKind.NetheriteIngot,
                "enigmatic_netherite_ingot",
                static () => ModelDb.Relic<EnigmaticNetheriteIngot>(),
                GrantMaterialStacks<EnigmaticNetheriteIngot>,
                RelicRarity.Rare,
                0,
                2,
                1,
                true),
            [EnigmaticUniqueMaterialKind.RedstoneDust] = new(
                EnigmaticUniqueMaterialKind.RedstoneDust,
                "enigmatic_redstone_dust",
                static () => ModelDb.Relic<EnigmaticRedstoneDust>(),
                GrantMaterialStacks<EnigmaticRedstoneDust>,
                RelicRarity.Common,
                1,
                4,
                1,
                true),
            [EnigmaticUniqueMaterialKind.GhastTear] = new(
                EnigmaticUniqueMaterialKind.GhastTear,
                "enigmatic_ghast_tear",
                static () => ModelDb.Relic<EnigmaticGhastTear>(),
                GrantMaterialStacks<EnigmaticGhastTear>,
                RelicRarity.Rare,
                1,
                2,
                1,
                true),
            [EnigmaticUniqueMaterialKind.BlazePowder] = new(
                EnigmaticUniqueMaterialKind.BlazePowder,
                "enigmatic_blaze_powder",
                static () => ModelDb.Relic<EnigmaticBlazePowder>(),
                GrantMaterialStacks<EnigmaticBlazePowder>,
                RelicRarity.Common,
                1,
                4,
                1,
                true),
            [EnigmaticUniqueMaterialKind.EnderEye] = new(
                EnigmaticUniqueMaterialKind.EnderEye,
                "enigmatic_ender_eye",
                static () => ModelDb.Relic<EnigmaticEnderEye>(),
                GrantMaterialStacks<EnigmaticEnderEye>,
                RelicRarity.Rare,
                1,
                2,
                1,
                true),
            [EnigmaticUniqueMaterialKind.EarthHeart] = new(
                EnigmaticUniqueMaterialKind.EarthHeart,
                "enigmatic_earth_heart",
                static () => ModelDb.Relic<EnigmaticEarthHeart>(),
                GrantMaterialStacks<EnigmaticEarthHeart>,
                RelicRarity.Rare,
                1,
                1,
                1,
                true),
            [EnigmaticUniqueMaterialKind.TwistedHeart] = new(
                EnigmaticUniqueMaterialKind.TwistedHeart,
                "enigmatic_synthesis_twisted_heart",
                static () => ModelDb.Relic<EnigmaticSynthesisTwistedHeart>(),
                GrantMaterialStacks<EnigmaticSynthesisTwistedHeart>,
                RelicRarity.Rare,
                1,
                1,
                0,
                false)
        };
    private static readonly Dictionary<(EnigmaticUniqueMaterialKind Kind, int Amount), RewardType> RewardTypes = [];

    public static void RegisterAll()
    {
        if (_registered)
            return;

        var registry = ModRewardRegistry.For(MainFile.ModId);
        foreach (var config in MaterialConfigs.Values.Where(static config => config.IncludeInSevenBlessingsPool))
        {
            for (var amount = config.MinRewardAmount; amount <= config.MaxRewardAmount; amount++)
            {
                var capturedKind = config.Kind;
                var capturedAmount = amount;
                RewardTypes[(capturedKind, capturedAmount)] = registry
                    .RegisterOwned(
                        config.RewardTypeEntry(capturedAmount),
                        (_, player, _) => new EnigmaticUniqueMaterialReward(player, capturedKind, capturedAmount))
                    .RewardType;
            }
        }

        _registered = true;
    }

    public static EnigmaticUniqueMaterialConfig GetConfig(EnigmaticUniqueMaterialKind kind)
    {
        return MaterialConfigs[kind];
    }

    public static EnigmaticUniqueMaterialKind RollUniqueMaterialKind(params object?[] contextParts)
    {
        var availableKinds = MaterialConfigs.Values
            .Where(static config => config.IncludeInSevenBlessingsPool && config.RewardPoolWeight > 0)
            .OrderBy(config => config.RelicIdEntry, StringComparer.Ordinal)
            .ToList();
        if (availableKinds.Count == 0)
            return EnigmaticUniqueMaterialKind.EtheriumIngot;

        var totalWeight = availableKinds.Sum(static config => config.RewardPoolWeight);
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            totalWeight,
            contextParts);
        foreach (var config in availableKinds)
        {
            if (roll < config.RewardPoolWeight)
                return config.Kind;

            roll -= config.RewardPoolWeight;
        }

        return availableKinds[^1].Kind;
    }

    public static int RollRewardAmount(EnigmaticUniqueMaterialKind kind, params object?[] contextParts)
    {
        var config = GetConfig(kind);
        return DeterministicMultiplayerChoiceHelper.RollDeterministically(
            config.MinRewardAmount,
            config.MaxRewardAmount + 1,
            contextParts);
    }

    public static RewardType GetRewardType(EnigmaticUniqueMaterialKind kind, int amount)
    {
        var clampedAmount = ClampRewardAmount(kind, amount);
        if (RewardTypes.TryGetValue((kind, clampedAmount), out var rewardType))
            return rewardType;

        throw new ArgumentOutOfRangeException(nameof(amount), amount, $"No reward type registered for {kind} amount {clampedAmount}.");
    }

    public static string CreateRewardKey(EnigmaticUniqueMaterialKind kind, int amount)
    {
        var clampedAmount = ClampRewardAmount(kind, amount);
        return GetConfig(kind).RewardKey(clampedAmount);
    }

    public static bool TryParseRewardKey(
        string key,
        out EnigmaticUniqueMaterialKind kind,
        out int amount)
    {
        kind = EnigmaticUniqueMaterialKind.EtheriumIngot;
        amount = 0;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        var parts = key.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out amount))
            return false;

        foreach (var config in MaterialConfigs.Values)
        {
            if (!string.Equals(config.RelicIdEntry, parts[0], StringComparison.Ordinal))
                continue;

            kind = config.Kind;
            amount = ClampRewardAmount(kind, amount);
            return true;
        }

        return false;
    }

    public static Reward CreateUniqueMaterialReward(Player player, EnigmaticUniqueMaterialKind kind, int amount)
    {
        return new EnigmaticUniqueMaterialReward(player, kind, amount);
    }

    private static int ClampRewardAmount(EnigmaticUniqueMaterialKind kind, int amount)
    {
        var config = GetConfig(kind);
        return Math.Clamp(amount, config.MinRewardAmount, config.MaxRewardAmount);
    }

    private static async Task<EnigmaticUniqueMaterialRelicBase?> GrantMaterialStacks<T>(Player owner, int amount)
        where T : EnigmaticUniqueMaterialRelicBase
    {
        return await EnigmaticUniqueMaterialRelicBase.GrantStacks<T>(owner, amount);
    }
}

public sealed class EnigmaticUniqueMaterialReward : ModCustomReward
{
    private readonly EnigmaticUniqueMaterialConfig _config;

    public EnigmaticUniqueMaterialKind Kind { get; }
    public int Amount { get; }

    public EnigmaticUniqueMaterialReward(Player player, EnigmaticUniqueMaterialKind kind, int amount) : base(player)
    {
        Kind = kind;
        _config = EnigmaticRewardRegistry.GetConfig(kind);
        Amount = Math.Clamp(amount, _config.MinRewardAmount, _config.MaxRewardAmount);
    }

    public override LocString Description => new(
        "relics",
        $"RE_ASTRAL_PARTY_MOD_REWARD_{_config.RelicIdEntry.ToUpperInvariant()}_{Amount}.description");

    protected override string RewardIconPath => ((AstralPartyRelicModel)_config.Relic).PublicBigIconPath;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralUniqueMaterialId),
        new HoverTip(_config.Relic.Title, _config.Relic.DynamicDescription.GetRawText(), _config.Relic.Icon)
    ];

    public override RewardType ModRewardType => EnigmaticRewardRegistry.GetRewardType(Kind, Amount);

    public override void MarkContentAsSeen()
    {
        SaveManager.Instance?.MarkRelicAsSeen(_config.Relic);
    }

    protected override async Task<bool> OnSelect()
    {
        if (Player == null)
            return false;

        await _config.GrantStacksAsync(Player, Amount);
        return true;
    }
}
