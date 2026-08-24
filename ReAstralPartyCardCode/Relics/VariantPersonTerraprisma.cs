using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class VariantPersonTerraprisma : PersonRelicBase
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShouldReceiveCombatHooks => true;

    public override decimal ModifyDamageMultiplicative(
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? target,
        decimal amount,
        ValueProp props,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null
            || dealer != Owner.Creature
            || cardSource?.Owner != Owner
            || cardSource.Type != CardType.Attack
            || cardSource is TerraprismaCard)
        {
            return 1m;
        }

        return 0.5m;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<LoadTerraprismaPower>()
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature == null || player != Owner)
            return;
        if (Owner.Creature.GetPower<LoadTerraprismaPower>() != null)
            return;

        await PowerCmd.Apply<LoadTerraprismaPower>(Owner.Creature, 1m, Owner.Creature, null, false);
    }

}
