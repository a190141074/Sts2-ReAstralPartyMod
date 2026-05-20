using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;

internal static class WindchaserCompat
{
    private const string WindchaserModId = "Windchaser";
    private const string WindchaserCharacterId = "WINDCHASER";

    private static readonly string[] SpellbookCardIds =
    [
        "WINDCHASER-ECHOING_FORM",
        "WINDCHASER-COMMAND_THE_STORM",
        "WINDCHASER-PURGE"
    ];

    public static bool IsLoaded()
    {
        return OptionalModCompatRegistry.IsModLoaded(WindchaserModId);
    }

    public static bool IsCharacter(Player? player)
    {
        var characterEntry = player?.Character?.Id.Entry;
        if (string.IsNullOrWhiteSpace(characterEntry))
            return false;

        return string.Equals(characterEntry, WindchaserCharacterId, StringComparison.OrdinalIgnoreCase)
               || characterEntry.StartsWith($"{WindchaserCharacterId}_", StringComparison.OrdinalIgnoreCase)
               || characterEntry.Contains(WindchaserCharacterId, StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<CardModel> GetSpellbookCards()
    {
        if (!IsLoaded())
            return [];

        var result = new List<CardModel>(SpellbookCardIds.Length);
        foreach (var cardId in SpellbookCardIds)
        {
            if (!OptionalModModelResolver.TryFindCardByEntry(cardId, out var card))
            {
                MainFile.Logger.Warn(
                    $"[{MainFile.ModId}] Windchaser spellbook card missing: id={cardId}. Skipping this entry.");
                continue;
            }

            result.Add(card);
        }

        return result;
    }
}
