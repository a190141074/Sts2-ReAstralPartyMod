using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PackRandomTokenGold : RandomTokenPackRelicBase
{
    protected override int CommonWeight => 40;
    protected override int UncommonWeight => 40;
    protected override int RareWeight => 20;

    public override RelicRarity Rarity => RelicRarity.Uncommon;
    
}
