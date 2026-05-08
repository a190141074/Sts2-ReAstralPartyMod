using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class SkillComeHereYou : AstralPartyCardModel
{
    private const decimal DamageAmount = 5m;
    private const decimal DebuffAmount = 1m;
    private const decimal DexterityLoss = -7m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<DebilitatePower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public SkillComeHereYou() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Target == null)
            return;

        await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            DamageAmount,
            ValueProp.Move,
            Owner.Creature,
            this);
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, DebuffAmount, Owner.Creature, this, false);
        await PowerCmd.Apply<DebilitatePower>(cardPlay.Target, DebuffAmount, Owner.Creature, this, false);
        await PowerCmd.Apply<DexterityPower>(cardPlay.Target, DexterityLoss, Owner.Creature, this, false);
    }
}
