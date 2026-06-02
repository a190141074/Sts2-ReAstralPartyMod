using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class CursedScrollGrabBagHelper
{
    private const int BaseScrollCopies = 1;
    private const int SevenCursesScrollCopies = 3;

    private static readonly FieldInfo? DequesField = typeof(RelicGrabBag)
        .GetField("_deques", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? OriginalRelicsField = typeof(RelicGrabBag)
        .GetField("_originalRelics", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void NormalizeForOwner(Player? owner)
    {
        if (owner == null)
            return;

        Normalize(owner.RelicGrabBag, owner);
    }

    public static void Normalize(RelicGrabBag grabBag, Player? owner)
    {
        if (owner == null)
            return;
        if (DequesField?.GetValue(grabBag) is not Dictionary<RelicRarity, List<RelicModel>> deques)
            return;
        if (!deques.TryGetValue(RelicRarity.Rare, out var rareDeque) || rareDeque.Count == 0)
            return;

        var scrollId = ModelDb.Relic<EnigmaticCursedScroll>().Id;
        var existingScroll = rareDeque.FirstOrDefault(relic => relic.Id == scrollId);
        if (existingScroll == null)
            return;

        var targetCopies = owner.GetRelic<EnigmaticSevenCurses>() != null
            ? SevenCursesScrollCopies
            : BaseScrollCopies;

        NormalizeList(rareDeque, existingScroll, scrollId, targetCopies, owner, "current_rare_deque");

        if (OriginalRelicsField?.GetValue(grabBag) is List<RelicModel> originalRelics)
            NormalizeList(originalRelics, existingScroll, scrollId, targetCopies, owner, "original_relics");
    }

    private static void NormalizeList(
        List<RelicModel> list,
        RelicModel scrollTemplate,
        ModelId scrollId,
        int targetCopies,
        Player owner,
        string listLabel)
    {
        var existingCount = list.Count(relic => relic.Id == scrollId);
        if (existingCount == 0 || existingCount == targetCopies)
            return;

        list.RemoveAll(relic => relic.Id == scrollId);
        for (var i = 0; i < targetCopies; i++)
        {
            var insertIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                list.Count + 1,
                MainFile.ModId,
                nameof(CursedScrollGrabBagHelper),
                listLabel,
                owner.NetId,
                owner.RunState?.Rng.StringSeed ?? "no_seed",
                targetCopies,
                i);
            list.Insert(insertIndex, scrollTemplate);
        }
    }
}
