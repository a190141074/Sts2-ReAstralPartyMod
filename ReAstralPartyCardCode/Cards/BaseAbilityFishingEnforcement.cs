using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class BaseAbilityFishingEnforcement : BaseAbilityCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override bool IsPlayable => BaseAbilityHelper.HasOtherLivingPlayerTarget(Owner);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BaseAbilityFishingEnforcement() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || !BaseAbilityHelper.IsOtherLivingPlayerTarget(Owner, cardPlay.Target))
            return;

        if (PandaPersonaHelper.HasAttackIntent(cardPlay.Target!))
        {
            await PowerCmd.Apply<StarLightPower>(Owner.Creature, 5m, Owner.Creature, this, false);
            return;
        }

        await CreatureCmd.GainBlock(Owner.Creature, 5m, ValueProp.Move, null);
    }
}
