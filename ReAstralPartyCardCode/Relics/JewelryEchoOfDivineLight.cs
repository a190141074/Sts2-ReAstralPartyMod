using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class JewelryEchoOfDivineLight : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        SinkouSetHelper.BuildSetDynamicVars(Owner);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        SinkouSetHelper.BuildSetHoverTips(Owner);

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null || SinkouSetHelper.HasVariantSinkou(Owner))
            return;

        SinkouSetHelper.TryAddUpgradedPunitiveJudgmentToDeck(Owner, this);
    }
}
