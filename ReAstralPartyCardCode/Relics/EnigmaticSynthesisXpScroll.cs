using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class EnigmaticSynthesisXpScroll : AstralPartyRelicModel
{
    private const int BaseSkippedSmithBonus = 1;
    private const int SevenCursesSkippedSmithBonus = 2;

    [SavedProperty] public int AstralParty_EnigmaticSynthesisXpScrollPendingSmithBonus { get; set; }

    protected override string RelicId => "enigmatic_synthesis_xp_scroll";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(0, AstralParty_EnigmaticSynthesisXpScrollPendingSmithBonus);

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
            return false;

        var pendingBonus = DisplayAmount;
        if (pendingBonus <= 0)
            return false;

        var smithOption = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smithOption == null)
            return false;

        smithOption.SmithCount += pendingBonus;
        return true;
    }

    internal void OnRestSiteOptionResolved(RestSiteOption option)
    {
        if (option is SmithRestSiteOption)
        {
            ClearPendingSmithBonus();
            return;
        }

        AstralParty_EnigmaticSynthesisXpScrollPendingSmithBonus += GetSkippedSmithBonusGain();
        InvokeDisplayAmountChanged();
        Flash();
    }

    private void ClearPendingSmithBonus()
    {
        if (AstralParty_EnigmaticSynthesisXpScrollPendingSmithBonus <= 0)
            return;

        AstralParty_EnigmaticSynthesisXpScrollPendingSmithBonus = 0;
        InvokeDisplayAmountChanged();
        Flash();
    }

    private int GetSkippedSmithBonusGain()
    {
        return Owner?.GetRelic<EnigmaticSevenCurses>() != null
            ? SevenCursesSkippedSmithBonus
            : BaseSkippedSmithBonus;
    }
}
