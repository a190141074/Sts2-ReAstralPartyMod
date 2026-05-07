using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativePandaMeng : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CounterPower>(),
        HoverTipFactory.FromCard<BaseAbilityChocolateCake>(),
        HoverTipFactory.FromCard<BaseAbilityHamburger>()
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (!PandaPersonaHelper.IsFoodCard(cardPlay.Card))
            return;

        Flash();
        await PowerCmd.Apply<CounterPower>(Owner.Creature, 1m, Owner.Creature, cardPlay.Card, false);

        if (!PandaPersonaHelper.IsHamburger(cardPlay.Card))
            return;

        await PandaMaxHpHelper.GainMaxHpFromRelic(Owner.Creature, 2m, true);
        await CreatureCmd.Heal(Owner.Creature, 2m, true);
    }
}
