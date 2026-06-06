using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using System.Linq;

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

        var recipeOptions = EnigmaticSynthesisRestSiteHelper.GetEligibleRecipesForSelection(Owner);
        if (recipeOptions.Count == 0)
        {
            RefreshEnabled();
            return false;
        }

        var rewardOptions = recipeOptions
            .Select(static recipe => recipe.Result.DisplayRelic)
            .ToList();
        var selectedRelic = await DeterministicMultiplayerChoiceHelper.SelectRelicForPlayer(
            Owner,
            rewardOptions,
            $"{OptionKey}.selection");
        if (selectedRelic == null)
        {
            RefreshEnabled();
            return false;
        }

        var selectedIndex = rewardOptions.FindIndex(relic =>
            (relic.CanonicalInstance ?? relic).Id == (selectedRelic.CanonicalInstance ?? selectedRelic).Id);
        if (selectedIndex < 0 || selectedIndex >= recipeOptions.Count)
        {
            RefreshEnabled();
            return false;
        }

        var didCraft = await EnigmaticSynthesisRestSiteHelper.TryCraftAsync(Owner, recipeOptions[selectedIndex]);
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
