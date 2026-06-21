using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class EnigmaticSynthesisCursedScroll : AstralPartyRelicModel
{
    protected override string RelicId => "enigmatic_synthesis_cursed_scroll";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => CursedScrollDeckHelper.GetWeightedCurseCount(Owner);

    internal void RefreshCounter()
    {
        InvokeDisplayAmountChanged();
    }

    internal static void RefreshCounterForOwner(Player? owner)
    {
        owner?.GetRelic<EnigmaticSynthesisCursedScroll>()?.RefreshCounter();
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        RefreshCounter();
    }

    public override Task BeforeCombatStart()
    {
        RefreshCounter();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        RefreshCounter();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        RefreshCounter();

        var extraDraw = CursedScrollDeckHelper.GetExtraDrawCount(
            CursedScrollDeckHelper.GetWeightedCurseCount(Owner));
        if (extraDraw <= 0)
            return;

        await PersonMultiplayerEffectHelper.DrawCardsForPlayer(choiceContext, extraDraw, Owner!, this);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (cardSource?.Owner != Owner || cardSource.Type != CardType.Attack)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;

        return CursedScrollDeckHelper.GetAttackDamageBonus(
            amount,
            CursedScrollDeckHelper.GetWeightedCurseCount(Owner));
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return 1m;

        return CursedScrollDeckHelper.GetDamageTakenMultiplier(
            CursedScrollDeckHelper.GetWeightedCurseCount(Owner));
    }

    public static decimal AdjustGoldGainAmount(Player? player, decimal amount)
    {
        if (player?.GetRelic<EnigmaticSynthesisCursedScroll>() == null)
            return amount;

        var weightedCurseCount = CursedScrollDeckHelper.GetWeightedCurseCount(player);
        return amount + CursedScrollDeckHelper.GetGoldGainBonus(amount, weightedCurseCount);
    }

    public static decimal AdjustHealAmount(Creature? creature, decimal amount)
    {
        var player = creature?.Player;
        if (player == null || player.Creature != creature || player.GetRelic<EnigmaticSynthesisCursedScroll>() == null)
            return amount;

        var weightedCurseCount = CursedScrollDeckHelper.GetWeightedCurseCount(player);
        return amount + CursedScrollDeckHelper.GetHealGainBonus(amount, weightedCurseCount);
    }
}
