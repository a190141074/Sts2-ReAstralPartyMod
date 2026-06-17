using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public sealed class SkillSunsetGlow : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Eternal, CardKeyword.Retain, AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TsunamiPower>(),
        HoverTipFactory.FromPower<SettingSunPower>(),
        HoverTipFactory.FromPower<ConductingPower>(),
        HoverTipFactory.FromPower<ThundersBreathPower>()
    ];

    public SkillSunsetGlow() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override bool IsPlayable =>
        Owner != null && Owner.GetRelic<VariantPersonElena>()?.CanPlaySunsetGlowThisTurn(this) == true;

    protected override void OnUpgrade()
    {
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        return card == this
            ? (PileType.Hand, CardPilePosition.Top)
            : (pileType, position);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var elena = Owner?.GetRelic<VariantPersonElena>();
        if (elena == null)
            return;

        await elena.ResolveSunsetGlowPlayed(choiceContext, this, cardPlay);
    }
}
