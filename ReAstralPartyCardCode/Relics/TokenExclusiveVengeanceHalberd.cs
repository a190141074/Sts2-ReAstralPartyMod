using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusiveVengeanceHalberd : AstralPartyRelicModel
{
    private const decimal StrengthBonus = 5m;
    private const decimal DexterityBonus = 5m;

    private static readonly HashSet<string> DefenseLossPowerIds =
    [
        "FRAIL_POWER",
        "VULNERABLE_POWER",
        "SURROUNDED_POWER",
        "TENDER_POWER",
        "WRAITH_FORM_POWER"
    ];

    private static readonly HashSet<string> HpLossPowerIds =
    [
        "CONSTRICT_POWER",
        "DISINTEGRATION_POWER",
        "DOOM_POWER",
        "POISON_POWER",
        "MAGIC_BOMB_POWER"
    ];

    private decimal _appliedStrengthBonus;
    private decimal _appliedDexterityBonus;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralSpiritFestivalSeriesId)
    ];

    public override async Task BeforeCombatStart()
    {
        await ReevaluateBonuses();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        await ReevaluateBonuses();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await ClearBonuses();
    }

    private async Task ReevaluateBonuses()
    {
        if (Owner?.Creature == null)
            return;

        var desiredStrength = HasDefenseLossDebuff(Owner.Creature) ? StrengthBonus : 0m;
        var desiredDexterity = HasHpLossDebuff(Owner.Creature) ? DexterityBonus : 0m;

        var strengthDelta = desiredStrength - _appliedStrengthBonus;
        if (strengthDelta != 0m)
        {
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, strengthDelta, Owner.Creature, null, true);
            _appliedStrengthBonus = desiredStrength;
        }

        var dexterityDelta = desiredDexterity - _appliedDexterityBonus;
        if (dexterityDelta != 0m)
        {
            await PowerCmd.Apply<DexterityPower>(Owner.Creature, dexterityDelta, Owner.Creature, null, true);
            _appliedDexterityBonus = desiredDexterity;
        }
    }

    private async Task ClearBonuses()
    {
        if (Owner?.Creature == null)
            return;

        if (_appliedStrengthBonus != 0m)
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, -_appliedStrengthBonus, Owner.Creature, null, true);
        if (_appliedDexterityBonus != 0m)
            await PowerCmd.Apply<DexterityPower>(Owner.Creature, -_appliedDexterityBonus, Owner.Creature, null, true);

        _appliedStrengthBonus = 0m;
        _appliedDexterityBonus = 0m;
    }

    private static bool HasDefenseLossDebuff(Creature creature)
    {
        if (creature.GetPowerAmount<DexterityPower>() < 0m)
            return true;

        return GetPowerIds(creature).Any(DefenseLossPowerIds.Contains);
    }

    private static bool HasHpLossDebuff(Creature creature)
    {
        return GetPowerIds(creature).Any(HpLossPowerIds.Contains);
    }

    private static IEnumerable<string> GetPowerIds(Creature creature)
    {
        var powersProperty = creature.GetType().GetProperty("Powers", BindingFlags.Public | BindingFlags.Instance);
        if (powersProperty?.GetValue(creature) is not System.Collections.IEnumerable powers)
            yield break;

        foreach (var power in powers)
        {
            var idValue = power.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(power);
            var entry = idValue?.GetType().GetProperty("Entry", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(idValue)?.ToString();
            if (!string.IsNullOrWhiteSpace(entry))
                yield return entry!;
        }
    }
}
