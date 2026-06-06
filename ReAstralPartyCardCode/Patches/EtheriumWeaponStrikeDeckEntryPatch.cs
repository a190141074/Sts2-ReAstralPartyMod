using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class EtheriumWeaponStrikeDeckEntryPatch : IPatchMethod
{
    public static string PatchId => "etherium_weapon_strike_deck_entry_patch";

    public static string Description =>
        "Gameplay patch: replace base strikes with the active etherium weapon strike after they enter the deck";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(typeof(AbstractModel), nameof(AbstractModel.AfterCardChangedPiles),
                [typeof(CardModel), typeof(PileType), typeof(AbstractModel)])
        ];
    }

    public static void Postfix(CardModel __instance, CardModel card, PileType oldPileType)
    {
        if (!ReferenceEquals(__instance, card))
            return;
        if (__instance.Owner == null)
            return;
        if (__instance.Pile?.Type != PileType.Deck || oldPileType == PileType.Deck)
            return;

        EtheriumWeaponStrikeReplacementHelper.TryResolveAddedCard(__instance.Owner, __instance);
    }
}
