using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonWeirdEgg : LegacyCooldownPersonaRelicBase
{
    private const int EventRoomBaseGoldGain = 9;

    [SavedProperty]
    public int AstralParty_PersonWeirdEggCounter
    {
        get => GetCanonicalCounter();
        set => SetCanonicalCounter(value);
    }

    [SavedProperty]
    public bool AstralParty_PersonWeirdEggPendingCombatStartCard
    {
        get => GetCanonicalPendingCombatStartCard();
        set => SetCanonicalPendingCombatStartCard(value);
    }

    [SavedProperty]
    public int AstralParty_PersonWeirdEggConsecutiveEventRooms { get; set; }

    // Read the old save field without writing it back into newly saved runs.
    public int FurCoatCoordsSet
    {
        get => default;
        set => SetLegacyCounterAliasIfMissing(value);
    }

    // Read the old save field without writing it back into newly saved runs.
    public bool StarsSpent
    {
        get => default;
        set => SetLegacyPendingAliasIfMissing(value);
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillTroubleMaker>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonWeirdEggConsecutiveEventRooms = 0;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner == null || room == null)
            return;

        if (room.RoomType != RoomType.Event)
        {
            AstralParty_PersonWeirdEggConsecutiveEventRooms = 0;
            return;
        }

        AstralParty_PersonWeirdEggConsecutiveEventRooms =
            Math.Max(0, AstralParty_PersonWeirdEggConsecutiveEventRooms) + 1;

        var goldToGain = GetEventRoomGoldGain(AstralParty_PersonWeirdEggConsecutiveEventRooms);
        Flash();
        await PersonaMultiplayerEffectHelper.GainGoldDeterministic(goldToGain, Owner);
    }

    // Keep the stored value aligned with the shown value so the cooldown is easy to reason about.
    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillTroubleMaker>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }

    private static decimal GetEventRoomGoldGain(int consecutiveEventRooms)
    {
        if (consecutiveEventRooms <= 0)
            return EventRoomBaseGoldGain;

        decimal goldToGain = EventRoomBaseGoldGain;
        for (var i = 1; i < consecutiveEventRooms; i++)
        {
            goldToGain *= 2m;
            if (goldToGain >= int.MaxValue)
                return int.MaxValue;
        }

        return goldToGain;
    }
}
