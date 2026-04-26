using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenBlueSpeedRollerGeneral : SpeedRollerRelicBase
{
    protected override decimal CombatStartDexterityBonus => 1m;

    public override RelicRarity Rarity => RelicRarity.Common;
}