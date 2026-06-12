using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class ProphecySoulDevourRunDeckRemovalPatch : IPatchMethod
{
    public static string PatchId => "prophecy_soul_devour_run_deck_removal_patch";
    public static string Description => "Grant Prophecy Soul Devour mineral recovery gold after formal run-deck removal succeeds";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(EventDeckCardHelper), nameof(EventDeckCardHelper.RemoveCardFromRunDeck), [typeof(Player), typeof(CardModel), typeof(bool)])];
    }

    public static async Task<bool> Postfix(Task<bool> __result, Player owner)
    {
        var removed = await __result;
        if (!removed)
            return false;

        var relic = owner.GetRelic<ProphecySoulDevour>();
        if (relic != null)
            await relic.OnRunDeckCardRemovedAsync();

        return removed;
    }
}
