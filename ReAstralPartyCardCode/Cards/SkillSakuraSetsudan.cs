using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool), StableEntryStem = "skill_sakura_setsudan")]
public sealed class SkillSakuraSetsudan : AstralPartyCardModel
{
    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    public SkillSakuraSetsudan() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy, false)
    {
    }

    protected override IEnumerable<MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> CanonicalVars =>
        [new MegaCrit.Sts2.Core.Localization.DynamicVars.DamageVar(2m, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move)];

    protected override string DefaultPortraitPath =>
        "res://ReAstralPartyMod/images/card_portraits/skill_sakura_setsudan.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Target == null)
            return;

        await CommonActions.CardAttack(this, cardPlay, 1).Execute(choiceContext);
        if (Owner.GetRelic<PersonKatanaMaster>() == null)
            return;

        var swordGustRelic = Owner.GetRelic<PersonalityDerivativeSwordGust>();
        if (swordGustRelic != null)
            await swordGustRelic.ResolveSakuraPlayed();

        var amount = Owner.Creature.GetPowerAmount<SwordGustPower>();
        if (amount < 3m)
            await PowerCmd.Apply(ModelDb.Power<SwordGustPower>().ToMutable(), Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
