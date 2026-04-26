using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenExclusiveLittleCarpDoll : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CounterPower>(),
        HoverTipFactory.FromPower<LittleCarpDollPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralDragonPalaceSeries)
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Card.Owner != Owner)
            return;
        if (!PersonaRelicHelper.IsPersonaSkillCard(cardPlay.Card))
            return;

        Flash();
        await PowerCmd.Apply<CounterPower>(Owner.Creature, 1m, Owner.Creature, cardPlay.Card, false);
    }

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
        if (result.UnblockedDamage <= 0m)
            return;
        if (dealer == null || dealer.Side == Owner.Creature.Side || dealer == Owner.Creature || dealer.IsDead)
            return;
        if (Owner.Creature.GetPowerAmount<CounterPower>() <= 0m)
            return;

        Flash();
        await PowerCmd.Apply<LittleCarpDollPower>(Owner.Creature, 1m, Owner.Creature, cardSource, false);
    }
}