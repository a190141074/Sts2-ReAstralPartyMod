using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CardRewardAlternativeStokovPaelsWingCompatPatch : IPatchMethod
{
    public static string PatchId => "card_reward_alternative_stokov_paels_wing_compat_patch";

    public static string Description => "Drop Skip from card reward alternatives when Stokov reroll and Pael's Wing sacrifice would otherwise exceed the vanilla two-option limit";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CardRewardAlternative), nameof(CardRewardAlternative.Generate), [typeof(CardReward)])];
    }

    public static bool Prefix(CardReward cardReward, ref IReadOnlyList<CardRewardAlternative> __result)
    {
        // Rebuild the vanilla list so we can apply the narrow Stokov + Pael's Wing fallback
        // immediately before the original two-option limit check.
        var alternatives = new List<CardRewardAlternative>();
        if (cardReward.CanSkip)
        {
            alternatives.Add(new CardRewardAlternative(
                "Skip",
                PostAlternateCardRewardAction.DismissScreenAndKeepReward));
        }

        if (cardReward.CanReroll)
        {
            alternatives.Add(new CardRewardAlternative(
                "REROLL",
                cardReward.Reroll,
                PostAlternateCardRewardAction.DoNothing));
        }

        Hook.ModifyCardRewardAlternatives(cardReward.Player.RunState, cardReward.Player, cardReward, alternatives);
        StokovPaelsWingCardRewardCompatHelper.TryDropSkipForStokovPaelsWing(cardReward, alternatives);

        if (alternatives.Count > 2)
            throw new InvalidOperationException("More than 2 card reward alternatives are not supported.");

        __result = alternatives;
        return false;
    }
}
