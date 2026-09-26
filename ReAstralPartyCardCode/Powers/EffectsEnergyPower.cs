using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class EffectsEnergyPower : AstralPartyPowerModel
{
    public const decimal MaxStacks = 10m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => Math.Clamp((int)Amount, 0, (int)MaxStacks);
    public override LocString Title => new("powers", "RE_ASTRAL_PARTY_MOD_POWER_EFFECTS_ENERGY_POWER.title");
    public override LocString Description => new("powers", "RE_ASTRAL_PARTY_MOD_POWER_EFFECTS_ENERGY_POWER.description");

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount,
        Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not EffectsEnergyPower || target != Owner)
            return false;

        modifiedAmount = Math.Clamp(amount, -Math.Max(0m, Amount), Math.Max(0m, MaxStacks - Amount));
        return true;
    }

    public static Task Add(Creature target, decimal amount, Creature? applier = null, CardModel? source = null)
    {
        if (amount == 0m)
            return Task.CompletedTask;
        return PowerCmd.Apply<EffectsEnergyPower>(target, amount, applier ?? target, source, false);
    }

    public static Task Spend(Creature target, decimal amount, Creature? applier = null, CardModel? source = null)
    {
        return Add(target, -Math.Max(0m, amount), applier, source);
    }
}
