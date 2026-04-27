using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class SkillUnstoppable : AstralPartyCardModel
{
    private const decimal TemporaryStrengthAmount = 2m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ReadyToStrikePower>(),
        HoverTipFactory.FromCard<Omnislice>()
    ];

    public SkillUnstoppable() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        await BoundaryReinforcementPower.ApplyTemporaryStrength(Owner.Creature, TemporaryStrengthAmount, Owner.Creature, this);
        await PowerCmd.Apply<ReadyToStrikePower>(Owner.Creature, 1m, Owner.Creature, this, false);

        var attackCards = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card.Type == CardType.Attack)
            .ToList();
        if (attackCards.Count == 0)
            return;

        await CardCmd.Discard(choiceContext, attackCards);
        foreach (var _ in attackCards)
        {
            var omnislice = MidnightFlashHelper.CreateOmnisliceCard(Owner);
            if (omnislice != null)
                await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(omnislice, true);
        }
    }
}
