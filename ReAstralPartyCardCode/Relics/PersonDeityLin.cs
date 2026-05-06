using System.Collections.Generic;
using System.Threading.Tasks;
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
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonDeityLin : CooldownPersonaRelicBase
{
    [SavedProperty] public int AstralParty_PersonDeityLinCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonDeityLinPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_PersonDeityLinConsumedLivingFolioStacksProgress { get; set; }
    [SavedProperty] public int AstralParty_PersonDeityLinPermanentLivingFolioDamageBonus { get; set; }
    [SavedProperty] public int AstralParty_PersonDeityLinLivingFolioRefundsThisTurn { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonDeityLinCounter;
        set => AstralParty_PersonDeityLinCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonDeityLinPendingCombatStartCard;
        set => AstralParty_PersonDeityLinPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SurveyFindsPower>(),
        HoverTipFactory.FromCard<SkillLivingFolio>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeLivingFolio>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await EnsureLivingFolioRelic();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        AstralParty_PersonDeityLinLivingFolioRefundsThisTurn = 0;
        await EnsureLivingFolioRelic();

        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillLivingFolio>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    protected override Task BeforeCooldownCardCheck(PlayerChoiceContext choiceContext, Player player)
    {
        AstralParty_PersonDeityLinLivingFolioRefundsThisTurn = 0;
        return Task.CompletedTask;
    }

    protected override Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonDeityLinLivingFolioRefundsThisTurn = 0;
        return Task.CompletedTask;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature == null)
            return;

        Flash();
        Owner.GetRelic<PersonalityDerivativeLivingFolio>()?.AddStacksCapped(1);
        await PowerCmd.Apply<SurveyFindsPower>(Owner.Creature, 1m, Owner.Creature, null, false);
    }

    public int GetLivingFolioPermanentDamageBonus()
    {
        return System.Math.Max(AstralParty_PersonDeityLinPermanentLivingFolioDamageBonus, 0);
    }

    public void RecordLivingFolioConsumption(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_PersonDeityLinConsumedLivingFolioStacksProgress += amount;
        var grew = false;
        while (TryAdvanceLivingFolioGrowth())
            grew = true;

        if (grew)
            Flash();
    }

    public bool TryRefundLivingFolioEnergy()
    {
        if (Owner == null)
            return false;

        var refundCap = GetLivingFolioRefundCapForCurrentTurn();
        if (refundCap <= 0)
            return false;
        if (AstralParty_PersonDeityLinLivingFolioRefundsThisTurn >= refundCap)
            return false;

        AstralParty_PersonDeityLinLivingFolioRefundsThisTurn++;
        return true;
    }

    private int GetLivingFolioRefundCapForCurrentTurn()
    {
        var currentMaxEnergy = (int)(Owner?.PlayerCombatState?.MaxEnergy ?? Owner?.MaxEnergy ?? 0m);
        return System.Math.Max(0, 9 - currentMaxEnergy);
    }

    private async Task EnsureLivingFolioRelic()
    {
        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeLivingFolio>(Owner);
    }

    private bool TryAdvanceLivingFolioGrowth()
    {
        var currentBonus = GetLivingFolioPermanentDamageBonus();
        var requiredStacks = GetRequiredConsumptionForNextGrowth(currentBonus);
        if (AstralParty_PersonDeityLinConsumedLivingFolioStacksProgress < requiredStacks)
            return false;

        AstralParty_PersonDeityLinConsumedLivingFolioStacksProgress -= requiredStacks;
        AstralParty_PersonDeityLinPermanentLivingFolioDamageBonus++;
        return true;
    }

    private static int GetRequiredConsumptionForNextGrowth(int currentBonus)
    {
        if (currentBonus <= 11)
            return 1;
        if (currentBonus <= 22)
            return 2;
        return 3;
    }
}
