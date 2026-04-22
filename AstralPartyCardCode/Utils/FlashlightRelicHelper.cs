using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class FlashlightRelicHelper
{
    public static bool ShouldHandleSharedSet(RelicModel relic)
    {
        var owner = relic.Owner;
        if (owner == null)
            return false;

        return ReferenceEquals(owner.Relics.FirstOrDefault(IsFlashlightRelic), relic);
    }

    public static async Task ApplyExposureToEnemies(Player owner)
    {
        var ownerCreature = owner.Creature;
        var combatState = ownerCreature?.CombatState;
        if (ownerCreature == null || combatState == null)
            return;

        foreach (var enemy in combatState.GetOpponentsOf(ownerCreature).Where(creature => creature.IsAlive))
            await PowerCmd.Apply<ExposurePower>(enemy, 1m, ownerCreature, null, false);
    }

    public static async Task TryGrantEternalStarlightOnKill(Player owner, Creature target)
    {
        if (!target.HasPower<ExposurePower>())
            return;

        await TokenEternalStarlight.GrantStacks(owner, 1);
    }

    public static bool IsTrackedAttackHit(
        Player owner,
        Creature? dealer,
        DamageResult result,
        Creature target,
        CardModel? cardSource,
        int minimumCost)
    {
        if (owner.Creature == null)
            return false;
        if (dealer != owner.Creature)
            return false;
        if (target.Side == owner.Creature.Side || !target.IsAlive)
            return false;
        if (result.TotalDamage <= 0m)
            return false;
        if (cardSource?.Owner != owner)
            return false;
        if (cardSource.Type != CardType.Attack)
            return false;

        return GetTrackedAttackCost(cardSource) >= minimumCost;
    }

    private static bool IsFlashlightRelic(RelicModel relic)
    {
        return relic is TokenBlueFlashlightGeneral
               || relic is TokenPurpleFlashlightStronglight
               || relic is TokenGoldFlashlightFlashburst;
    }

    private static int GetTrackedAttackCost(CardModel card)
    {
        if (card.EnergyCost.CostsX)
            return 1;

        return card.EnergyCost.GetResolved();
    }
}
