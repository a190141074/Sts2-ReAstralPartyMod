using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenPurpleAdrenalineGeneral : AdrenalineRelicBase
{
    protected override decimal HalfHpBonus => 1m;
    protected override decimal QuarterHpBonus => 3m;

    public override RelicRarity Rarity => RelicRarity.Uncommon;
}