using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenBlueElegantFeather : AstralPartyRelicModel
{
    private const decimal MaxStacks = 3m;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ElegantFeatherPower>()
    ];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return;
        if (result.TotalDamage <= 0m)
            return;
        if (result.UnblockedDamage > 0m)
            return;

        var currentStacks = Owner.Creature.GetPowerAmount<ElegantFeatherPower>();
        if (currentStacks >= MaxStacks)
            return;

        Flash();
        await PowerCmd.Apply<ElegantFeatherPower>(
            Owner.Creature,
            Math.Min(1m, MaxStacks - currentStacks),
            Owner.Creature,
            cardSource,
            false
        );
    }
}