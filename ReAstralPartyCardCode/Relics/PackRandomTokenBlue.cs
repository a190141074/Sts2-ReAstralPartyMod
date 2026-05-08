using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PackRandomTokenBlue : RandomTokenPackRelicBase
{
    protected override int CommonWeight => 60;
    protected override int UncommonWeight => 30;
    protected override int RareWeight => 10;

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override string IconBasePath => "res://ReAstralPartyMod/images/potion/pack_random_token_blue";
}
