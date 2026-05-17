using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class SkillChannelEnergy : AstralPartyCardModel
{
    private const decimal BaseHealAmount = 2m;
    private const decimal HealPerGatheringStrengthStack = 2m;
    private const decimal AttackDamageBonusThisTurn = 4m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(BaseHealAmount)];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<GatheringStrengthPower>(),
        HoverTipFactory.FromPower<ChannelEnergyAttackBoostPower>()
    ];

    public SkillChannelEnergy() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        var gatheringStrengthStacks = Owner.Creature.GetPowerAmount<GatheringStrengthPower>();
        var healAmount = BaseHealAmount + gatheringStrengthStacks * HealPerGatheringStrengthStack;
        if (healAmount > 0m)
            await CreatureCmd.Heal(Owner.Creature, healAmount, true);

        if (gatheringStrengthStacks > 0m)
            await PowerCmd.Apply<ChannelEnergyAttackBoostPower>(
                Owner.Creature,
                AttackDamageBonusThisTurn,
                Owner.Creature,
                this,
                false);
    }
}
