using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "personality_derivative_sword_gust")]
public sealed class PersonalityDerivativeSwordGust : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillSakuraSetsudan>(),
        HoverTipFactory.FromCard<SkillTokenLegendarySwordGawuCut>(),
        HoverTipFactory.FromPower<SwordGustPower>()
    ];

    public async Task ResolveSakuraPlayed()
    {
        if (Owner?.Creature == null)
            return;
        if (Owner.Creature.GetPowerAmount<SwordGustPower>() < 3m)
            return;

        var power = Owner.Creature.GetPower<SwordGustPower>();
        if (power == null)
            return;

        await PowerCmd.ModifyAmount(power, -2m, Owner.Creature, null, false);
        var gawu = Owner.Creature.CombatState?.CreateCard(ModelDb.Card<SkillTokenLegendarySwordGawuCut>(), Owner);
        if (gawu != null)
            await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(gawu, true, CardPilePosition.Top, this);
    }
}
