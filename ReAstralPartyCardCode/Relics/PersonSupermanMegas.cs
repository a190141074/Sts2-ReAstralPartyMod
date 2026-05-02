using System;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonSupermanMegas : LegacyCooldownPersonaRelicBase
{
    private const int NextTurnDrawThreshold = 5;

    [SavedProperty]
    public int AstralParty_PersonSupermanMegasCounter
    {
        get => GetCanonicalCounter();
        set => SetCanonicalCounter(value);
    }

    [SavedProperty]
    public bool AstralParty_PersonSupermanMegasPendingCombatStartCard
    {
        get => GetCanonicalPendingCombatStartCard();
        set => SetCanonicalPendingCombatStartCard(value);
    }

    // Preserve legacy wire/save names so older Superman Megas runs still hydrate correctly.
    public int AncientCard
    {
        get => default;
        set => SetLegacyCounterAliasIfMissing(value);
    }

    public bool StarterCard
    {
        get => default;
        set => SetLegacyPendingAliasIfMissing(value);
    }

    public int FurCoatCoordCols
    {
        get => default;
        set => SetLegacyCounterAliasIfMissing(value);
    }

    public bool FurCoatCoordRows
    {
        get => default;
        set => SetLegacyPendingAliasIfMissing(value);
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillSolarBombardment>(),
        HoverTipFactory.FromPower<DrawCardsNextTurnPower>()
    ];

    protected override async Task BeforeAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (PileType.Hand.GetPile(Owner).Cards.Count < NextTurnDrawThreshold)
        {
            Flash();
            await PowerCmd.Apply<DrawCardsNextTurnPower>(Owner.Creature, 1m, Owner.Creature, null);
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        if (cardPlay.Card.Owner != Owner)
            return;

        if (cardPlay.Card.CanonicalInstance is not SkillSolarBombardment)
            return;

        Flash();
        await CardGainAttribution.RunWithSource(this, () => CardPileCmd.Draw(choiceContext, 1m, Owner));
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillSolarBombardment>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
