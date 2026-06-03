using System;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EnigmaticSynthesisEtheriumSetHelper
{
    private const decimal LowHpThresholdRatio = 0.2m;

    public static bool HasSevenCurses(Player? owner)
    {
        return owner?.GetRelic<EnigmaticSevenCurses>() != null;
    }

    public static decimal GetEffectiveAmount(Player? owner, decimal fullAmount)
    {
        if (fullAmount <= 0m)
            return 0m;

        return HasSevenCurses(owner)
            ? fullAmount
            : Math.Floor(fullAmount / 2m);
    }

    public static bool HasFullEquipmentSet(Player? owner)
    {
        return owner?.GetRelic<EnigmaticSynthesisEtheriumHelmet>() != null
               && owner.GetRelic<EnigmaticSynthesisEtheriumCuirass>() != null
               && owner.GetRelic<EnigmaticSynthesisEtheriumGreaves>() != null
               && owner.GetRelic<EnigmaticSynthesisEtheriumBoots>() != null;
    }

    public static bool IsSetEnabled(Player? owner)
    {
        return HasSevenCurses(owner) && HasFullEquipmentSet(owner);
    }

    public static bool IsSetHost(Player? owner, AstralPartyRelicModel? relic)
    {
        if (owner == null || relic == null)
            return false;

        AstralPartyRelicModel? host = owner.GetRelic<EnigmaticSynthesisEtheriumHelmet>();
        host ??= owner.GetRelic<EnigmaticSynthesisEtheriumCuirass>();
        host ??= owner.GetRelic<EnigmaticSynthesisEtheriumGreaves>();
        host ??= owner.GetRelic<EnigmaticSynthesisEtheriumBoots>();
        return ReferenceEquals(host, relic);
    }

    public static bool IsLowHp(Creature? creature)
    {
        return creature != null && creature.CurrentHp < creature.MaxHp * LowHpThresholdRatio;
    }

    public static bool WasLowHpBeforeDamage(Creature? creature, DamageResult result)
    {
        if (creature == null || result.UnblockedDamage <= 0m)
            return false;

        var hpBeforeDamage = creature.CurrentHp + result.UnblockedDamage;
        return hpBeforeDamage < creature.MaxHp * LowHpThresholdRatio;
    }
}
