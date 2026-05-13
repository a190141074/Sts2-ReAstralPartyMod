using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonCyberKitty : CooldownPersonaRelicBase
{
    private const int PassiveTemporaryStatAmount = 1;
    private const int NodeThreshold = 3;

    [SavedProperty] public int AstralParty_PersonCyberKittyCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonCyberKittyPendingCombatStartCard { get; set; }

    [SavedProperty] public bool AstralParty_PersonCyberKittyTriggeredThisTurn { get; set; }

    [SavedProperty] public int AstralParty_PersonCyberKittyLastProcessedRound { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonCyberKittyCounter;
        set => AstralParty_PersonCyberKittyCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonCyberKittyPendingCombatStartCard;
        set => AstralParty_PersonCyberKittyPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillRemoteIntrusion>(),
        HoverTipFactory.FromPower<CyberKittyNodePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonCyberKittyTriggeredThisTurn = false;
        AstralParty_PersonCyberKittyLastProcessedRound = 0;
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        AstralParty_PersonCyberKittyTriggeredThisTurn = false;
        AstralParty_PersonCyberKittyLastProcessedRound = 0;
        await PowerCmd.SetAmount<CyberKittyNodePower>(Owner.Creature, 4m, Owner.Creature, null);
    }

    protected override async Task BeforeCooldownCardCheck(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null || Owner.Creature == null)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_PersonCyberKittyLastProcessedRound == roundNumber)
            return;

        AstralParty_PersonCyberKittyLastProcessedRound = roundNumber;
        AstralParty_PersonCyberKittyTriggeredThisTurn = false;
        var rerolledNode = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            1,
            11,
            MainFile.ModId,
            Id.Entry,
            nameof(CyberKittyNodePower),
            Owner.RunState.Rng.StringSeed,
            Owner.NetId,
            roundNumber);
        await PowerCmd.SetAmount<CyberKittyNodePower>(Owner.Creature, rerolledNode, Owner.Creature, null);

        var nodeAmount = (int)Owner.Creature.GetPowerAmount<CyberKittyNodePower>();
        if (nodeAmount <= NodeThreshold)
            return;

        Flash();
        await AstralTemporaryStrengthPower.Apply(Owner.Creature, PassiveTemporaryStatAmount, this, Owner.Creature, null, true);
        await AstralTemporaryDexterityPower.Apply(Owner.Creature, PassiveTemporaryStatAmount, this, Owner.Creature, null, true);
        await CyberKittyCombatHelper.GainRandomAttackCardDiscounted(choiceContext, Owner, this);
    }

    protected override Task BeforeAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        AstralParty_PersonCyberKittyTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Attack)
            return;
        if (AstralParty_PersonCyberKittyTriggeredThisTurn)
            return;
        if (Owner.Creature.HasPower<CyberKittyFirewallBypassPower>())
            return;

        AstralParty_PersonCyberKittyTriggeredThisTurn = true;
        Flash();

        var discardedAttack = await CyberKittyCombatHelper.DiscardLeftmostAttackAndUpgradeForCombat(choiceContext, Owner);
        if (discardedAttack != null)
            return;

        CyberKittyCombatHelper.UpgradeRandomAttackForCombat(Owner);
    }

    protected override Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonCyberKittyTriggeredThisTurn = false;
        AstralParty_PersonCyberKittyLastProcessedRound = 0;
        return Task.CompletedTask;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillRemoteIntrusion>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }
}
