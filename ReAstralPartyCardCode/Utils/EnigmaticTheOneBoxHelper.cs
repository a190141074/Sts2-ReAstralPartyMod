using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EnigmaticTheOneBoxHelper
{
    private static readonly IReadOnlySet<EnigmaticUniqueMaterialKind> BoxedKinds = new HashSet<EnigmaticUniqueMaterialKind>
    {
        EnigmaticUniqueMaterialKind.EtheriumIngot,
        EnigmaticUniqueMaterialKind.NefariousEssence,
        EnigmaticUniqueMaterialKind.NetheriteIngot,
        EnigmaticUniqueMaterialKind.RedstoneDust,
        EnigmaticUniqueMaterialKind.GhastTear,
        EnigmaticUniqueMaterialKind.BlazePowder,
        EnigmaticUniqueMaterialKind.EnderEye,
        EnigmaticUniqueMaterialKind.EarthHeart,
        EnigmaticUniqueMaterialKind.TwistedHeart,
        EnigmaticUniqueMaterialKind.PhantomMembrane,
        EnigmaticUniqueMaterialKind.DarkestScroll,
        EnigmaticUniqueMaterialKind.EnchantedBook,
        EnigmaticUniqueMaterialKind.Dye,
        EnigmaticUniqueMaterialKind.EnchantedFeather,
        EnigmaticUniqueMaterialKind.GoldIngot,
        EnigmaticUniqueMaterialKind.TriccScroll,
        EnigmaticUniqueMaterialKind.ExperienceBottle,
        EnigmaticUniqueMaterialKind.Emerald,
        EnigmaticUniqueMaterialKind.AwkwardPotion,
        EnigmaticUniqueMaterialKind.RecallPotion,
        EnigmaticUniqueMaterialKind.NetherStar,
        EnigmaticUniqueMaterialKind.LapisLazuli,
        EnigmaticUniqueMaterialKind.CryingObsidian,
        EnigmaticUniqueMaterialKind.AbyssalHeart,
        EnigmaticUniqueMaterialKind.AstralDust,
        EnigmaticUniqueMaterialKind.HeartOfTheSea,
        EnigmaticUniqueMaterialKind.CosmicHeart,
        EnigmaticUniqueMaterialKind.BlazeRod,
        EnigmaticUniqueMaterialKind.EnderRod,
        EnigmaticUniqueMaterialKind.EnderPearl,
        EnigmaticUniqueMaterialKind.EvilIngot
    };

    public static bool IsBoxedKind(EnigmaticUniqueMaterialKind kind)
    {
        return BoxedKinds.Contains(kind);
    }

    public static bool TryGetBoxedKind(RelicModel? relic, out EnigmaticUniqueMaterialKind kind)
    {
        kind = default;
        if (relic == null)
            return false;

        var canonicalId = (relic.CanonicalInstance ?? relic).Id;
        foreach (var candidate in BoxedKinds)
        {
            if (EnigmaticRewardRegistry.GetConfig(candidate).Relic.Id != canonicalId)
                continue;

            kind = candidate;
            return true;
        }

        return false;
    }

    public static EnigmaticSpecialTheOneBox? GetBox(Player? owner)
    {
        return owner?.GetRelic<EnigmaticSpecialTheOneBox>();
    }

    public static int GetStoredAmount(Player? owner, EnigmaticUniqueMaterialKind kind)
    {
        return GetBox(owner)?.GetStoredAmount(kind) ?? 0;
    }

    public static async Task GrantBoxedMaterialAsync(Player owner, EnigmaticUniqueMaterialKind kind, int amount)
    {
        if (amount <= 0 || !IsBoxedKind(kind))
            return;

        var box = await EnsureBoxAsync(owner);
        if (box == null)
            return;

        ExclusiveRelicUnlockHelper.MarkRelicUnlockedForCurrentRunAndProfile(owner, EnigmaticRewardRegistry.GetConfig(kind).Relic);
        box.AddStoredMaterial(kind, amount);
    }

    public static async Task<EnigmaticSpecialTheOneBox?> EnsureBoxAsync(Player? owner)
    {
        if (owner == null)
            return null;

        var existing = GetBox(owner);
        if (existing != null && !existing.IsMelted)
            return existing;

        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(owner, ModelDb.Relic<EnigmaticSpecialTheOneBox>());
        return GetBox(owner);
    }

    public static IReadOnlyList<EnigmaticUniqueMaterialRelicBase> GetVisibleBoxedMaterials(Player? owner)
    {
        if (owner == null)
            return [];

        return owner.Relics
            .OfType<EnigmaticUniqueMaterialRelicBase>()
            .Where(material =>
                !material.IsMelted
                && material.SynthesisAmount > 0
                && TryGetBoxedKind(material, out _))
            .OrderBy(material => material.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }
}
