using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonSocialFearNun : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;

    private int _counter = 1;
    private bool _pendingCombatStartCard;
    private bool _boundaryAppliedThisTurn;
    private bool _hasCanonicalCounter;
    private bool _hasCanonicalPendingCombatStartCard;
    private bool _hasCanonicalBoundaryAppliedThisTurn;

    [SavedProperty]
    public int AstralParty_PersonSocialFearNunCounter
    {
        get => _counter;
        set
        {
            _counter = value;
            _hasCanonicalCounter = true;
        }
    }

    [SavedProperty]
    public bool AstralParty_PersonSocialFearNunPendingCombatStartCard
    {
        get => _pendingCombatStartCard;
        set
        {
            _pendingCombatStartCard = value;
            _hasCanonicalPendingCombatStartCard = true;
        }
    }

    [SavedProperty]
    public bool AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn
    {
        get => _boundaryAppliedThisTurn;
        set
        {
            _boundaryAppliedThisTurn = value;
            _hasCanonicalBoundaryAppliedThisTurn = true;
        }
    }

    // Keep the legacy property name for JSON save migration without registering it for network sync.
    public int GainEnergyInNextCombat
    {
        get => default;
        set
        {
            if (!_hasCanonicalCounter && value != default)
                _counter = value;
        }
    }

    // Keep the legacy property name for JSON save migration without registering it for network sync.
    public bool TurnsSeen
    {
        get => default;
        set
        {
            if (!_hasCanonicalPendingCombatStartCard && value)
                _pendingCombatStartCard = true;
        }
    }

    // Keep the legacy property name for JSON save migration without registering it for network sync.
    public bool FurCoatActIndex
    {
        get => default;
        set
        {
            if (!_hasCanonicalBoundaryAppliedThisTurn && value)
                _boundaryAppliedThisTurn = true;
        }
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override bool ShouldReceiveCombatHooks => true;

    public override int DisplayAmount => GetClampedCounter();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillIronVirgin>(),
        HoverTipFactory.FromPower<BoundaryReinforcementPower>(),
        HoverTipFactory.FromPower<IronVirginWardPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonSocialFearNunCounter = 1;
        AstralParty_PersonSocialFearNunPendingCombatStartCard = true;
        AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn = false;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonSocialFearNunPendingCombatStartCard)
        {
            await GrantIronVirgin();
            AstralParty_PersonSocialFearNunPendingCombatStartCard = false;
        }
        else if (GetClampedCounter() >= GetMaxCounter())
        {
            await GrantIronVirgin();
            AstralParty_PersonSocialFearNunCounter = 1;
            AstralParty_PersonSocialFearNunPendingCombatStartCard = false;
        }

        await ApplyBoundaryReinforcement();
        AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn = true;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        AdvanceCounter();
        AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AdvanceCounterAfterCombatEnd();
        AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public bool DidBoundaryApplyThisTurn()
    {
        return AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn;
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonSocialFearNunCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonSocialFearNunCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonSocialFearNunPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonSocialFearNunCounter = 1;
            AstralParty_PersonSocialFearNunPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantIronVirgin()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillIronVirgin>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }

    private async Task ApplyBoundaryReinforcement()
    {
        if (Owner?.Creature == null)
            return;

        await PowerCmd.Apply(
            ModelDb.Power<BoundaryReinforcementPower>().ToMutable(),
            Owner.Creature,
            BoundaryReinforcementPower.MaxDuration,
            Owner.Creature,
            null,
            false);
    }
}
