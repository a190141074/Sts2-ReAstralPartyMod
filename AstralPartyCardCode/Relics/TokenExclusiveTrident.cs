using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenExclusiveTrident : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TridentEmpowerPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralDreamshipSeries)
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (!PersonaRelicHelper.IsPersonaSkillCard(cardPlay.Card))
            return;

        var candidates = Owner.Creature.CombatState.Players
            .Where(player => player.Creature != null
                             && player.Creature.IsAlive
                             && !player.Creature.HasPower<TridentEmpowerPower>())
            .ToList();
        if (candidates.Count == 0)
            return;

        var targetPlayer = candidates[Owner.RunState.Rng.Niche.NextInt(candidates.Count)];
        Flash();
        await PowerCmd.Apply<TridentEmpowerPower>(targetPlayer.Creature!, 1m, Owner.Creature, cardPlay.Card, false);
    }
}