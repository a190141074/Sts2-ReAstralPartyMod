using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonWeirdEgg : LegacyCooldownPersonaRelicBase
{
    private const int EventRoomBaseGoldGain = 9;
    private const decimal StarLightPerTriggeredEventCard = 3m;

    protected override string RelicId => "variant_person_weird_egg";

    [SavedProperty]
    public int AstralParty_VariantPersonWeirdEggCounter
    {
        get => GetCanonicalCounter();
        set => SetCanonicalCounter(value);
    }

    [SavedProperty]
    public bool AstralParty_VariantPersonWeirdEggPendingCombatStartCard
    {
        get => GetCanonicalPendingCombatStartCard();
        set => SetCanonicalPendingCombatStartCard(value);
    }

    [SavedProperty] public int AstralParty_VariantPersonWeirdEggConsecutiveEventRooms { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillAnomalyMaker>(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_VariantPersonWeirdEggConsecutiveEventRooms = 0;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner == null || room == null)
            return;

        if (room.RoomType != RoomType.Event)
        {
            AstralParty_VariantPersonWeirdEggConsecutiveEventRooms = 0;
            return;
        }

        AstralParty_VariantPersonWeirdEggConsecutiveEventRooms =
            Math.Max(0, AstralParty_VariantPersonWeirdEggConsecutiveEventRooms) + 1;

        var goldToGain = GetEventRoomGoldGain(AstralParty_VariantPersonWeirdEggConsecutiveEventRooms);
        Flash();
        await PersonaMultiplayerEffectHelper.GainGoldDeterministic(goldToGain, Owner);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (cardPlay.Card is SkillAnomalyMaker)
            return;
        if (!AstralAnomalyEventCardPool.IsAnomalyEventCard(cardPlay.Card))
            return;

        Flash();
        await PowerCmd.Apply<StarLightPower>(
            Owner.Creature,
            StarLightPerTriggeredEventCard,
            Owner.Creature,
            cardPlay.Card,
            false);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillAnomalyMaker>(), Owner);
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
