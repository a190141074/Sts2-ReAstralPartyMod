using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeNeedyGirl : AstralPartyRelicModel
{
    public const int BaseLoveCap = 4;

    [SavedProperty] public int AstralParty_PersonalityDerivativeNeedyGirlGrowthCount { get; set; }
    [SavedProperty] public int AstralParty_PersonalityDerivativeNeedyGirlBonusLoveCap { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeNeedyGirlGrowthCount;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillEmotionalOverdose>(),
        HoverTipFactory.FromPower<LovePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeNeedyGirlGrowthCount =
            Math.Max(0, AstralParty_PersonalityDerivativeNeedyGirlGrowthCount);
        AstralParty_PersonalityDerivativeNeedyGirlBonusLoveCap =
            Math.Max(0, AstralParty_PersonalityDerivativeNeedyGirlBonusLoveCap);
        InvokeDisplayAmountChanged();
    }

    public int GetLoveCap()
    {
        return BaseLoveCap + Math.Max(0, AstralParty_PersonalityDerivativeNeedyGirlBonusLoveCap);
    }

    public async Task GainPermanentGrowth(Creature owner)
    {
        if (owner == null)
            return;

        AstralParty_PersonalityDerivativeNeedyGirlGrowthCount++;
        AstralParty_PersonalityDerivativeNeedyGirlBonusLoveCap++;
        Flash();
        InvokeDisplayAmountChanged();
        await CreatureCmd.GainMaxHp(owner, 1m);
    }
}
