using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonUnclePederman : CooldownPersonRelicBase
{
    private static readonly FieldInfo? DamagePropsField =
        typeof(AttackCommand).GetField("<DamageProps>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private const int MinimumNodeValue = 1;
    private const int MaximumNodeValue = 6;
    private const int MinimumRollValue = -2;
    private const int MaximumRollValue = 2;
    private const int DefaultNodeValue = 1;

    [SavedProperty] public int AstralParty_PersonUnclePedermanCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonUnclePedermanPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_PersonUnclePedermanLastProcessedRound { get; set; }
    [SavedProperty] public bool AstralParty_PersonUnclePedermanPendingForceNodeSixNextTurn { get; set; }
    [SavedProperty] public bool AstralParty_PersonUnclePedermanPendingMaxRollOverride { get; set; }
    [SavedProperty] public int AstralParty_PersonUnclePedermanAppliedTemporaryStrengthDeltaThisTurn { get; set; }
    [SavedProperty] public int AstralParty_PersonUnclePedermanAppliedTemporaryDexterityDeltaThisTurn { get; set; }
    [SavedProperty] public bool AstralParty_PersonUnclePedermanIgnoreBlockThisTurn { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonUnclePedermanCounter;
        set => AstralParty_PersonUnclePedermanCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonUnclePedermanPendingCombatStartCard;
        set => AstralParty_PersonUnclePedermanPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillReallyAngry>(),
        HoverTipFactory.FromPower<UnclePedermanNodePower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        ResetCombatState();
        await PowerCmd.SetAmount<UnclePedermanNodePower>(Owner.Creature, DefaultNodeValue, Owner.Creature, null);
    }

    protected override async Task BeforeCooldownCardCheck(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null || player != Owner)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_PersonUnclePedermanLastProcessedRound == roundNumber)
            return;

        AstralParty_PersonUnclePedermanLastProcessedRound = roundNumber;
        await CleanupTemporaryStatDeltas();

        var nodeAmount = await ResolveNodeAmount(roundNumber);
        await ResolvePassiveRolls(roundNumber);
        AstralParty_PersonUnclePedermanIgnoreBlockThisTurn = nodeAmount == MaximumNodeValue;
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;
        if (!AstralParty_PersonUnclePedermanIgnoreBlockThisTurn)
            return Task.CompletedTask;
        if (command.Attacker != Owner.Creature || command.TargetSide == Owner.Creature.Side)
            return Task.CompletedTask;
        if (command.ModelSource is not CardModel cardSource)
            return Task.CompletedTask;
        if (cardSource.Owner != Owner || cardSource.Type != CardType.Attack)
            return Task.CompletedTask;

        DamagePropsField?.SetValue(command, command.DamageProps | ValueProp.Unblockable);
        return Task.CompletedTask;
    }

    protected override async Task BeforeAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature == null || side != Owner.Creature.Side)
            return;

        AstralParty_PersonUnclePedermanIgnoreBlockThisTurn = false;
        await CleanupTemporaryStatDeltas();
    }

    protected override Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        ResetCombatState();
        return Task.CompletedTask;
    }

    internal void QueueMaxRollOverride()
    {
        AstralParty_PersonUnclePedermanPendingMaxRollOverride = true;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillReallyAngry>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private async Task<int> ResolveNodeAmount(int roundNumber)
    {
        if (Owner?.Creature == null)
            return DefaultNodeValue;

        int nodeAmount;
        if (AstralParty_PersonUnclePedermanPendingForceNodeSixNextTurn)
        {
            nodeAmount = MaximumNodeValue;
            AstralParty_PersonUnclePedermanPendingForceNodeSixNextTurn = false;
        }
        else
        {
            nodeAmount = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                MinimumNodeValue,
                MaximumNodeValue + 1,
                MainFile.ModId,
                Id.Entry,
                "node",
                Owner.RunState.Rng.StringSeed,
                Owner.NetId,
                roundNumber);
        }

        if (nodeAmount == MinimumNodeValue)
            AstralParty_PersonUnclePedermanPendingForceNodeSixNextTurn = true;

        await PowerCmd.SetAmount<UnclePedermanNodePower>(Owner.Creature, nodeAmount, Owner.Creature, null);
        return nodeAmount;
    }

    private async Task ResolvePassiveRolls(int roundNumber)
    {
        if (Owner?.Creature == null)
            return;

        var useMaximumRoll = AstralParty_PersonUnclePedermanPendingMaxRollOverride;
        AstralParty_PersonUnclePedermanPendingMaxRollOverride = false;

        var strengthDelta = useMaximumRoll ? MaximumRollValue : RollPassiveValue("strength", roundNumber);
        var dexterityDelta = useMaximumRoll ? MaximumRollValue : RollPassiveValue("dexterity", roundNumber);
        var vigorDelta = useMaximumRoll ? MaximumRollValue : RollPassiveValue("vigor", roundNumber);
        var blockDelta = useMaximumRoll ? MaximumRollValue : RollPassiveValue("block", roundNumber);
        var energyDelta = useMaximumRoll ? MaximumRollValue : RollPassiveValue("energy", roundNumber);

        await ApplyTemporaryStrengthDelta(strengthDelta);
        await ApplyTemporaryDexterityDelta(dexterityDelta);
        await ApplyVigorDelta(vigorDelta);
        await ApplyBlockDelta(blockDelta);
        await ApplyEnergyDelta(energyDelta);
    }

    private int RollPassiveValue(string key, int roundNumber)
    {
        if (Owner == null)
            return 0;

        return DeterministicMultiplayerChoiceHelper.RollDeterministically(
            MinimumRollValue,
            MaximumRollValue + 1,
            MainFile.ModId,
            Id.Entry,
            key,
            Owner.RunState.Rng.StringSeed,
            Owner.NetId,
            roundNumber);
    }

    private async Task ApplyTemporaryStrengthDelta(int delta)
    {
        if (Owner?.Creature == null || delta == 0)
            return;

        AstralParty_PersonUnclePedermanAppliedTemporaryStrengthDeltaThisTurn += delta;
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, delta, Owner.Creature, null, true);
    }

    private async Task ApplyTemporaryDexterityDelta(int delta)
    {
        if (Owner?.Creature == null || delta == 0)
            return;

        AstralParty_PersonUnclePedermanAppliedTemporaryDexterityDeltaThisTurn += delta;
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, delta, Owner.Creature, null, true);
    }

    private async Task ApplyVigorDelta(int delta)
    {
        if (Owner?.Creature == null || delta == 0)
            return;

        if (delta > 0)
        {
            await PowerCmd.Apply<VigorPower>(Owner.Creature, delta, Owner.Creature, null, false);
            return;
        }

        var vigorPower = Owner.Creature.GetPower<VigorPower>();
        if (vigorPower == null)
            return;

        await PowerCmd.ModifyAmount(vigorPower, delta, Owner.Creature, null, true);
    }

    private async Task ApplyBlockDelta(int delta)
    {
        if (Owner?.Creature == null || delta == 0)
            return;

        if (delta > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, delta, ValueProp.Move, null);
            return;
        }

        await CreatureCmd.LoseBlock(Owner.Creature, Math.Abs(delta));
    }

    private async Task ApplyEnergyDelta(int delta)
    {
        if (Owner == null || delta == 0)
            return;

        if (delta > 0)
        {
            await PlayerCmd.GainEnergy(delta, Owner);
            return;
        }

        await PlayerCmd.LoseEnergy(Math.Abs(delta), Owner);
    }

    private async Task CleanupTemporaryStatDeltas()
    {
        if (Owner?.Creature == null)
            return;

        if (AstralParty_PersonUnclePedermanAppliedTemporaryStrengthDeltaThisTurn != 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                Owner.Creature,
                -AstralParty_PersonUnclePedermanAppliedTemporaryStrengthDeltaThisTurn,
                Owner.Creature,
                null,
                true);
            AstralParty_PersonUnclePedermanAppliedTemporaryStrengthDeltaThisTurn = 0;
        }

        if (AstralParty_PersonUnclePedermanAppliedTemporaryDexterityDeltaThisTurn != 0)
        {
            await PowerCmd.Apply<DexterityPower>(
                Owner.Creature,
                -AstralParty_PersonUnclePedermanAppliedTemporaryDexterityDeltaThisTurn,
                Owner.Creature,
                null,
                true);
            AstralParty_PersonUnclePedermanAppliedTemporaryDexterityDeltaThisTurn = 0;
        }
    }

    private void ResetCombatState()
    {
        AstralParty_PersonUnclePedermanLastProcessedRound = 0;
        AstralParty_PersonUnclePedermanPendingForceNodeSixNextTurn = false;
        AstralParty_PersonUnclePedermanPendingMaxRollOverride = false;
        AstralParty_PersonUnclePedermanAppliedTemporaryStrengthDeltaThisTurn = 0;
        AstralParty_PersonUnclePedermanAppliedTemporaryDexterityDeltaThisTurn = 0;
        AstralParty_PersonUnclePedermanIgnoreBlockThisTurn = false;
    }
}
