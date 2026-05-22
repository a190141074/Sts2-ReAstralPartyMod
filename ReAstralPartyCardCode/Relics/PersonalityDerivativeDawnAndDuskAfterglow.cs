using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeDawnAndDuskAfterglow : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeDawnAndDuskAfterglowLastProcessedRound { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<RageOfFirePower>(),
        HoverTipFactory.FromPower<BlazingSolarBurnPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeDawnAndDuskAfterglowLastProcessedRound = 0;
    }

    public override async Task BeforeCombatStart()
    {
        AstralParty_PersonalityDerivativeDawnAndDuskAfterglowLastProcessedRound = 0;
        if (Owner == null)
            return;

        await AstralSinkouHelper.ApplyOrRefreshRageToAllEnemies(Owner, this);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null || player != Owner)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_PersonalityDerivativeDawnAndDuskAfterglowLastProcessedRound == roundNumber)
            return;

        AstralParty_PersonalityDerivativeDawnAndDuskAfterglowLastProcessedRound = roundNumber;
        if (roundNumber <= 0 || roundNumber % 5 != 0)
            return;

        Flash();
        await AstralSinkouHelper.ApplyOrRefreshRageToAllEnemies(Owner, this);
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature target,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (Owner?.Creature == null || wasRemovalPrevented || target != Owner.Creature)
            return;

        Flash();
        await AstralSinkouHelper.GrantDeathBenefitsToTeammates(Owner);
    }
}
