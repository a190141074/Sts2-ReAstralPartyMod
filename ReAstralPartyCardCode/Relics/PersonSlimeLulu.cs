using System;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonSlimeLulu : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;

    [SavedProperty] public int AstralParty_PersonSlimeLuluCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonSlimeLuluPendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonSlimeLuluHealingSlimeUses { get; set; }

    // Preserve legacy wire/save names so older SlimeLulu runs still hydrate correctly.
    public int CombatsLeft
    {
        get => AstralParty_PersonSlimeLuluCounter;
        set => AstralParty_PersonSlimeLuluCounter = value;
    }

    public int CardsAdded
    {
        get => AstralParty_PersonSlimeLuluHealingSlimeUses;
        set => AstralParty_PersonSlimeLuluHealingSlimeUses = value;
    }

    public bool Skin
    {
        get => AstralParty_PersonSlimeLuluPendingCombatStartCard;
        set => AstralParty_PersonSlimeLuluPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillHealingSlime>(),
        HoverTipFactory.FromPower<HalfLifeHealPower>()
    ];

    public override int DisplayAmount => GetClampedCounter();

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner?.Creature == null)
            return;

        AstralParty_PersonSlimeLuluCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonSlimeLuluPendingCombatStartCard = true;
        AstralParty_PersonSlimeLuluHealingSlimeUses = 0;
        RefreshCounterDisplay();

        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            10m,
            false
        );
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        if (AstralParty_PersonSlimeLuluPendingCombatStartCard)
        {
            await GrantHealingSlime();
            AstralParty_PersonSlimeLuluPendingCombatStartCard = false;
            RefreshCounterDisplay();
            return;
        }

        if (GetClampedCounter() >= GetMaxCounter())
        {
            await GrantHealingSlime();
            AstralParty_PersonSlimeLuluCounter = 1;
            AstralParty_PersonSlimeLuluPendingCombatStartCard = false;
            RefreshCounterDisplay();
        }
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null)
            return Task.CompletedTask;

        if (side != Owner.Creature.Side)
            return Task.CompletedTask;

        AdvanceCounter();
        RefreshCounterDisplay();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AdvanceCounterAfterCombatEnd();
        RefreshCounterDisplay();
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return;

        if (result.UnblockedDamage <= 0)
            return;

        Flash();

        await PowerCmd.Apply<HalfLifeHealPower>(
            Owner.Creature,
            1m,
            Owner.Creature,
            null,
            false
        );

        AdvanceCounter();
        RefreshCounterDisplay();
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonSlimeLuluCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonSlimeLuluCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonSlimeLuluPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonSlimeLuluCounter = 1;
            AstralParty_PersonSlimeLuluPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantHealingSlime()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();

        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillHealingSlime>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }

    private void RefreshCounterDisplay()
    {
        InvokeDisplayAmountChanged();
    }
}
