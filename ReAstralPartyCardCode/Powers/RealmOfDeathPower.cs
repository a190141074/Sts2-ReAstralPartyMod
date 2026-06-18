using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class RealmOfDeathPower : AstralPartyPowerModel
{
    private const decimal Duration = 2m;
    private const decimal MaxHpRewardPercent = 0.1m;

    [SavedProperty] public string AstralParty_RealmOfDeathMarkedTargetCombatIdRaw { get; set; } = string.Empty;
    public uint AstralParty_RealmOfDeathMarkedTargetCombatId
    {
        get => uint.TryParse(AstralParty_RealmOfDeathMarkedTargetCombatIdRaw, out var value) ? value : 0u;
        set => AstralParty_RealmOfDeathMarkedTargetCombatIdRaw = value.ToString();
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => Math.Max((int)Amount, 0);

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not RealmOfDeathPower)
            return false;
        if (target != Owner || amount <= 0m)
            return false;

        modifiedAmount = Math.Max(Duration - Amount, 0m);
        return true;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (amount <= 0m || dealer == null || target == null)
            return 1m;

        var dealerHasRealm = dealer.GetPower<RealmOfDeathPower>() != null;
        var targetHasRealm = target.GetPower<RealmOfDeathPower>() != null;
        if (dealerHasRealm == targetHasRealm)
            return 1m;

        return 0m;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer != Owner || !result.WasTargetKilled)
            return;
        if (AstralParty_RealmOfDeathMarkedTargetCombatId == 0u
            || target.CombatId != AstralParty_RealmOfDeathMarkedTargetCombatId)
            return;
        if (target.GetPower<RealmOfDeathPower>() == null)
            return;

        var reward = Math.Max(1m, Math.Ceiling(target.MaxHp * MaxHpRewardPercent));
        await CreatureCmd.GainMaxHp(Owner, reward);
        AstralParty_RealmOfDeathMarkedTargetCombatId = 0u;
    }

    public static async Task<RealmOfDeathPower?> ApplyToTarget(
        Creature target,
        Creature? applier,
        CardModel? cardSource)
    {
        if (target == null)
            return null;

        var power = (RealmOfDeathPower)ModelDb.Power<RealmOfDeathPower>().ToMutable();
        await PowerCmd.Apply(power, target, Duration, applier, cardSource, false);
        return target.GetPower<RealmOfDeathPower>();
    }
}
