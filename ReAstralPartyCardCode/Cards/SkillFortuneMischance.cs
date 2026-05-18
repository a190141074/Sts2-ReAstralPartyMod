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

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillFortuneMischance : AstralPartyCardModel
{
    private const decimal BaseDamage = 2m;
    private const decimal BaseHeal = 2m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Eternal, AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override bool IsPlayable =>
        Owner != null
        && Owner.GetRelic<PersonalityDerivativeFortuneMischance>()?.AstralParty_PersonalityDerivativeFortuneMischanceStacks >= 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(BaseHeal)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<BaiZeBlessingPower>(),
        HoverTipFactory.FromPower<GatheringStrengthPower>()
    ];

    public SkillFortuneMischance() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.AnyPlayer)
    {
    }

    protected override void OnUpgrade()
    {
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        return card == this
            ? (PileType.Hand, CardPilePosition.Top)
            : (pileType, position);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var owner = Owner;
        var ownerCreature = owner?.Creature;
        var target = cardPlay.Target;
        if (owner == null || ownerCreature == null || target == null || !target.IsAlive)
            return;
        if (owner.GetRelic<PersonalityDerivativeFortuneMischance>() is not { } derivativeRelic)
            return;
        if (!derivativeRelic.TryConsume(1))
            return;

        await CreatureCmd.Heal(target, BaseHeal, true);
        await PowerCmd.Apply<BaiZeBlessingPower>(target, 1m, ownerCreature, this, false);

        var targetPlayer = target.Player;
        if (targetPlayer?.GetRelic<PersonFeng>() != null)
            await PowerCmd.Apply<GatheringStrengthPower>(target, 1m, ownerCreature, this, false);
    }
}
