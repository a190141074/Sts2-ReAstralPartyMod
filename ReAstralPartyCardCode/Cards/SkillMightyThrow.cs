using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool), StableEntryStem = "skill_mighty_throw")]
public sealed class SkillMightyThrow : AstralPartyCardModel
{
    public SkillMightyThrow() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy, false)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Target is not { IsAlive: true } target)
            return;

        var powersToCopy = target.Powers
            .Where(IsCopyableDebuff)
            .Select(power => (Power: GetCanonicalPower(power), Amount: power.Amount))
            .Where(copy => copy.Power != null && copy.Amount > 0m)
            .Select(copy => (Power: copy.Power!, copy.Amount))
            .OrderBy(copy => copy.Power.Id.Entry, StringComparer.Ordinal)
            .ToList();
        var recipients = CombatTargetOrdering.GetNearestLivingSameSideStable(target, 2);

        foreach (var recipient in recipients)
        foreach (var copied in powersToCopy)
            await PowerCmd.Apply(copied.Power.ToMutable(), recipient, copied.Amount, Owner.Creature, this, false);

        await CommonActions.CardAttack(this, cardPlay, 1).Execute(choiceContext);
    }

    private static bool IsCopyableDebuff(PowerModel power)
    {
        return power.Type == PowerType.Debuff
               && power.Amount > 0m
               && (power.GetType().Assembly == typeof(PowerModel).Assembly
                   || power.GetType().Assembly == typeof(MainFile).Assembly)
               && GetCanonicalPower(power) != null;
    }

    private static PowerModel? GetCanonicalPower(PowerModel power)
    {
        return power.IsMutable ? ModelDb.GetById<PowerModel>(power.Id) : power;
    }
}
