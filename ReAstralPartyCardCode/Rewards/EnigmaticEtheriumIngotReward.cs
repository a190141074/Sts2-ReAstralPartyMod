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
    TwistedHeart = 8,
    PhantomMembrane = 9,
    DarkestScroll = 10,
    EnchantedBook = 11,
    Dye = 12,
    EnchantedFeather = 13,
    GemRing = 14,
    GoldIngot = 15,
    TriccScroll = 16,
    ExperienceBottle = 17,
    Emerald = 18,
    AwkwardPotion = 19,
    RecallPotion = 20,
    NetherStar = 21,
    LapisLazuli = 22,
    CryingObsidian = 23,
    EnchanterPearl = 24,
    AbyssalHeart = 25,
    AstralDust = 26,
    HeartOfTheSea = 27,
    CosmicHeart = 28,
    TheTwist = 29
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
                2,
                6,
                1,
                true),
            [EnigmaticUniqueMaterialKind.NefariousEssence] = new(
                EnigmaticUniqueMaterialKind.NefariousEssence,
                "enigmatic_nefarious_essence",
                static () => ModelDb.Relic<EnigmaticNefariousEssence>(),
                GrantMaterialStacks<EnigmaticNefariousEssence>,
                RelicRarity.Common,
                1,
                4,
                1,
                true),
            [EnigmaticUniqueMaterialKind.NetheriteIngot] = new(
                EnigmaticUniqueMaterialKind.NetheriteIngot,
                "enigmatic_netherite_ingot",
                static () => ModelDb.Relic<EnigmaticNetheriteIngot>(),
                GrantMaterialStacks<EnigmaticNetheriteIngot>,
                RelicRarity.Rare,
                1,
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
                false),
            [EnigmaticUniqueMaterialKind.PhantomMembrane] = new(
                EnigmaticUniqueMaterialKind.PhantomMembrane,
                "enigmatic_phantom_membrane",
                static () => ModelDb.Relic<EnigmaticPhantomMembrane>(),
                GrantMaterialStacks<EnigmaticPhantomMembrane>,
                RelicRarity.Rare,
                1,
                3,
                1,
                true),
            [EnigmaticUniqueMaterialKind.DarkestScroll] = new(
                EnigmaticUniqueMaterialKind.DarkestScroll,
                "enigmatic_darkest_scroll",
                static () => ModelDb.Relic<EnigmaticDarkestScroll>(),
                GrantMaterialStacks<EnigmaticDarkestScroll>,
                RelicRarity.Rare,
                1,
                1,
                1,
                true),
            [EnigmaticUniqueMaterialKind.EnchantedBook] = new(
                EnigmaticUniqueMaterialKind.EnchantedBook,
                "enigmatic_enchanted_book",
                static () => ModelDb.Relic<EnigmaticEnchantedBook>(),
                GrantMaterialStacks<EnigmaticEnchantedBook>,
                RelicRarity.Rare,
                1,
                1,
                1,
                true),
            [EnigmaticUniqueMaterialKind.Dye] = new(
                EnigmaticUniqueMaterialKind.Dye,
                "enigmatic_dye",
                static () => ModelDb.Relic<EnigmaticDye>(),
                GrantMaterialStacks<EnigmaticDye>,
                RelicRarity.Common,
                1,
                4,
                1,
                true),
            [EnigmaticUniqueMaterialKind.EnchantedFeather] = new(
                EnigmaticUniqueMaterialKind.EnchantedFeather,
                "enigmatic_enchanted_feather",
                static () => ModelDb.Relic<EnigmaticEnchantedFeather>(),
                GrantMaterialStacks<EnigmaticEnchantedFeather>,
                RelicRarity.Common,
                1,
                4,
                1,
                true),
            [EnigmaticUniqueMaterialKind.GemRing] = new(
                EnigmaticUniqueMaterialKind.GemRing,
                "enigmatic_gem_ring",
                static () => ModelDb.Relic<EnigmaticGemRing>(),
                GrantMaterialStacks<EnigmaticGemRing>,
                RelicRarity.Rare,
                1,
                1,
                1,
                true),
            [EnigmaticUniqueMaterialKind.GoldIngot] = new(
                EnigmaticUniqueMaterialKind.GoldIngot,
                "enigmatic_gold_ingot",
                static () => ModelDb.Relic<EnigmaticGoldIngot>(),
                GrantMaterialStacks<EnigmaticGoldIngot>,
                RelicRarity.Rare,
                1,
                1,
                1,
                true),
            [EnigmaticUniqueMaterialKind.TriccScroll] = new(
                EnigmaticUniqueMaterialKind.TriccScroll,
                "enigmatic_tricc_scroll",
                static () => ModelDb.Relic<EnigmaticTriccScroll>(),
                GrantMaterialStacks<EnigmaticTriccScroll>,
                RelicRarity.Common,
                1,
                1,
                1,
                true),
            [EnigmaticUniqueMaterialKind.ExperienceBottle] = new(
                EnigmaticUniqueMaterialKind.ExperienceBottle,
                "enigmatic_experience_bottle",
                static () => ModelDb.Relic<EnigmaticExperienceBottle>(),
                GrantMaterialStacks<EnigmaticExperienceBottle>,
                RelicRarity.Rare,
                1,
                3,
                1,
                true),
            [EnigmaticUniqueMaterialKind.Emerald] = new(
                EnigmaticUniqueMaterialKind.Emerald,
                "enigmatic_emerald",
                static () => ModelDb.Relic<EnigmaticEmerald>(),
                GrantMaterialStacks<EnigmaticEmerald>,
                RelicRarity.Rare,
                1,
                1,
                1,
                true),
            [EnigmaticUniqueMaterialKind.AwkwardPotion] = new(
                EnigmaticUniqueMaterialKind.AwkwardPotion,
                "enigmatic_awkward_potion",
                static () => ModelDb.Relic<EnigmaticAwkwardPotion>(),
                GrantMaterialStacks<EnigmaticAwkwardPotion>,
                RelicRarity.Common,
                1,
                2,
                1,
                true),
            [EnigmaticUniqueMaterialKind.RecallPotion] = new(
                EnigmaticUniqueMaterialKind.RecallPotion,
                "enigmatic_synthesis_recall_potion",
                static () => ModelDb.Relic<EnigmaticSynthesisRecallPotion>(),
                GrantMaterialStacks<EnigmaticSynthesisRecallPotion>,
                RelicRarity.Uncommon,
                1,
                1,
                0,
                true),
            [EnigmaticUniqueMaterialKind.NetherStar] = new(
                EnigmaticUniqueMaterialKind.NetherStar,
                "enigmatic_nether_star",
                static () => ModelDb.Relic<EnigmaticNetherStar>(),
                GrantMaterialStacks<EnigmaticNetherStar>,
                RelicRarity.Rare,
                1,
                1,
                0,
                true),
            [EnigmaticUniqueMaterialKind.LapisLazuli] = new(
                EnigmaticUniqueMaterialKind.LapisLazuli,
                "enigmatic_lapis_lazuli",
                static () => ModelDb.Relic<EnigmaticLapisLazuli>(),
                GrantMaterialStacks<EnigmaticLapisLazuli>,
                RelicRarity.Uncommon,
                2,
                6,
                1,
                true),
            [EnigmaticUniqueMaterialKind.CryingObsidian] = new(
                EnigmaticUniqueMaterialKind.CryingObsidian,
                "enigmatic_crying_obsidian",
                static () => ModelDb.Relic<EnigmaticCryingObsidian>(),
                GrantMaterialStacks<EnigmaticCryingObsidian>,
                RelicRarity.Rare,
                1,
                2,
                1,
                true),
            [EnigmaticUniqueMaterialKind.EnchanterPearl] = new(
                EnigmaticUniqueMaterialKind.EnchanterPearl,
                "enigmatic_synthesis_enchanter_pearl",
                static () => ModelDb.Relic<EnigmaticSynthesisEnchanterPearl>(),
                GrantMaterialStacks<EnigmaticSynthesisEnchanterPearl>,
                RelicRarity.Rare,
                1,
                1,
                0,
                false),
            [EnigmaticUniqueMaterialKind.AbyssalHeart] = new(
                EnigmaticUniqueMaterialKind.AbyssalHeart,
                "enigmatic_abyssal_heart",
                static () => ModelDb.Relic<EnigmaticAbyssalHeart>(),
                GrantMaterialStacks<EnigmaticAbyssalHeart>,
                RelicRarity.Rare,
                1,
                1,
                0,
                false),
            [EnigmaticUniqueMaterialKind.AstralDust] = new(
                EnigmaticUniqueMaterialKind.AstralDust,
                "enigmatic_astral_dust",
                static () => ModelDb.Relic<EnigmaticAstralDust>(),
                GrantMaterialStacks<EnigmaticAstralDust>,
                RelicRarity.Rare,
                1,
                3,
                1,
                true),
            [EnigmaticUniqueMaterialKind.HeartOfTheSea] = new(
                EnigmaticUniqueMaterialKind.HeartOfTheSea,
                "enigmatic_heart_of_the_sea",
                static () => ModelDb.Relic<EnigmaticHeartOfTheSea>(),
                GrantMaterialStacks<EnigmaticHeartOfTheSea>,
                RelicRarity.Rare,
                1,
                1,
                1,
                true),
            [EnigmaticUniqueMaterialKind.CosmicHeart] = new(
                EnigmaticUniqueMaterialKind.CosmicHeart,
                "enigmatic_synthesis_cosmic_heart",
                static () => ModelDb.Relic<EnigmaticSynthesisCosmicHeart>(),
                GrantMaterialStacks<EnigmaticSynthesisCosmicHeart>,
                RelicRarity.Rare,
                1,
                1,
                0,
                false),
            [EnigmaticUniqueMaterialKind.TheTwist] = new(
                EnigmaticUniqueMaterialKind.TheTwist,
                "enigmatic_synthesis_the_twist",
                static () => ModelDb.Relic<EnigmaticSynthesisTheTwist>(),
                GrantMaterialStacks<EnigmaticSynthesisTheTwist>,
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
        foreach (var config in MaterialConfigs.Values)
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
        return RollUniqueMaterialKindWithBonuses(null, 0, 0, null, contextParts);
    }

    public static EnigmaticUniqueMaterialKind RollUniqueMaterialKindWithRareBonus(
        int rareWeightBonusPermille,
        params object?[] contextParts)
    {
        return RollUniqueMaterialKindWithBonuses(null, rareWeightBonusPermille, 0, null, contextParts);
    }

    public static EnigmaticUniqueMaterialKind RollUniqueMaterialKindWithRareBonusExcluding(
        int rareWeightBonusPermille,
        IReadOnlyCollection<EnigmaticUniqueMaterialKind>? excludedKinds,
        params object?[] contextParts)
    {
        return RollUniqueMaterialKindWithBonuses(null, rareWeightBonusPermille, 0, excludedKinds, contextParts);
    }

    public static EnigmaticUniqueMaterialKind RollUniqueMaterialKindWithBonuses(
        Player? owner,
        int rareWeightBonusPermille,
        int unownedWeightBonusPermille,
        IReadOnlyCollection<EnigmaticUniqueMaterialKind>? excludedKinds,
        params object?[] contextParts)
    {
        var availableKinds = GetRewardPoolConfigs(excludedKinds);
        if (availableKinds.Count == 0)
            return EnigmaticUniqueMaterialKind.EtheriumIngot;

        var totalWeight = availableKinds.Sum(config => GetPoolWeight(config, owner, rareWeightBonusPermille, unownedWeightBonusPermille));
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            totalWeight,
            contextParts);
        foreach (var config in availableKinds)
        {
            var weight = GetPoolWeight(config, owner, rareWeightBonusPermille, unownedWeightBonusPermille);
            if (roll < weight)
                return config.Kind;

            roll -= weight;
        }

        return availableKinds[^1].Kind;
    }

    public static EnigmaticUniqueMaterialKind RollUniqueMaterialKindFromIncludedKinds(
        IReadOnlyCollection<EnigmaticUniqueMaterialKind> includedKinds,
        int rareWeightBonusPermille,
        params object?[] contextParts)
    {
        var allowed = MaterialConfigs.Values
            .Where(config => includedKinds.Contains(config.Kind))
            .Where(static config => config.IncludeInSevenBlessingsPool && config.RewardPoolWeight > 0)
            .ToList();
        if (allowed.Count == 0)
            return EnigmaticUniqueMaterialKind.EtheriumIngot;

        var totalWeight = allowed.Sum(config => GetPoolWeight(config, null, rareWeightBonusPermille, 0));
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            totalWeight,
            contextParts);
        foreach (var config in allowed)
        {
            var weight = GetPoolWeight(config, null, rareWeightBonusPermille, 0);
            if (roll < weight)
                return config.Kind;

            roll -= weight;
        }

        return allowed[^1].Kind;
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

    private static List<EnigmaticUniqueMaterialConfig> GetRewardPoolConfigs(
        IReadOnlyCollection<EnigmaticUniqueMaterialKind>? excludedKinds)
    {
        return MaterialConfigs.Values
            .Where(static config => config.IncludeInSevenBlessingsPool && config.RewardPoolWeight > 0)
            .Where(config => excludedKinds == null || !excludedKinds.Contains(config.Kind))
            .OrderBy(config => config.RelicIdEntry, StringComparer.Ordinal)
            .ToList();
    }

    private static int GetPoolWeight(
        EnigmaticUniqueMaterialConfig config,
        Player? owner,
        int rareWeightBonusPermille,
        int unownedWeightBonusPermille)
    {
        var baseWeightPermille = config.RewardPoolWeight * 1000;
        if (rareWeightBonusPermille > 0 && config.Rarity == RelicRarity.Rare)
            baseWeightPermille += config.RewardPoolWeight * rareWeightBonusPermille;

        if (unownedWeightBonusPermille > 0 &&
            owner != null &&
            EnigmaticSynthesisRestSiteHelper.GetOwnedMaterialStacks(owner, config.Kind) <= 0)
            baseWeightPermille += config.RewardPoolWeight * unownedWeightBonusPermille;

        return baseWeightPermille;
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

    public override LocString Description
    {
        get
        {
            var description = new LocString(
                "relics",
                "RE_ASTRAL_PARTY_MOD_UNIQUE_MATERIAL_REWARD.label");
            description.Add("Material", _config.Relic.Title);
            description.Add("Amount", Amount);
            return description;
        }
    }

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
