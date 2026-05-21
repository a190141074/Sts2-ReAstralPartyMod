using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillShatterStar : AstralPartyCardModel
{
    private const decimal BaseDamage = 21m;
    private const decimal DamageScalePerCharge = 0.077m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromRelic<PersonalityDerivativeDivineThrone>()
    ];

    public SkillShatterStar() : base(2, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target;
        if (target == null || target.Side == Owner.Creature.Side || !target.IsAlive)
            return;

        var sara = Owner.GetRelic<VariantPersonSara>();
        var charge = sara?.GetCurrentCharge() ?? 0;
        var damage = BaseDamage * (1m + charge * (decimal)DamageScalePerCharge);
        var targetWasAlive = target.IsAlive;
        await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move, Owner.Creature, this);
        if (targetWasAlive && !target.IsAlive && sara != null)
        {
            sara.ReduceCooldownOne();
            await AstralDivinePersonaHelper.TryGrantExtraTurn(Owner, this, "断星击杀");
        }
    }
}
