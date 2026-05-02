using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonSocialFearNun : LegacyCooldownPersonaRelicBase
{
    private bool _boundaryAppliedThisTurn;
    private bool _hasCanonicalBoundaryAppliedThisTurn;

    [SavedProperty]
    public int AstralParty_PersonSocialFearNunCounter
    {
        get => GetCanonicalCounter();
        set => SetCanonicalCounter(value);
    }

    [SavedProperty]
    public bool AstralParty_PersonSocialFearNunPendingCombatStartCard
    {
        get => GetCanonicalPendingCombatStartCard();
        set => SetCanonicalPendingCombatStartCard(value);
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
        set => SetLegacyCounterAliasIfMissing(value);
    }

    // Keep the legacy property name for JSON save migration without registering it for network sync.
    public bool TurnsSeen
    {
        get => default;
        set => SetLegacyPendingAliasIfMissing(value);
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

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillIronVirgin>(),
        HoverTipFactory.FromPower<BoundaryReinforcementPower>(),
        HoverTipFactory.FromPower<IronVirginWardPower>()
    ];

    protected override async Task BeforeCooldownCardCheck(PlayerChoiceContext choiceContext, Player player)
    {
        await ApplyBoundaryReinforcement();
        AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn = true;
    }

    protected override Task AfterAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn = false;
        return Task.CompletedTask;
    }

    protected override Task AfterAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn = false;
        return Task.CompletedTask;
    }

    public bool DidBoundaryApplyThisTurn()
    {
        return AstralParty_PersonSocialFearNunBoundaryAppliedThisTurn;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillIronVirgin>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
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
