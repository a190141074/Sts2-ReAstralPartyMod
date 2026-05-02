using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusivePsychedelicSeafoodSoup : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VigorPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralDragonPalaceSeriesId)
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return;

        var vigorRoll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            2,
            MainFile.ModId,
            Id.Entry,
            Owner.RunState.Rng.StringSeed,
            Owner.NetId,
            Owner.Creature.CombatState?.RoundNumber ?? 0);
        var vigorAmount = vigorRoll == 0 ? 1m : 6m;
        Flash();
        await PowerCmd.Apply<VigorPower>(Owner.Creature, vigorAmount, Owner.Creature, null, false);
    }
}
