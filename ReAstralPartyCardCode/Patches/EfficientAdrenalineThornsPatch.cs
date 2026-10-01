using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public static class EfficientAdrenalineThornsPatch
{
    public static bool Prefix(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref Task __result)
    {
        var player = dealer?.Player ?? dealer?.PetOwner;
        if (player == null || (!HasActiveEfficientAdrenaline(player)
                               && !(cardSource?.Type == CardType.Attack && HasCrossedTwinCarp(player))))
            return true;

        __result = Task.CompletedTask;
        return false;
    }

    private static bool HasCrossedTwinCarp(Player player)
    {
        return player.Relics.OfType<TokenExclusiveCrossedTwinCarp>().Any()
               || player.Creature?.GetPowerInstances<TokenRelicBridgePower>()
                   .Where(power => power.BridgedTokenRelicId == ModelDb.GetId<TokenExclusiveCrossedTwinCarp>())
                   .Select(power => power.GetBridgedRelic())
                   .OfType<TokenExclusiveCrossedTwinCarp>()
                   .Any() == true;
    }

    private static bool HasActiveEfficientAdrenaline(Player player)
    {
        if (player.Relics.OfType<TokenGoldAdrenalineEfficient>().Any(static relic => relic.IsEffectActive))
            return true;

        var creature = player.Creature;
        if (creature == null)
            return false;

        var efficientRelicId = ModelDb.GetId<TokenGoldAdrenalineEfficient>();
        return creature.GetPowerInstances<TokenRelicBridgePower>()
            .Where(power => power.BridgedTokenRelicId == efficientRelicId)
            .Select(power => power.GetBridgedRelic())
            .OfType<TokenGoldAdrenalineEfficient>()
            .Any(static relic => relic.IsEffectActive);
    }
}
