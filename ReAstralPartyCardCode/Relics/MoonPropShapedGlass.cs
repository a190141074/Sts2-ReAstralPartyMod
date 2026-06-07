using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropShapedGlass : AstralPartyRelicModel
{
    private const int DiscountedMerchantCost = 100;

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override int MerchantCost => DiscountedMerchantCost;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner?.Creature != null)
            await MoonPropShapedGlassHelper.TryClampCurrentHpAsync(Owner.Creature);
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (Owner?.Creature == null || creature != Owner.Creature || delta <= 0m)
            return;

        await MoonPropShapedGlassHelper.TryClampCurrentHpAsync(creature);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (dealer != Owner.Creature && !(dealer == null && cardSource?.Owner == Owner))
            return 0m;

        return amount;
    }
}
