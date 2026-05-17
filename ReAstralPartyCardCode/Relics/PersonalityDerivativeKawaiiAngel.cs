using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeKawaiiAngel : AstralPartyRelicModel
{
    public const int MaxTriggersPerCombat = 3;

    [SavedProperty]
    public int AstralParty_PersonalityDerivativeKawaiiAngelRemainingTriggers { get; set; } =
        MaxTriggersPerCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeKawaiiAngelRemainingTriggers;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillCyberAngel>(),
        HoverTipFactory.FromPower<FanPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ResetCombatTriggers();
    }

    public override Task BeforeCombatStart()
    {
        ResetCombatTriggers();
        return Task.CompletedTask;
    }

    public bool TryConsumeTrigger()
    {
        if (AstralParty_PersonalityDerivativeKawaiiAngelRemainingTriggers <= 0)
            return false;

        AstralParty_PersonalityDerivativeKawaiiAngelRemainingTriggers--;
        Flash();
        InvokeDisplayAmountChanged();
        return true;
    }

    private void ResetCombatTriggers()
    {
        AstralParty_PersonalityDerivativeKawaiiAngelRemainingTriggers = MaxTriggersPerCombat;
        InvokeDisplayAmountChanged();
    }
}
