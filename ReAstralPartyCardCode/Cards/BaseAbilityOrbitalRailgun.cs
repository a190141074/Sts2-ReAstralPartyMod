using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class BaseAbilityOrbitalRailgun : BaseAbilityCardModel
{
    private const int BaseDamage = 6;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, CardKeyword.Retain, AstralKeywords.AstralOrbitalBombardmentMaterial];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move | ValueProp.Unblockable)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralOrbitalBombardmentMaterialId)
    ];

    public BaseAbilityOrbitalRailgun() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
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
            BaseDamage,
            ValueProp.Move | ValueProp.Unblockable,
            Owner.Creature,
            this
        );
    }
}
