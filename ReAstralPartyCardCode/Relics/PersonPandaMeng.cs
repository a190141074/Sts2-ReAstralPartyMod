using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
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
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonPandaMeng : CooldownPersonaRelicBase
{
    private const decimal MaxHpGainRatio = 0.4m;
    private const decimal SharedSupportAmount = 2m;

    [SavedProperty] public int AstralParty_PersonPandaMengCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonPandaMengPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_PersonPandaMengObservedRegenerationAmount { get; set; }
    [SavedProperty] public int AstralParty_PersonPandaMengObservedHalfLifeHealAmount { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonPandaMengCounter;
        set => AstralParty_PersonPandaMengCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonPandaMengPendingCombatStartCard;
        set => AstralParty_PersonPandaMengPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillBigEater>(),
        HoverTipFactory.FromPower<RegenPower>(),
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativePandaMeng>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner?.Creature != null)
        {
            var bonusMaxHp = Math.Max(1m, Math.Ceiling(Owner.Creature.CurrentHp * MaxHpGainRatio));
            await PandaMaxHpHelper.GainMaxHpFromRelic(Owner.Creature, bonusMaxHp, false);
            await CreatureCmd.Heal(Owner.Creature, bonusMaxHp, false);
        }

        await EnsureDerivativeRelic();
        ResetObservedSupportPowerAmounts();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        await EnsureDerivativeRelic();
        ResetObservedSupportPowerAmounts();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, -1m, Owner.Creature, null, true);
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, -1m, Owner.Creature, null, true);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || Owner.Creature.CombatState == null)
            return;
        if (power.Owner != Owner.Creature)
            return;
        if (ReferenceEquals(cardSource, this))
            return;

        switch (power)
        {
            case RegenPower:
                await HandleSharedSupportPowerGain<RegenPower>(
                    GetObservedPowerAmount(power.Amount),
                    AstralParty_PersonPandaMengObservedRegenerationAmount,
                    value => AstralParty_PersonPandaMengObservedRegenerationAmount = value);
                break;
            case HalfLifeHealPower:
                await HandleSharedSupportPowerGain<HalfLifeHealPower>(
                    GetObservedPowerAmount(power.Amount),
                    AstralParty_PersonPandaMengObservedHalfLifeHealAmount,
                    value => AstralParty_PersonPandaMengObservedHalfLifeHealAmount = value);
                break;
        }
    }

    protected override Task BeforeCooldownCardCheck(PlayerChoiceContext choiceContext, Player player)
    {
        ResetObservedSupportPowerAmounts();
        return Task.CompletedTask;
    }

    protected override Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        ResetObservedSupportPowerAmounts();
        return Task.CompletedTask;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillBigEater>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private async Task EnsureDerivativeRelic()
    {
        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativePandaMeng>(Owner);
    }

    private async Task HandleSharedSupportPowerGain<TPower>(
        int currentAmount,
        int observedAmount,
        Action<int> setObservedAmount)
        where TPower : PowerModel
    {
        var normalizedAmount = Math.Max(0, currentAmount);
        if (normalizedAmount <= observedAmount)
        {
            setObservedAmount(normalizedAmount);
            return;
        }

        setObservedAmount(normalizedAmount);
        Flash();

        foreach (var teammate in PersonaMultiplayerEffectHelper.GetStableCombatPlayers(Owner!)
                     .Where(player => player != Owner)
                     .Where(player => player.Creature != null && player.Creature.Side == Owner!.Creature!.Side && player.Creature.IsAlive))
        {
            await PowerCmd.Apply(
                ModelDb.Power<TPower>().ToMutable(),
                teammate.Creature!,
                SharedSupportAmount,
                Owner!.Creature!,
                null,
                false);
        }
    }

    private void ResetObservedSupportPowerAmounts()
    {
        AstralParty_PersonPandaMengObservedRegenerationAmount = GetObservedPowerAmount(Owner?.Creature?.GetPowerAmount<RegenPower>() ?? 0m);
        AstralParty_PersonPandaMengObservedHalfLifeHealAmount = GetObservedPowerAmount(Owner?.Creature?.GetPowerAmount<HalfLifeHealPower>() ?? 0m);
    }

    private static int GetObservedPowerAmount(decimal amount)
    {
        return Math.Max(0, (int)Math.Ceiling(amount));
    }
}
