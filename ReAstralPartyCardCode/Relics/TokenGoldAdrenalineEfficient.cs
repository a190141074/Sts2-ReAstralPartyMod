using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldAdrenalineEfficient : AdrenalineRelicBase
{
    protected override decimal HalfHpBonus => 3m;
    protected override decimal QuarterHpBonus => 8m;

    public override RelicRarity Rarity => RelicRarity.Rare;
}
