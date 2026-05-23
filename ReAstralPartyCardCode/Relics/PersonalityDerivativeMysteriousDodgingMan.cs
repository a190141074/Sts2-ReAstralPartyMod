using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
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
public class PersonalityDerivativeMysteriousDodgingMan : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeMysteriousDodgingManLastProcessedRound { get; set; }
    [SavedProperty] public int AstralParty_PersonalityDerivativeMysteriousDodgingManCooldownCounter { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeMysteriousDodgingManCooldownCounter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeaknessInsightPower>(),
        HoverTipFactory.FromPower<ExposedFlawPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ResetCombatState();
    }

    public override Task BeforeCombatStart()
    {
        ResetCombatState();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null || player != Owner)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_PersonalityDerivativeMysteriousDodgingManLastProcessedRound == roundNumber)
            return;

        AstralParty_PersonalityDerivativeMysteriousDodgingManLastProcessedRound = roundNumber;
        if (AstralParty_PersonalityDerivativeMysteriousDodgingManCooldownCounter > 0)
        {
            AstralParty_PersonalityDerivativeMysteriousDodgingManCooldownCounter--;
            InvokeDisplayAmountChanged();
        }

        if (AstralParty_PersonalityDerivativeMysteriousDodgingManCooldownCounter > 0)
            return;

        if (MosesCombatHelper.GetWeaknessInsightAmount(Owner) < 3)
            return;

        var enemies = CombatTargetOrdering.GetLivingOpponentsStable(Owner.Creature);
        if (enemies.Count == 0)
            return;

        var targetIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            enemies.Count,
            MainFile.ModId,
            Id.Entry,
            nameof(PersonalityDerivativeMysteriousDodgingMan),
            Owner.RunState?.Rng?.StringSeed,
            Owner.NetId,
            roundNumber);
        var target = enemies[targetIndex];

        Flash();
        await PowerCmd.Apply<ExposedFlawPower>(target, 1m, Owner.Creature, null, false);
        AstralParty_PersonalityDerivativeMysteriousDodgingManCooldownCounter = 2;
        InvokeDisplayAmountChanged();
    }

    private void ResetCombatState()
    {
        AstralParty_PersonalityDerivativeMysteriousDodgingManLastProcessedRound = 0;
        AstralParty_PersonalityDerivativeMysteriousDodgingManCooldownCounter = 0;
        InvokeDisplayAmountChanged();
    }
}
