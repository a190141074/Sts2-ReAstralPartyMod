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
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonSara : CooldownPersonaRelicBase
{
    private const int ChargeMilestone = 7;
    private const int ExtraTurnChargeThreshold = 21;

    [SavedProperty] public int AstralParty_VariantPersonSaraCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonSaraPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonSaraCharge { get; set; }
    [SavedProperty] public bool AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonSaraLastProcessedRound { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonSaraCounter;
        set => AstralParty_VariantPersonSaraCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonSaraPendingCombatStartCard;
        set => AstralParty_VariantPersonSaraPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 5;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override int DisplayAmount => Owner?.Creature?.CombatState != null
        ? GetClampedCounter()
        : Math.Max(AstralParty_VariantPersonSaraCharge, 0);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillShatterStar>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeDivineThrone>(),
        HoverTipFactory.FromPower<DivineSonPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_VariantPersonSaraCharge = 0;
        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = false;
        AstralParty_VariantPersonSaraLastProcessedRound = 0;
        await AstralDivinePersonaHelper.EnsureDivineThrone(Owner);
        await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner!, 0);
    }

    public override async Task BeforeCombatStart()
    {
        AstralParty_VariantPersonSaraCharge = 0;
        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = false;
        AstralParty_VariantPersonSaraLastProcessedRound = 0;
        if (Owner != null)
        {
            await AstralDivinePersonaHelper.EnsureDivineThrone(Owner);
            await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner, 0);
        }
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (Owner?.Creature?.CombatState == null || dealer != Owner.Creature)
            return;
        if (target.Side == Owner.Creature.Side)
            return;
        if (result.TotalDamage < 0m)
            return;

        var before = AstralParty_VariantPersonSaraCharge;
        AstralParty_VariantPersonSaraCharge++;
        var after = AstralParty_VariantPersonSaraCharge;
        if (after / ChargeMilestone > before / ChargeMilestone)
        {
            Flash();
            await AstralDivinePersonaHelper.SyncSaraMilestone(Owner, after, cardSource != null ? cardSource : this);
        }
        else
        {
            await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner, after);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await base.AfterTurnEnd(choiceContext, side);

        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;
        if (AstralParty_VariantPersonSaraCharge < ExtraTurnChargeThreshold)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn
            || AstralParty_VariantPersonSaraLastProcessedRound == roundNumber)
            return;

        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = true;
        AstralParty_VariantPersonSaraLastProcessedRound = roundNumber;
        AstralParty_VariantPersonSaraCharge = 0;
        await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner, 0);
        await PlayerCmd.GainEnergy(2m, Owner);
        await AstralDivinePersonaHelper.TryGrantExtraTurn(Owner, this, "萨拉21充能回合结束");
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = false;
        await Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        AstralParty_VariantPersonSaraCharge = 0;
        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = false;
        AstralParty_VariantPersonSaraLastProcessedRound = 0;
        if (Owner != null)
            await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner, 0);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillShatterStar>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    public void ReduceCooldownOne()
    {
        ReduceCooldownProgress(1);
    }

    public int GetCurrentCharge()
    {
        return Math.Max(AstralParty_VariantPersonSaraCharge, 0);
    }
}
