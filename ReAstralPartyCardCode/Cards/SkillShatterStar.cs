using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Cards.DynamicVars;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillShatterStar : AstralPartyCardModel
{
    private const decimal BaseDamage = 21m;
    private const decimal DamageScalePerCharge = 0.077m;
    private const string DamageVarName = "Damage";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Computed(DamageVarName, BaseDamage, static card => ResolveDisplayedDamage(card as SkillShatterStar))
    ];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromRelic<PersonalityDerivativeDivineThrone>()
    ];

    public SkillShatterStar() : base(2, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy)
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
        var damage = CalculateDamageForCharge(sara?.GetCurrentCharge() ?? 0);
        var targetWasAlive = target.IsAlive;
        await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move, Owner.Creature, this);
        if (targetWasAlive && !target.IsAlive && sara != null)
        {
            sara.ReduceCooldownOne();
            await AstralDivinePersonaHelper.HandleShatterStarKillExtraTurn(Owner, sara, this);
        }
    }

    private static decimal ResolveDisplayedDamage(SkillShatterStar? card)
    {
        return CalculateDamageForCharge(card?.Owner?.GetRelic<VariantPersonSara>()?.GetCurrentCharge() ?? 0);
    }

    private static decimal CalculateDamageForCharge(int charge)
    {
        return BaseDamage * (1m + Math.Max(charge, 0) * DamageScalePerCharge);
    }

    public void RefreshDisplayedDamage()
    {
        // Computed dynamic vars derive their value from current charge and no longer need manual refresh.
    }
}
