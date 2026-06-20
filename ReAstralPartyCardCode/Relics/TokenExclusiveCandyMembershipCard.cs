using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interactions.RightClick;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusiveCandyMembershipCard : AstralPartyRelicModel, IModRightClickableRelic
{
    private const int CooldownRounds = 2;

    [SavedProperty] public int AstralParty_TokenExclusiveCandyMembershipCardNextReadyTurnIndex { get; set; } = 1;
    [SavedProperty] public int AstralParty_TokenExclusiveCandyMembershipCardCurrentTurnIndex { get; set; } = 1;
    [SavedProperty] public int AstralParty_TokenExclusiveCandyMembershipCardLastProcessedRound { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override int DisplayAmount => GetRemainingCooldownRounds();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillTokenCuriousCandyMachine>(),
        HoverTipFactory.FromPower<ModificationPower>(),
        HoverTipFactory.FromPower<DoomPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralGhostAlleySetId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenExclusiveCandyMembershipCardNextReadyTurnIndex =
            Math.Max(1, AstralParty_TokenExclusiveCandyMembershipCardNextReadyTurnIndex);
        AstralParty_TokenExclusiveCandyMembershipCardCurrentTurnIndex =
            Math.Max(1, AstralParty_TokenExclusiveCandyMembershipCardCurrentTurnIndex);
        RefreshDisplay();
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenExclusiveCandyMembershipCardLastProcessedRound = 0;
        RefreshDisplay();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return Task.CompletedTask;

        var currentRound = Owner.Creature.CombatState.RoundNumber;
        if (currentRound <= AstralParty_TokenExclusiveCandyMembershipCardLastProcessedRound)
        {
            RefreshDisplay();
            return Task.CompletedTask;
        }

        AstralParty_TokenExclusiveCandyMembershipCardLastProcessedRound = currentRound;
        AstralParty_TokenExclusiveCandyMembershipCardCurrentTurnIndex =
            Math.Max(AstralParty_TokenExclusiveCandyMembershipCardCurrentTurnIndex + 1, 1);
        RefreshDisplay();
        return Task.CompletedTask;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Combat.CombatSide side)
    {
        RefreshDisplay();
        return Task.CompletedTask;
    }

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        return context.Player == Owner
               && Owner?.Creature?.CombatState != null;
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context)
    {
        return context.Player == Owner
               && Owner?.Creature?.CombatState != null
               && AstralParty_TokenExclusiveCandyMembershipCardCurrentTurnIndex
               >= AstralParty_TokenExclusiveCandyMembershipCardNextReadyTurnIndex;
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (AstralParty_TokenExclusiveCandyMembershipCardCurrentTurnIndex
            < AstralParty_TokenExclusiveCandyMembershipCardNextReadyTurnIndex)
            return;

        await CandyMachineHelper.CreateSkillTokenCuriousCandyMachineCardInHand(Owner);
        AstralParty_TokenExclusiveCandyMembershipCardNextReadyTurnIndex =
            AstralParty_TokenExclusiveCandyMembershipCardCurrentTurnIndex + CooldownRounds;
        RefreshDisplay();
        Flash();
    }

    private int GetRemainingCooldownRounds()
    {
        if (Owner?.Creature?.CombatState == null)
            return 0;

        var remaining = AstralParty_TokenExclusiveCandyMembershipCardNextReadyTurnIndex
                        - AstralParty_TokenExclusiveCandyMembershipCardCurrentTurnIndex;
        return Math.Max(0, remaining);
    }

    private void RefreshDisplay()
    {
        InvokeDisplayAmountChanged();
    }
}
