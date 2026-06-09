using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CreatureDamageLucidDreamPitchBlackImpulsePatch : IPatchMethod
{
    public static string PatchId => "creature_damage_lucid_dream_pitch_black_impulse";

    public static string Description => "Redirect explicit multi-hit damage to deterministic random targets for Lucid Dream Pitch Black Impulse";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(CreatureCmd),
                nameof(CreatureCmd.Damage),
                [
                    typeof(PlayerChoiceContext),
                    typeof(Creature),
                    typeof(decimal),
                    typeof(ValueProp),
                    typeof(Creature),
                    typeof(CardModel)
                ])
        ];
    }

    public static bool Prefix(
        PlayerChoiceContext choiceContext,
        ref Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        LucidDreamMaliceRuntimeHelper.TryRedirectPitchBlackImpulseTarget(
            choiceContext,
            ref target,
            amount,
            props,
            dealer,
            cardSource);
        return true;
    }
}
