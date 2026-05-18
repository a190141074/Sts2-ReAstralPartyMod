using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillRoyalPrerogative : AstralPartyCardModel
{
    private const decimal CardsToDraw = 3m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;


    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralTemporaryId)
    ];

    public SkillRoyalPrerogative() : base(
        0,
        CardType.Skill,
        CardRarity.Ancient,
        TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner == null)
            return;

        var drawnCards = (await CardGainAttribution.RunWithSource(this,
            () => CardPileCmd.Draw(choiceContext, CardsToDraw, Owner))).ToList();
        foreach (var card in drawnCards)
            if (!card.Keywords.Contains(AstralKeywords.AstralTemporary))
                CardCmd.ApplyKeyword(card, AstralKeywords.AstralTemporary);
    }
}

