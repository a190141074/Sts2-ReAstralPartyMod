using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class ChargeTokenRegistry
{
    private static readonly HashSet<ModelId> Ids =
    [
        ModelDb.GetId<TokenBlueEnergyRecycler>(), ModelDb.GetId<TokenBlueElectricBlade>(),
        ModelDb.GetId<TokenPurpleAirBag>(), ModelDb.GetId<TokenPurpleShockGloves>(),
        ModelDb.GetId<TokenBlueWarpEngine>(), ModelDb.GetId<TokenPurpleElectricCore>(),
        ModelDb.GetId<TokenGoldPerpetualMotionMachine>(), ModelDb.GetId<TokenGoldElectromagneticGun>()
    ];

    public static bool IsChargeToken(RelicModel relic) => Ids.Contains(relic.CanonicalInstance?.Id ?? relic.Id);
}
