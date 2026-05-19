using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldBufferShield : AstralPartyRelicModel
{
    private const decimal HealAmount = 1m;
    private const decimal StarLightAmount = 3m;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null)
            return;
        if (!IsOwnedByTarget(target, ownerCreature))
            return;
        if (result.UnblockedDamage <= 0m)
            return;
        if (dealer == null || dealer.Side == ownerCreature.Side)
            return;

        Flash();
        await PowerCmd.Apply<HalfLifeHealPower>(ownerCreature, HealAmount, ownerCreature, null, false);
        await PowerCmd.Apply(
            ModelDb.Power<StarLightPower>().ToMutable(),
            ownerCreature,
            StarLightAmount,
            ownerCreature,
            null,
            false
        );
    }

    private static bool IsOwnedByTarget(Creature target, Creature ownerCreature)
    {
        if (target == ownerCreature)
            return true;

        return target.PetOwner == ownerCreature.Player;
    }
}
