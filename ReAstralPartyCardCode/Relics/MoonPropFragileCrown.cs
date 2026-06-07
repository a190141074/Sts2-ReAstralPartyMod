using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropFragileCrown : AstralPartyRelicModel
{
    private const int TriggerPermille = 300;
    private const decimal Act1StarLight = 2m;
    private const decimal Act2StarLight = 4m;
    private const decimal Act3PlusStarLight = 8m;
    private const int DiscountedMerchantCost = 100;

    [SavedProperty] public int AstralParty_MoonPropFragileCrownRollCounter { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override int MerchantCost => DiscountedMerchantCost;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!IsTrackedOutboundDamage(dealer, target, result.TotalDamage, cardSource))
            return;

        var didTrigger = RingOfSevenCursesHelper.RollPermille(
            TriggerPermille,
            MainFile.ModId,
            RelicId,
            nameof(AfterDamageGiven),
            Owner?.RunState?.Rng.StringSeed,
            Owner?.RunState?.CurrentActIndex,
            Owner?.RunState?.TotalFloor,
            Owner?.NetId,
            target.ModelId.ToString(),
            cardSource?.Id.Entry ?? "<none>",
            AstralParty_MoonPropFragileCrownRollCounter++);
        if (!didTrigger || Owner?.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply<StarLightPower>(
            Owner.Creature,
            GetStarLightAmountForCurrentAct(),
            Owner.Creature,
            cardSource,
            false);
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
        if (result.UnblockedDamage <= 0m)
            return;

        var hpBeforeHit = Math.Min(target.MaxHp, target.CurrentHp + result.UnblockedDamage);
        var missingHpBeforeHit = Math.Max(0m, target.MaxHp - hpBeforeHit);
        if (missingHpBeforeHit <= 0m || Owner.Gold <= 0)
            return;

        var goldToLose = Math.Min(missingHpBeforeHit, (decimal)Owner.Gold);
        if (goldToLose <= 0m)
            return;

        Flash();
        await PersonaMultiplayerEffectHelper.LoseGoldDeterministic(goldToLose, Owner, GoldLossType.Spent);
    }

    private bool IsTrackedOutboundDamage(Creature? dealer, Creature target, decimal amount, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (amount <= 0m)
            return false;
        if (target.Side == Owner.Creature.Side)
            return false;

        return dealer == Owner.Creature || (dealer == null && cardSource?.Owner == Owner);
    }

    private decimal GetStarLightAmountForCurrentAct()
    {
        return (Owner?.RunState?.CurrentActIndex ?? 0) switch
        {
            <= 0 => Act1StarLight,
            1 => Act2StarLight,
            _ => Act3PlusStarLight
        };
    }
}
