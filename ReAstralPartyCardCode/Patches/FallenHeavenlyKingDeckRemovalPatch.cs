using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class FallenHeavenlyKingDeckRemovalPatch : IPatchMethod
{
    public static string PatchId => "fallen_heavenly_king_deck_removal_patch";
    public static string Description => "Queue Heavenly King deck replacement after formal run-deck removal completes";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromDeck),
                [typeof(IReadOnlyList<CardModel>), typeof(bool)])
        ];
    }

    public static void Postfix(IReadOnlyList<CardModel> cards, ref Task __result)
    {
        var affectedOwners = cards
            .Select(static card => card.Owner)
            .OfType<Player>()
            .Select(static owner => new DeckOwnerSnapshot(owner.RunState as RunState, owner.NetId))
            .Where(static snapshot => snapshot.RunState != null)
            .Distinct()
            .ToArray();

        if (affectedOwners.Length == 0)
            return;

        __result = AfterDeckRemoval(__result, affectedOwners);
    }

    private static async Task AfterDeckRemoval(Task originalTask, IReadOnlyList<DeckOwnerSnapshot> affectedOwners)
    {
        await originalTask;

        foreach (var owner in affectedOwners)
            FallenHeavenlyKingDeckHelper.OnRunDeckRemovalCompleted(owner.RunState!, owner.NetId);
    }

    private sealed record DeckOwnerSnapshot(RunState? RunState, ulong NetId);
}
