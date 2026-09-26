using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class EnergyChargeHelper
{
    public static int Get(Creature? creature) => creature == null ? 0 : (int)creature.GetPowerAmount<EnergyPower>();

    public static bool Has(Creature? creature, int amount) => Get(creature) >= amount;

    public static Task Add(Creature creature, int amount, Creature? applier = null, CardModel? source = null) =>
        EnergyPower.Add(creature, amount, applier, source);

    public static Task Spend(Creature creature, int amount, Creature? applier = null, CardModel? source = null) =>
        EnergyPower.Spend(creature, amount, applier, source);
}
