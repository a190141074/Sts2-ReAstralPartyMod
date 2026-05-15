using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleAdrenalineGeneral : AdrenalineRelicBase
{
    protected override decimal HalfHpBonus => 1m;
    protected override decimal QuarterHpBonus => 3m;

    public override RelicRarity Rarity => RelicRarity.Uncommon;
}
