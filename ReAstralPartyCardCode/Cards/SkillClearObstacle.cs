using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillClearObstacle : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<PoisonPower>(),
        HoverTipFactory.FromPower<DoomPower>(),
        HoverTipFactory.FromPower<OverloadModePower>()
    ];

    public SkillClearObstacle() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (card != this || !IsUpgraded || card.EnergyCost.CostsX)
            return false;

        modifiedCost = 0m;
        return originalCost != modifiedCost;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target;
        if (target == null || target.Side == Owner.Creature.Side || !target.IsAlive)
            return;

        await AstralNoaHelper.TriggerPoisonAndDoomOnce(choiceContext, target, Owner.Creature, this);
        await AstralNoaHelper.ClearPoisonAndDoom(target);
        await PowerCmd.Apply<OverloadModePower>(Owner.Creature, 3m, Owner.Creature, this, false);
    }
}
