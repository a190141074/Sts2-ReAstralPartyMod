using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PackRandomTokenPurple : RandomTokenPackRelicBase
{
    protected override int CommonWeight => 20;
    protected override int UncommonWeight => 50;
    protected override int RareWeight => 30;

    public override RelicRarity Rarity => RelicRarity.Uncommon;
    
}
