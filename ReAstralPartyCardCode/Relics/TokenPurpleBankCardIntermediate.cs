using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleBankCardIntermediate : AstralPartyRelicModel
{
    private const int EternalStarlightToGrant = 28;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        TokenEternalStarlight.BuildReferenceHoverTip(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralEternalStarlightSetId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null)
            return;
        if (TokenRelicBridgeInitializationContext.ShouldSkipOneTimeObtainRewards)
            return;

        await TokenEternalStarlight.GrantStacks(Owner, EternalStarlightToGrant);
        Owner.GetRelic<TokenGoldStarCoinHammer>()?.RefreshDisplayedBonusDamage();
    }
}
