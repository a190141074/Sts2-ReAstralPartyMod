using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class EnergyChargeHelper
{
    public static int Get(Creature? creature)
    {
        return creature == null ? 0 : (int)creature.GetPowerAmount<EffectsEnergyPower>();
    }

    public static bool Has(Creature? creature, int amount)
    {
        return Get(creature) >= amount;
    }

    public static Task Add(Creature creature, int amount, Creature? applier = null, CardModel? source = null)
    {
        return EffectsEnergyPower.Add(creature, amount, applier, source);
    }

    public static Task Add(PlayerChoiceContext context, Creature creature, int amount,
        Creature? applier = null, CardModel? source = null)
    {
        return EffectsEnergyPower.Add(context, creature, amount, applier, source);
    }

    public static Task Spend(Creature creature, int amount, Creature? applier = null, CardModel? source = null)
    {
        return EffectsEnergyPower.Spend(creature, amount, applier, source);
    }

    public static Task Spend(PlayerChoiceContext context, Creature creature, int amount,
        Creature? applier = null, CardModel? source = null)
    {
        return EffectsEnergyPower.Spend(context, creature, amount, applier, source);
    }
}
