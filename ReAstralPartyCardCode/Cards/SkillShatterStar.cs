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
        new DamageVar(BaseDamage, ValueProp.Move)
    ];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromRelic<PersonalityDerivativeDivineThrone>()
    ];

    public SkillShatterStar() : base(2, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        RefreshDamageDisplay();
        return base.AfterCardChangedPiles(card, oldPileType, source);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        RefreshDamageDisplay();
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
        var damage = CalculateDamage(sara?.GetCurrentCharge() ?? 0);
        var targetWasAlive = target.IsAlive;
        await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move, Owner.Creature, this);
        if (targetWasAlive && !target.IsAlive && sara != null)
        {
            sara.ReduceCooldownOne();
            await AstralDivinePersonaHelper.HandleShatterStarKillExtraTurn(Owner, sara, this);
        }

        RefreshDamageDisplay();
    }

    private decimal CalculateDamage(int charge)
    {
        return BaseDamage * (1m + Math.Max(charge, 0) * DamageScalePerCharge);
    }

    private void RefreshDamageDisplay()
    {
        if (!DynamicVars.ContainsKey(DamageVarName))
            return;

        var sara = Owner?.GetRelic<VariantPersonSara>();
        DynamicVars[DamageVarName].BaseValue = CalculateDamage(sara?.GetCurrentCharge() ?? 0);
    }

    public void RefreshDisplayedDamage()
    {
        RefreshDamageDisplay();
    }
}
