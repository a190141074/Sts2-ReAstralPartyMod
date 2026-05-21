using System.Globalization;
using System.Text.Json;
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
    private static readonly ModelId DistinguishedCapeId = new("RELIC", "DISTINGUISHED_CAPE");
    private static readonly ModelId BeatingRemnantId = new("RELIC", "BEATING_REMNANT");
    private static readonly ModelId DemonTongueId = new("RELIC", "DEMON_TONGUE");

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

    public static decimal GetTwentyFivePercentOfMaxHp(Creature creature)
    {
        return Math.Max(1m, Math.Ceiling(creature.MaxHp * 0.25m));
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
        if (owner.GetRelic<PvzHyperTemporalNut>() is not { IsMelted: false } hyperNut)
            return false;
        relics.Add(hyperNut);

        if (!PlayerOwnsUnmeltedRelic(owner, DistinguishedCapeId, out var cape) || cape == null)
            return false;
        if (!PlayerOwnsUnmeltedRelic(owner, BeatingRemnantId, out var remnant) || remnant == null)
            return false;
        if (!PlayerOwnsUnmeltedRelic(owner, DemonTongueId, out var tongue) || tongue == null)
            return false;

        relics.Add(cape);
        relics.Add(remnant);
        relics.Add(tongue);
        fusionRelics = relics;
        return true;
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
        return JsonSerializer.Serialize(snapshots.Select(value => value.ToString(CultureInfo.InvariantCulture)).ToArray());
    }

    public static List<decimal> DeserializeHpSnapshots(string value)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<string[]>(value) ?? [];
            return raw
                .Select(item => decimal.TryParse(item, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : -1m)
                .Where(item => item >= 0m)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
