using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenBlueBankCardGeneral : AstralPartyRelicModel
{
    private const int EternalStarlightToGrant = 14;

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        TokenEternalStarlight.BuildReferenceHoverTip(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralEternalStarlightSet)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null)
            return;

        await TokenEternalStarlight.GrantStacks(Owner, EternalStarlightToGrant);
        Owner.GetRelic<TokenGoldStarCoinHammer>()?.RefreshDisplayedBonusDamage();
    }
}
