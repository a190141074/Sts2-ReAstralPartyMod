using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonMidnightFlash : CooldownPersonRelicBase
{
    private const int CooldownReductionOnKill = 2;

    [SavedProperty] public int AstralParty_PersonMidnightFlashCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonMidnightFlashPendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonMidnightFlashCounter;
        set => AstralParty_PersonMidnightFlashCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonMidnightFlashPendingCombatStartCard;
        set => AstralParty_PersonMidnightFlashPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillUnstoppable>(),
        HoverTipFactory.FromPower<ReadyToStrikePower>(),
        HoverTipFactory.FromCard<SkillMudTruckCrash>(),
        HoverTipFactory.FromPower<FracturePower>()
    ];

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource
    )
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;
        if (dealer != Owner.Creature || target.Side == Owner.Creature.Side)
            return Task.CompletedTask;
        if (!result.WasTargetKilled)
            return Task.CompletedTask;
        if (!Owner.Creature.HasPower<ReadyToStrikePower>())
            return Task.CompletedTask;

        Flash();
        ReduceCooldownProgress(CooldownReductionOnKill);
        return Task.CompletedTask;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillUnstoppable>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
