using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusiveLittleCarpDoll : AstralPartyRelicModel
{
    [SavedProperty] public bool AstralParty_TokenExclusiveLittleCarpDollTriggeredThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CounterPower>(),
        HoverTipFactory.FromPower<LittleCarpDollPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralDragonPalaceSeriesId)
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Card.Owner != Owner)
            return;
        if (!PersonRelicHelper.IsPersonSkillCard(cardPlay.Card))
            return;

        Flash();
        await PowerCmd.Apply<CounterPower>(Owner.Creature, 1m, Owner.Creature, cardPlay.Card, false);
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenExclusiveLittleCarpDollTriggeredThisTurn = false;
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenExclusiveLittleCarpDollTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_TokenExclusiveLittleCarpDollTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner)
            AstralParty_TokenExclusiveLittleCarpDollTriggeredThisTurn = false;

        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return;
        if (AstralParty_TokenExclusiveLittleCarpDollTriggeredThisTurn)
            return;
        if (result.TotalDamage <= 0m)
            return;
        if (dealer == null || dealer.Side == Owner.Creature.Side || dealer == Owner.Creature || dealer.IsDead)
            return;
        if (!HasRetaliationReady())
            return;

        AstralParty_TokenExclusiveLittleCarpDollTriggeredThisTurn = true;
        Flash();
        await PowerCmd.Apply<LittleCarpDollPower>(Owner.Creature, 1m, Owner.Creature, cardSource, false);
    }

    private bool HasRetaliationReady()
    {
        if (Owner?.Creature == null)
            return false;

        return Owner.Creature.GetPowerAmount<CounterPower>() > 0m
               || Owner.Creature.GetPowerAmount<ThornsPower>() > 0m
               || Owner.Creature.GetPowerAmount<ReflectPower>() > 0m;
    }
}
