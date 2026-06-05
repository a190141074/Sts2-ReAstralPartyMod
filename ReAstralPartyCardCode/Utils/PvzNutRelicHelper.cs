using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class PvzNutRelicHelper
{
    private const string UltimateNutAttackImmunityContext = "pvz_ultimate_hyper_spacetime_nut_attack_immunity";
    private static readonly IReadOnlyList<RelicModel> RareNutRelics =
    [
        ModelDb.Relic<PvzRareHyperTemporalNut>(),
        ModelDb.Relic<PvzRareSunshineNut>(),
        ModelDb.Relic<PvzRareBigMouthedNut>(),
        ModelDb.Relic<PvzRareAngmaoNut>(),
        ModelDb.Relic<PvzRareGoldenNut>(),
        ModelDb.Relic<PvzRareDestructionNut>()
    ];
    private static readonly ModelId DistinguishedCapeId = new("RELIC", "DISTINGUISHED_CAPE");
    private static readonly ModelId BeatingRemnantId = new("RELIC", "BEATING_REMNANT");
    private static readonly ModelId BufferShieldId = ModelDb.GetId<TokenGoldBufferShield>();

    public static bool IsOwnedByTarget(Creature target, Creature ownerCreature)
    {
        if (target == ownerCreature)
            return true;

        return target.PetOwner == ownerCreature.Player;
    }

    public static bool IsEnemyAttackSource(Creature ownerCreature, Creature? dealer, CardModel? cardSource)
    {
        if (dealer == null || dealer.Side == ownerCreature.Side)
            return false;
        if (cardSource == null)
            return false;

        return cardSource.Type == CardType.Attack;
    }

    public static decimal GetThirtyPercentOfMaxHp(Creature creature)
    {
        return Math.Max(1m, Math.Ceiling(creature.MaxHp * 0.3m));
    }

    public static decimal GetTwentyPercentOfMaxHp(Creature creature)
    {
        return Math.Max(1m, Math.Ceiling(creature.MaxHp * 0.2m));
    }

    public static bool ShouldNegateEnemyAttack(Player owner, int hitOrdinal)
    {
        var seed = owner.RunState?.Rng.StringSeed ?? string.Empty;
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            100,
            seed,
            owner.NetId,
            UltimateNutAttackImmunityContext,
            hitOrdinal);
        return roll < 30;
    }

    public static bool TryAccumulateRunDamageForThreshold(
        decimal currentTotalDamageTaken,
        int currentResolvedCount,
        decimal addedDamage,
        decimal damageThreshold,
        out decimal nextTotalDamageTaken,
        out int nextResolvedCount,
        out int newlyResolvedCount)
    {
        nextTotalDamageTaken = currentTotalDamageTaken;
        nextResolvedCount = currentResolvedCount;
        newlyResolvedCount = 0;
        if (addedDamage <= 0m || damageThreshold <= 0m)
            return false;

        nextTotalDamageTaken += addedDamage;
        while (nextTotalDamageTaken >= GetNextRunDamageThreshold(nextResolvedCount, damageThreshold))
        {
            nextResolvedCount++;
            newlyResolvedCount++;
        }

        return newlyResolvedCount > 0;
    }

    public static decimal GetNextRunDamageThreshold(int resolvedCount, decimal damageThreshold)
    {
        return (resolvedCount + 1) * damageThreshold;
    }

    public static bool CanTriggerThisRound(int currentRound, int lastTriggeredRound, int cooldownRounds)
    {
        return currentRound - lastTriggeredRound >= cooldownRounds;
    }

    public static int GetRemainingCooldownRounds(int currentRound, int lastTriggeredRound, int cooldownRounds)
    {
        if (cooldownRounds <= 0)
            return 0;

        var elapsedRounds = Math.Max(0, currentRound - lastTriggeredRound);
        return Math.Clamp(cooldownRounds - elapsedRounds, 0, cooldownRounds);
    }

    public static int MarkTriggeredThisRound(int currentRound)
    {
        return currentRound;
    }

    public static int GetThresholdProgress(decimal total, decimal threshold)
    {
        if (threshold <= 0m)
            return 0;

        var normalized = total % threshold;
        if (normalized < 0m)
            normalized += threshold;
        return StableNumericStateHelper.FloorToNonNegativeInt(normalized);
    }

    public static RelicModel? GetVanillaRelicByEntry(string entry)
    {
        try
        {
            return ModelDb.GetById<RelicModel>(new ModelId("RELIC", entry));
        }
        catch
        {
            return null;
        }
    }

    public static bool PlayerOwnsUnmeltedRelic(Player? owner, ModelId relicId, out RelicModel? relic)
    {
        relic = owner?.Relics.FirstOrDefault(candidate => candidate.Id == relicId && !candidate.IsMelted);
        return relic != null;
    }

    public static bool CanFuseUltimateNut(Player? owner, out IReadOnlyList<RelicModel> fusionRelics)
    {
        fusionRelics = [];
        if (owner == null)
            return false;
        if (owner.GetRelic<PvzUltimateHyperSpacetimeNut>() != null)
            return false;

        var relics = new List<RelicModel>();
        if (owner.GetRelic<PvzRareHyperTemporalNut>() is not { IsMelted: false } hyperNut)
            return false;
        relics.Add(hyperNut);

        if (!PlayerOwnsUnmeltedRelic(owner, DistinguishedCapeId, out var cape) || cape == null)
            return false;
        if (!PlayerOwnsUnmeltedRelic(owner, BeatingRemnantId, out var remnant) || remnant == null)
            return false;
        if (!PlayerOwnsUnmeltedRelic(owner, BufferShieldId, out var bufferShield) || bufferShield == null)
            return false;

        relics.Add(cape);
        relics.Add(remnant);
        relics.Add(bufferShield);
        fusionRelics = relics;
        return true;
    }

    public static IReadOnlyList<RelicModel> GetAvailableRareNutChoices(Player owner)
    {
        return RareNutRelics
            .Where(relic => owner.Relics.All(owned => owned.CanonicalInstance.Id != relic.CanonicalInstance.Id))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static async Task MeltRelicsAsync(IEnumerable<RelicModel> relics)
    {
        foreach (var relic in relics)
        {
            if (relic.IsMelted)
                continue;
            await RelicCmd.Melt(relic);
        }
    }

    public static string SerializeHpSnapshots(IReadOnlyList<decimal> snapshots)
    {
        return StableNumericStateHelper.SerializeDecimalSequence(snapshots);
    }

    public static List<decimal> DeserializeHpSnapshots(string value)
    {
        return StableNumericStateHelper.DeserializeDecimalSequence(value);
    }

    public static string SerializeDecimal(decimal value)
    {
        return StableNumericStateHelper.SerializeDecimal(value);
    }

    public static decimal DeserializeDecimal(string? value)
    {
        return StableNumericStateHelper.DeserializeDecimal(value);
    }
}
