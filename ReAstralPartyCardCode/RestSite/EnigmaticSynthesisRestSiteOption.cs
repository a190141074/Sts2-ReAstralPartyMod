using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public class EnigmaticSynthesisRestSiteOption : AstralPartyRestSiteOptionModel
{
    public static string OptionKey => GetOptionId<EnigmaticSynthesisRestSiteOption>();

    public EnigmaticSynthesisRestSiteOption(Player owner) : base(owner)
    {
        RefreshEnabled();
    }

    public override LocString Description => RestSiteUiLoc(IsEnabled ? "description_enabled" : "description_disabled");

    public override async Task<bool> OnSelect()
    {
        RefreshEnabled();
        if (!IsEnabled)
            return false;

        var rewardOptions = EnigmaticSynthesisRestSiteHelper.GetEligibleRelics(Owner);
        if (rewardOptions.Count == 0)
        {
            RefreshEnabled();
            return false;
        }

        var selectionTitle = RestSiteUiLoc("selection_header").GetRawText();
        var selectedRelic = await DeterministicMultiplayerChoiceHelper.SelectRelicForPlayer(
            Owner,
            rewardOptions,
            $"{OptionKey}.selection");
        if (selectedRelic == null)
        {
            RefreshEnabled();
            return false;
        }

        var didCraft = await EnigmaticSynthesisRestSiteHelper.TryCraftAsync(Owner, selectedRelic);
        if (didCraft)
            Owner.GetRelic<EnigmaticSevenBlessings>()?.Flash();

        RefreshEnabled();
        return false;
    }

    private void RefreshEnabled()
    {
        IsEnabled = EnigmaticSynthesisRestSiteHelper.CanUse(Owner);
    }
}
