using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillSummonRain : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<RainGracePower>(),
        HoverTipFactory.FromPower<WaterWrapPower>()
    ];

    public SkillSummonRain() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature?.CombatState == null)
            return;

        foreach (var player in PersonMultiplayerEffectHelper.GetStableCombatPlayers(Owner))
        {
            var creature = player.Creature;
            if (creature == null || !creature.IsAlive)
                continue;

            await PowerCmd.Apply<RainGracePower>(creature, 1m, Owner.Creature, this, false);
            await PowerCmd.Apply<WaterWrapPower>(creature, 10m, Owner.Creature, this, false);
        }
    }
}
