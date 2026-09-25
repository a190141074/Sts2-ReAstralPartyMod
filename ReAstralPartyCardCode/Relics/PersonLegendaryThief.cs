using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonLegendaryThief : CooldownPersonRelicBase
{
    [SavedProperty] public int AstralParty_PersonLegendaryThiefCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonLegendaryThiefPendingCombatStartCard { get; set; }
    [SavedProperty] public decimal AstralParty_PersonLegendaryThiefAppliedDexterity { get; set; }

    protected override int CounterValue { get => AstralParty_PersonLegendaryThiefCounter; set => AstralParty_PersonLegendaryThiefCounter = value; }
    protected override bool PendingCombatStartCard { get => AstralParty_PersonLegendaryThiefPendingCombatStartCard; set => AstralParty_PersonLegendaryThiefPendingCombatStartCard = value; }
    protected override int BaseMaxCounter => 3;
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [HoverTipFactory.FromCard<SkillSleightOfHand>(), HoverTipFactory.FromPower<WindfallHitsPower>(), HoverTipFactory.FromPower<StarLightPower>(), HoverTipFactory.FromPower<DexterityPower>()];

    public override async Task BeforeCombatStart()
    {
        AstralParty_PersonLegendaryThiefAppliedDexterity = 0m;
        await SyncDexterity();
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner?.Creature == null || power.Owner != Owner.Creature || power is not StarLightPower)
            return;
        await SyncDexterity();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return;
        await PowerCmd.Apply<WindfallHitsPower>(Owner.Creature, 1m, Owner.Creature, cardPlay.Card, false);
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature || cardSource?.Owner != Owner || cardSource.Type != CardType.Attack)
            return 0m;
        return amount > 0m ? -1m : 0m;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (Owner?.Creature == null || target != Owner.Creature || wasRemovalPrevented)
            return;

        var players = PersonMultiplayerEffectHelper.GetStableCombatPlayers(Owner)
            .Where(player => player.Creature?.IsAlive == true)
            .ToList();
        if (players.Count == 0)
            return;

        var total = Math.Max(0m, Owner.Gold) + 60m;
        var share = Math.Floor(total / players.Count);
        var remainder = total - share * players.Count;
        if (Owner.Gold > 0m)
            await PersonMultiplayerEffectHelper.LoseGoldDeterministic(Owner.Gold, Owner, GoldLossType.Spent);
        if (share <= 0m)
            return;

        MainFile.Logger.Info(
            $"[LegendaryThief] Death gold split: owner={Owner.NetId} players={string.Join(',', players.Select(player => player.NetId))} total={total} share={share} remainder={remainder}");
        Flash();
        foreach (var player in players)
            await PersonMultiplayerEffectHelper.GainGoldDeterministic(share, player);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillSleightOfHand>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private async Task SyncDexterity()
    {
        var creature = Owner?.Creature;
        if (creature == null)
            return;

        var desired = Math.Floor(Math.Max(0m, creature.GetPowerAmount<StarLightPower>()) / 7m);
        var delta = desired - AstralParty_PersonLegendaryThiefAppliedDexterity;
        if (delta == 0m)
            return;
        AstralParty_PersonLegendaryThiefAppliedDexterity = desired;
        await PowerCmd.Apply<DexterityPower>(creature, delta, creature, null, true);
    }
}
