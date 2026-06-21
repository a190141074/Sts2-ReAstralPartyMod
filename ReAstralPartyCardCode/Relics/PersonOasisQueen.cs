using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonOasisQueen : LegacyCooldownPersonRelicBase
{
    private const int MaxTemporaryDamageBonus = 3;

    [SavedProperty]
    public int AstralParty_PersonOasisQueenCounter
    {
        get => GetCanonicalCounter();
        set => SetCanonicalCounter(value);
    }

    [SavedProperty]
    public bool AstralParty_PersonOasisQueenPendingCombatStartCard
    {
        get => GetCanonicalPendingCombatStartCard();
        set => SetCanonicalPendingCombatStartCard(value);
    }

    // Preserve the original wire/save name so older Oasis Queen saves remain readable.
    public bool CurrentBlock
    {
        get => default;
        set => SetLegacyPendingAliasIfMissing(value);
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillRoyalPrerogative>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralTemporaryId)
    ];

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature?.CombatState == null || Owner.Creature == null)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (dealer != Owner.Creature && cardSource?.Owner != Owner)
            return 0m;

        var hand = PileType.Hand.GetPile(Owner);
        var temporaryCards = hand.Cards.Count(card => card.Keywords.Contains(AstralKeywords.AstralTemporary));
        return Math.Min(temporaryCards, MaxTemporaryDamageBonus);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillRoyalPrerogative>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }
}
