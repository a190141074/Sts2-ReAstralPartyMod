using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusiveTrident : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TridentEmpowerPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralDreamshipSeriesId)
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
            .OrderBy(player => player.NetId)
            .ToList();
        if (candidates.Count == 0)
            return;

        var targetPlayer = DeterministicMultiplayerChoiceHelper.PickDeterministically(
            candidates,
            player => player.NetId.ToString(),
            MainFile.ModId,
            Id.Entry,
            Owner.RunState.Rng.StringSeed,
            Owner.NetId,
            cardPlay.Card.Id.Entry,
            Owner.Creature.CombatState.RoundNumber);
        if (targetPlayer == null)
            return;

        Flash();
        await PowerCmd.Apply<TridentEmpowerPower>(targetPlayer.Creature!, 1m, Owner.Creature, cardPlay.Card, false);
    }
}
