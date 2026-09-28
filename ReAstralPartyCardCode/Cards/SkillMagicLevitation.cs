using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class SkillMagicLevitation : PersonSkillCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [AstralKeywords.AstralUnique];
    protected override bool ShouldAutoApplyCooldownEnchantment => true;
    public SkillMagicLevitation() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature != null)
            await PowerCmd.Apply<AstralLevitationPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
