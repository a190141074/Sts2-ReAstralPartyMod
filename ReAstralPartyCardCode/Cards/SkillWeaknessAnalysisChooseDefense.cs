using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(MosesChoiceCardPool))]
public class SkillWeaknessAnalysisChooseDefense : AstralPartyCardModel
{
    protected override string CardId => "skill_weakness_analysis_choose_defense";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    public SkillWeaknessAnalysisChooseDefense() : base(
        0,
        CardType.Skill,
        CardRarity.Ancient,
        TargetType.Self,
        showInCardLibrary: false)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}
