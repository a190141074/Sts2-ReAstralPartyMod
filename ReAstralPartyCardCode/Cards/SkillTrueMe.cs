using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillTrueMe : AstralPartyCardModel
{
    private const decimal HealAmount = 1m;
    private const decimal BaseTemporaryStrengthAmount = 1m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WarmPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public SkillTrueMe() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.AnyPlayer)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target ?? Owner.Creature;
        var targetPlayer = target.Player;
        if (targetPlayer == null || !target.IsAlive)
            return;

        await CreatureCmd.Heal(Owner.Creature, HealAmount, true);
        if (target != Owner.Creature)
            await CreatureCmd.Heal(target, HealAmount, true);

        await AstralTemporaryStrengthPower.Apply(
            Owner.Creature,
            BaseTemporaryStrengthAmount,
            this,
            Owner.Creature,
            this,
            true);

        await AstralTemporaryStrengthPower.Apply(
            target,
            BaseTemporaryStrengthAmount,
            this,
            Owner.Creature,
            this,
            true);

        var warmAmount = (int)Owner.Creature.GetPowerAmount<WarmPower>();
        if (warmAmount < PersonDorothyHaze.MaxWarmStacks)
            return;

        var currentDexterity = (int)Owner.Creature.GetPowerAmount<DexterityPower>();
        if (currentDexterity > 0)
        {
            await AstralTemporaryStrengthPower.Apply(
                Owner.Creature,
                currentDexterity,
                this,
                Owner.Creature,
                this,
                true);
        }

        var warmPower = Owner.Creature.GetPower<WarmPower>();
        if (warmPower != null)
            await PowerCmd.Remove(warmPower);
    }
}
