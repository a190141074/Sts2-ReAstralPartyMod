using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public sealed class TetraWarforgeRestSiteOption : AstralPartyRestSiteOptionModel
{
    private static readonly LocString SelectionPrompt =
        new("rest_site_ui", $"OPTION_{OptionKey}.selection_prompt");

    public static string OptionKey => GetOptionId<TetraWarforgeRestSiteOption>();

    protected override string OptionName => "TetraWarforge";

    public TetraWarforgeRestSiteOption(Player owner) : base(owner)
    {
    }

    public override bool IsEnabled => ResolveRelic()?.CanUseWarforgeRestSiteOption(Owner) ?? false;

    public override LocString Description => RestSiteUiLoc("description");

    public override async Task<bool> OnSelect()
    {
        var relic = ResolveRelic();
        if (relic == null || !relic.CanUseWarforgeRestSiteOption(Owner))
            return false;

        var enchantment = ModelDb.Enchantment<TetraWarforgeEnchantment>();
        var selectedCards = await CardSelectCmd.FromDeckForEnchantment(
            Owner,
            enchantment,
            1,
            card => card.Enchantment == null && enchantment.CanEnchant(card),
            new CardSelectorPrefs(SelectionPrompt, 1, 1)
            {
                Cancelable = true,
                RequireManualConfirmation = true
            });
        var selectedList = selectedCards.ToList();
        if (selectedList.Count != 1)
            return false;

        await EventDeckCardMutationHelper.Enchant<TetraWarforgeEnchantment>(
            Owner,
            selectedList,
            "tetra_warforge.rest_site");
        relic.Flash();
        return true;
    }

    private TetraHoloSphere? ResolveRelic()
    {
        return Owner.GetRelic<TetraHoloSphere>();
    }
}
