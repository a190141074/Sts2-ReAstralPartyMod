using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropFragileCrown : MoonPropStackableRelicBase
{
    private const int TriggerPermille = 300;
    private const decimal Act1StarLight = 2m;
    private const decimal Act2StarLight = 4m;
    private const decimal Act3PlusStarLight = 8m;

    [SavedProperty] public int AstralParty_MoonPropFragileCrownRollCounter { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("ActRewards", GetActRewardsText()),
        new StringVar("GoldLossPercent", GetGoldLossPercentText())
    ];

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
            GetStarLightAmountForCurrentAct() * GetStacks(),
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

        var goldToLose = Math.Min(missingHpBeforeHit * GetStacks(), (decimal)Owner.Gold);
        if (goldToLose <= 0m)
            return;

        Flash();
        await PersonMultiplayerEffectHelper.LoseGoldDeterministic(goldToLose, Owner, GoldLossType.Spent);
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

    private string GetActRewardsText()
    {
        var stacks = GetStacks();
        return $"{FormatValue(Act1StarLight * stacks)}/{FormatValue(Act2StarLight * stacks)}/{FormatValue(Act3PlusStarLight * stacks)}";
    }

    private string GetGoldLossPercentText()
    {
        return FormatPercent(GetStacks());
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("ActRewards", GetActRewardsText());
        SetDynamicString("GoldLossPercent", GetGoldLossPercentText());
    }
}
