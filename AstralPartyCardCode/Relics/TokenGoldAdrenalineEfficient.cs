using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenGoldAdrenalineEfficient : AdrenalineRelicBase
{
    protected override decimal HalfHpBonus => 3m;
    protected override decimal QuarterHpBonus => 8m;

    public override RelicRarity Rarity => RelicRarity.Rare;
}