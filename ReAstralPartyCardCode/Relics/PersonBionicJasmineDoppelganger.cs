using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonBionicJasmineDoppelganger : CooldownPersonRelicBase,
    IRelicExtraIconAmountLabelsProvider,
    IRelicExtraIconAmountLabelsChangeSource
{
    private const int BaseStepThreshold = 13;
    private const int AutoProcessTurnLimit = 16;

    [SavedProperty] public int AstralParty_PersonBionicJasmineDoppelgangerCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonBionicJasmineDoppelgangerPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_PersonBionicJasmineDoppelgangerSteps { get; set; }
    [SavedProperty] public bool AstralParty_PersonBionicJasmineDoppelgangerNextBonusIsStrength { get; set; } = true;
    [SavedProperty] public int AstralParty_PersonBionicJasmineDoppelgangerPersistentStrengthBonuses { get; set; }
    [SavedProperty] public int AstralParty_PersonBionicJasmineDoppelgangerPersistentDexterityBonuses { get; set; }
    [SavedProperty] public int AstralParty_PersonBionicJasmineDoppelgangerOwnTurnCountThisCombat { get; set; }
    [SavedProperty] public int AstralParty_PersonBionicJasmineDoppelgangerProcessThisCombat { get; set; }

    public event Action? RelicExtraIconAmountLabelsInvalidated;

    protected override int CounterValue
    {
        get => AstralParty_PersonBionicJasmineDoppelgangerCounter;
        set => AstralParty_PersonBionicJasmineDoppelgangerCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonBionicJasmineDoppelgangerPendingCombatStartCard;
        set => AstralParty_PersonBionicJasmineDoppelgangerPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 4;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillEnergyOverload>(),
        HoverTipFactory.FromPower<PassivePower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralStepsId)
    ];

    public override Task BeforeCombatStart()
    {
        AstralParty_PersonBionicJasmineDoppelgangerOwnTurnCountThisCombat = 0;
        AstralParty_PersonBionicJasmineDoppelgangerProcessThisCombat = 0;
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        var shouldApplyOpeningBonuses = Owner?.Creature?.CombatState != null
                                        && Owner.Creature.Side == side
                                        && combatState.RoundNumber == 1;

        await base.BeforeSideTurnStart(choiceContext, side, combatState);

        if (shouldApplyOpeningBonuses)
            await ApplyCombatStartSetup();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return;

        AstralParty_PersonBionicJasmineDoppelgangerOwnTurnCountThisCombat++;
        if (AstralParty_PersonBionicJasmineDoppelgangerOwnTurnCountThisCombat > AutoProcessTurnLimit)
            return;

        await ApplyProcessAsync(Owner.Creature, 1m, this, Owner.Creature, null);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);

        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null)
        {
            AstralParty_PersonBionicJasmineDoppelgangerOwnTurnCountThisCombat = 0;
            AstralParty_PersonBionicJasmineDoppelgangerProcessThisCombat = 0;
            return;
        }

        var processStacks = Math.Max(0, AstralParty_PersonBionicJasmineDoppelgangerProcessThisCombat);
        var initialPointBonus = GetInitialPointStepBonus();
        AddSteps(processStacks + initialPointBonus);

        var processPower = ownerCreature.GetPower<PassivePower>();
        if (processPower != null)
            await PowerCmd.Remove(processPower);

        AstralParty_PersonBionicJasmineDoppelgangerOwnTurnCountThisCombat = 0;
        AstralParty_PersonBionicJasmineDoppelgangerProcessThisCombat = 0;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillEnergyOverload>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    public IReadOnlyList<ExtraIconAmountLabelSlot> GetRelicExtraIconAmountLabelSlots()
    {
        var steps = Math.Max(0, AstralParty_PersonBionicJasmineDoppelgangerSteps);
        if (steps <= 0)
            return [];

        return
        [
            ExtraIconAmountLabelSlot.At(ExtraIconAmountLabelCorner.TopRight, steps.ToString())
        ];
    }

    public static Task ApplyProcessAsync(
        Creature owner,
        decimal amount,
        AbstractModel originModel,
        Creature? applier,
        CardModel? cardSource)
    {
        if (amount <= 0m)
            return Task.CompletedTask;

        var player = owner.Player;
        var relic = player?.GetRelic<PersonBionicJasmineDoppelganger>();
        if (relic != null)
            relic.AddProcessThisCombat(amount);

        return PowerCmd.Apply<PassivePower>(owner, amount, applier, cardSource, false);
    }

    private async Task ApplyCombatStartSetup()
    {
        if (Owner?.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, -1m, Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, -GetOpeningDexterityLossForCurrentAct(), Owner.Creature, null);

        if (AstralParty_PersonBionicJasmineDoppelgangerPersistentStrengthBonuses > 0)
            await PowerCmd.Apply<StrengthPower>(
                Owner.Creature,
                AstralParty_PersonBionicJasmineDoppelgangerPersistentStrengthBonuses,
                Owner.Creature,
                null);

        if (AstralParty_PersonBionicJasmineDoppelgangerPersistentDexterityBonuses > 0)
            await PowerCmd.Apply<DexterityPower>(
                Owner.Creature,
                AstralParty_PersonBionicJasmineDoppelgangerPersistentDexterityBonuses,
                Owner.Creature,
                null);
    }

    private decimal GetOpeningDexterityLossForCurrentAct()
    {
        var actNumber = Math.Max((Owner?.RunState?.CurrentActIndex ?? 0) + 1, 1);
        return actNumber switch
        {
            <= 1 => 1m,
            2 => 2m,
            _ => 3m
        };
    }

    private int GetInitialPointStepBonus()
    {
        var initialPoint = Owner?.GetRelic<TokenGoldInitialPoint>();
        if (initialPoint == null)
            return 0;

        return initialPoint.AstralParty_TokenGoldInitialPointAscensionCount switch
        {
            1 => 1,
            3 => 3,
            _ => 0
        };
    }

    private void AddSteps(int stepsToAdd)
    {
        if (stepsToAdd <= 0)
            return;

        AstralParty_PersonBionicJasmineDoppelgangerSteps += stepsToAdd;
        ResolvePersistentBonusesFromSteps();
        Flash();
        NotifyDisplayChanged();
    }

    private void AddProcessThisCombat(decimal amount)
    {
        var added = StableNumericStateHelper.FloorToNonNegativeInt(amount);
        if (added <= 0)
            return;

        AstralParty_PersonBionicJasmineDoppelgangerProcessThisCombat =
            Math.Max(0, AstralParty_PersonBionicJasmineDoppelgangerProcessThisCombat) + added;
    }

    private void ResolvePersistentBonusesFromSteps()
    {
        var threshold = ExtraBatteryRelicHelper.GetAdjustedBionicJasmineStepThreshold(Owner, BaseStepThreshold);
        while (AstralParty_PersonBionicJasmineDoppelgangerSteps >= threshold)
        {
            AstralParty_PersonBionicJasmineDoppelgangerSteps -= threshold;
            if (AstralParty_PersonBionicJasmineDoppelgangerNextBonusIsStrength)
                AstralParty_PersonBionicJasmineDoppelgangerPersistentStrengthBonuses++;
            else
                AstralParty_PersonBionicJasmineDoppelgangerPersistentDexterityBonuses++;

            AstralParty_PersonBionicJasmineDoppelgangerNextBonusIsStrength =
                !AstralParty_PersonBionicJasmineDoppelgangerNextBonusIsStrength;
        }
    }

    private void NotifyDisplayChanged()
    {
        InvokeDisplayAmountChanged();
        RelicExtraIconAmountLabelsInvalidated?.Invoke();
    }
}
