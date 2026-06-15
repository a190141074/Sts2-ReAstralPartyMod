using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class AncientRelicGroupRegistry
{
    private const string LocStem = "RE_ASTRAL_PARTY_MOD_PROPHECY_SOUL_DEVOUR_ANCIENT_GROUP";

    private static readonly IReadOnlyList<AncientRelicGroupDefinition> Definitions =
    [
        Create(
            "PAEL",
            "paels_eye",
            10,
            relicIdPrefixes: ["paels_"]),
        Create(
            "NEOW",
            "neows_talisman",
            20,
            relicIds:
            [
                "neows_talisman",
                "neows_bones",
                "neows_torment",
                "divine_destiny",
                "divine_right"
            ]),
        Create(
            "WONGO",
            "wongos_mystery_ticket",
            30,
            relicIds:
            [
                "wongos_mystery_ticket",
                "wongo_customer_appreciation_badge"
            ]),
        Create(
            "VAKUU",
            "vakuu_card_selector",
            40,
            relicIds:
            [
                "vakuu_card_selector",
                "history_course",
                "miniature_cannon",
                "planisphere"
            ]),
        Create(
            "LOST",
            "lost_wisp",
            50,
            relicIds:
            [
                "lost_wisp",
                "lost_coffer",
                "forgotten_soul",
                "undying_sigil"
            ]),
        Create(
            "ROYAL",
            "royal_stamp",
            60,
            relicIds:
            [
                "royal_stamp",
                "royal_poison",
                "diamond_diadem",
                "distinguished_cape"
            ]),
        Create(
            "MARTIAL",
            "sword_of_jade",
            70,
            relicIds:
            [
                "sword_of_jade",
                "sword_of_stone",
                "iron_club",
                "crossbow",
                "parrying_shield",
                "spiked_gauntlets",
                "helical_dart"
            ]),
        Create(
            "BEAST",
            "byrdpip",
            80,
            relicIds:
            [
                "byrdpip",
                "ghost_seed",
                "fragrant_mushroom",
                "big_mushroom",
                "fur_coat",
                "reptile_trinket"
            ]),
        Create(
            "MYSTIC",
            "arcane_scroll",
            90,
            relicIds:
            [
                "arcane_scroll",
                "massive_scroll",
                "storybook",
                "dusty_tome",
                "music_box",
                "glass_eye",
                "fiddle"
            ]),
        Create(
            "OTHER",
            "toolbox",
            9999,
            relicIds:
            [
                "toolbox"
            ])
    ];

    public static IReadOnlyList<AncientRelicGroupDefinition> All => Definitions;

    public static IReadOnlyList<AncientRelicGroupOption> BuildOptions(IReadOnlyList<RelicModel> relics)
    {
        var grouped = new Dictionary<string, List<RelicModel>>(StringComparer.Ordinal);
        foreach (var relic in relics)
        {
            var definition = ResolveDefinition(relic);
            if (!grouped.TryGetValue(definition.GroupKey, out var bucket))
            {
                bucket = [];
                grouped[definition.GroupKey] = bucket;
            }

            bucket.Add(relic);
        }

        return Definitions
            .Where(definition => grouped.ContainsKey(definition.GroupKey))
            .Select(definition =>
            {
                var groupRelics = grouped[definition.GroupKey]
                    .OrderBy(relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry, StringComparer.Ordinal)
                    .ToList();
                var representative = ResolveRepresentative(definition, groupRelics);
                return new AncientRelicGroupOption(definition, representative, groupRelics);
            })
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.GroupKey, StringComparer.Ordinal)
            .ToList();
    }

    private static AncientRelicGroupDefinition ResolveDefinition(RelicModel relic)
    {
        foreach (var definition in Definitions)
        {
            if (definition.GroupKey == "OTHER")
                continue;
            if (definition.Matches(relic))
                return definition;
        }

        return Definitions.First(static definition => definition.GroupKey == "OTHER");
    }

    private static RelicModel ResolveRepresentative(AncientRelicGroupDefinition definition, IReadOnlyList<RelicModel> relics)
    {
        var representative = relics.FirstOrDefault(relic =>
            string.Equals((relic.CanonicalInstance?.Id ?? relic.Id).Entry, definition.RepresentativeRelicId, StringComparison.Ordinal));
        return representative ?? relics[0];
    }

    private static AncientRelicGroupDefinition Create(
        string groupKey,
        string representativeRelicId,
        int displayOrder,
        IReadOnlyList<string>? relicIds = null,
        IReadOnlyList<string>? relicIdPrefixes = null)
    {
        return new AncientRelicGroupDefinition(
            groupKey,
            $"{LocStem}.{groupKey}.title",
            $"{LocStem}.{groupKey}.description",
            representativeRelicId,
            displayOrder,
            relicIds ?? [],
            relicIdPrefixes ?? []);
    }
}
