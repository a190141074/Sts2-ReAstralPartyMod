using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool), StableEntryStem = "skill_not_yet_activated")]
public class SkillNotYetActivated : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique, CardKeyword.Exhaust];

    protected override bool ShouldAutoApplyCooldownEnchantment => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public SkillNotYetActivated() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature?.CombatState == null)
            return;

        var creativeAi = Owner.Creature.CombatState.CreateCard(ModelDb.Card<CreativeAi>(), Owner);
        var subroutine = Owner.Creature.CombatState.CreateCard(ModelDb.Card<Subroutine>(), Owner);

        if (IsUpgraded)
        {
            CardCmd.Upgrade(creativeAi);
            CardCmd.Upgrade(subroutine);
        }

        creativeAi.EnergyCost.UpgradeBy(-1);
        subroutine.EnergyCost.UpgradeBy(-1);

        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(creativeAi, true, CardPilePosition.Top, this);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(subroutine, true, CardPilePosition.Top, this);
    }
}
