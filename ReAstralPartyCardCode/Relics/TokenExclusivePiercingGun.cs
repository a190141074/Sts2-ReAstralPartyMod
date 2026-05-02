using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusivePiercingGun : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralSpiritFestivalSeriesId)
    ];

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return;
        if (target.Side == Owner.Creature.Side)
            return;
        if (cardSource?.Owner != Owner || cardSource.Type != CardType.Skill)
            return;
        if (result.TotalDamage <= 0m || result.BlockedDamage <= 0m)
            return;

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            target,
            result.BlockedDamage,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim,
            Owner.Creature,
            cardSource);
    }
}