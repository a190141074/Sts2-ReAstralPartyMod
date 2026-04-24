using System;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonSamuraiPrawn : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;

    private int _counter = 1;
    private bool _pendingCombatStartCard;
    private bool _hasCanonicalPendingCombatStartCard;

    [SavedProperty]
    public int AstralParty_PersonSamuraiPrawnCounter
    {
        get => _counter;
        set => _counter = NormalizeLegacyCounter(value);
    }

    [SavedProperty]
    public bool AstralParty_PersonSamuraiPrawnPendingCombatStartCard
    {
        get => _pendingCombatStartCard;
        set
        {
            _pendingCombatStartCard = value;
            _hasCanonicalPendingCombatStartCard = true;
        }
    }

    [SavedProperty] public bool AstralParty_PersonSamuraiPrawnFirstAttackTriggered { get; set; }

    // Keep the removed legacy save field name so older runs can deserialize after Famous Blade growth
    // moved entirely onto PersonalityDerivativeSwordIntent.
    public int AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura
    {
        get => default;
        set { }
    }

    // Preserve the earliest Samurai Prawn save field so older runs can migrate into the new cooldown model.
    public bool AstralParty_PersonSamuraiPrawnOpenedThisCombat
    {
        get => default;
        set
        {
            if (_hasCanonicalPendingCombatStartCard)
                return;

            _counter = NormalizeLegacyCounter(_counter);
            _pendingCombatStartCard = !value && IsLegacyGrantCounter(_counter);
        }
    }

    // Keep the removed legacy flag name so old saves can still deserialize without polluting SavedProperty sync.
    public new bool IsMelted
    {
        get => default;
        set { }
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFamousBlade>(),
        HoverTipFactory.FromPower<SwordAuraPower>()
    ];

    // Use the real cooldown value instead of a modulo view of an ever-growing counter.
    public override int DisplayAmount => GetClampedCounter();

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonSamuraiPrawnCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonSamuraiPrawnPendingCombatStartCard = true;
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;
        InvokeDisplayAmountChanged();

        if (Owner.GetRelic<PersonalityDerivativeSwordIntent>() == null)
            await RelicCmd.Obtain(
                ModelDb.Relic<PersonalityDerivativeSwordIntent>().ToMutable(),
                Owner
            );
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player
    )
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        await EnsureSwordIntentRelic();

        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;

        if (AstralParty_PersonSamuraiPrawnPendingCombatStartCard)
        {
            await GrantFamousBlade();
            AstralParty_PersonSamuraiPrawnPendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        // Reaching the cap means the relic is ready to generate its card and restart the cooldown.
        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantFamousBlade();
        AstralParty_PersonSamuraiPrawnCounter = 1;
        AstralParty_PersonSamuraiPrawnPendingCombatStartCard = false;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (Owner?.Creature == null)
            return;

        if (AstralParty_PersonSamuraiPrawnFirstAttackTriggered)
            return;

        if (command.Attacker != Owner.Creature || command.TargetSide == Owner.Creature.Side)
            return;

        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = true;

        if (Owner.Creature.GetPowerAmount<SwordAuraPower>() >= 3)
            return;

        Flash();
        await PowerCmd.Apply(
            ModelDb.Power<SwordAuraPower>().ToMutable(),
            Owner.Creature,
            1,
            Owner.Creature,
            null,
            false
        );
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        AdvanceCounter();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;
        AdvanceCounterAfterCombatEnd();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonSamuraiPrawnCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private static bool IsLegacyGrantCounter(int counter)
    {
        return counter == 1;
    }

    private static int NormalizeLegacyCounter(int counter)
    {
        if (counter <= 1)
            return 1;

        if (counter <= BaseMaxCounter)
            return counter;

        return (counter - 1) % 3 + 1;
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonSamuraiPrawnCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonSamuraiPrawnPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonSamuraiPrawnCounter = 1;
            AstralParty_PersonSamuraiPrawnPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantFamousBlade()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFamousBlade>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }

    private async Task EnsureSwordIntentRelic()
    {
        if (Owner == null || Owner.GetRelic<PersonalityDerivativeSwordIntent>() != null)
            return;

        await RelicCmd.Obtain(
            ModelDb.Relic<PersonalityDerivativeSwordIntent>().ToMutable(),
            Owner
        );
    }
}
