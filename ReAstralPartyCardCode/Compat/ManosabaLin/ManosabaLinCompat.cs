using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Compat.ManosabaLin;

internal static class ManosabaLinCompat
{
    private const string ManosabaLinModId = "ManosabaLin";
    private const string HiroCharacterId = "Hiro";
    private const string WithPowerEntry = "WITH_POWER";

    public static bool IsLoaded()
    {
        return OptionalModCompatRegistry.IsModLoaded(ManosabaLinModId);
    }

    public static bool IsCharacter(Player? player)
    {
        var characterEntry = player?.Character?.Id.Entry;
        if (string.IsNullOrWhiteSpace(characterEntry))
            return false;

        return string.Equals(characterEntry, HiroCharacterId, StringComparison.OrdinalIgnoreCase)
               || characterEntry.StartsWith($"{HiroCharacterId}_", StringComparison.OrdinalIgnoreCase)
               || characterEntry.Contains(HiroCharacterId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryFindWithPower(out PowerModel power)
    {
        return OptionalModModelResolver.TryFindPowerByEntry(WithPowerEntry, out power);
    }
}
