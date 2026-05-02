using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenBlueSpeedRollerGeneral : SpeedRollerRelicBase
{
    protected override decimal CombatStartDexterityBonus => 1m;

    public override RelicRarity Rarity => RelicRarity.Common;
}