using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class DreamDisintegrationClaw : AstralPartyRelicModel
{
    private const decimal ActOneBonusDamagePercent = 0.34m;
    private const decimal ActTwoBonusDamagePercent = 0.44m;
    private const decimal ActThreePlusBonusDamagePercent = 0.54m;
    private const decimal ActOneHealPercent = 0.12m;
    private const decimal ActTwoHealPercent = 0.15m;
    private const decimal ActThreePlusHealPercent = 0.18m;
    private const decimal BossMultiplier = 2m;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!IsQualifiedStrikeHit(target, amount, dealer, cardSource))
            return 0m;

        return StableNumericStateHelper.FloorToNonNegativeInt(
            amount * GetScaledPercent(GetCurrentActBonusDamagePercent(), target));
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!IsQualifiedStrikeResult(target, result, dealer, cardSource))
            return;
        if (Owner?.Creature == null)
            return;

        var healAmount = StableNumericStateHelper.FloorToNonNegativeInt(
            result.UnblockedDamage * GetScaledPercent(GetCurrentActHealPercent(), target));
        if (healAmount <= 0)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, healAmount, true);
    }

    private bool IsQualifiedStrikeHit(
        Creature? target,
        decimal amount,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (dealer != Owner.Creature)
            return false;
        if (target == null || target.Side == Owner.Creature.Side)
            return false;
        if (amount <= 0m)
            return false;
        if (cardSource?.Owner != Owner || !WarforgeEnchantmentHelper.CountsAsAttack(cardSource))
            return false;

        return cardSource.Tags.Contains(CardTag.Strike);
    }

    private bool IsQualifiedStrikeResult(
        Creature target,
        DamageResult result,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!IsQualifiedStrikeHit(target, result.TotalDamage, dealer, cardSource))
            return false;

        return result.UnblockedDamage > 0m;
    }

    private decimal GetCurrentActBonusDamagePercent()
    {
        var actNumber = Math.Max((Owner?.RunState?.CurrentActIndex ?? 0) + 1, 1);
        return actNumber switch
        {
            <= 1 => ActOneBonusDamagePercent,
            2 => ActTwoBonusDamagePercent,
            _ => ActThreePlusBonusDamagePercent
        };
    }

    private decimal GetCurrentActHealPercent()
    {
        var actNumber = Math.Max((Owner?.RunState?.CurrentActIndex ?? 0) + 1, 1);
        return actNumber switch
        {
            <= 1 => ActOneHealPercent,
            2 => ActTwoHealPercent,
            _ => ActThreePlusHealPercent
        };
    }

    private decimal GetScaledPercent(decimal percent, Creature? target)
    {
        if (target?.CombatState?.Encounter?.RoomType == RoomType.Boss)
            percent *= BossMultiplier;

        return percent;
    }
}
