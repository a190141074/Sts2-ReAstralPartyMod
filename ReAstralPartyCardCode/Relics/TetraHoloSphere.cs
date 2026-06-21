using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;
using ReAstralPartyMod.ReAstralPartyCardCode.RestSite;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class TetraHoloSphere : AstralPartyRelicModel
{
    private static readonly LocString SelectionPrompt =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_TETRA_HOLO_SPHERE.select_prompt");

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (Owner == null || player != Owner)
            return false;
        if (options.Any(static option => option is TetraWarforgeRestSiteOption))
            return false;

        options.Add(new TetraWarforgeRestSiteOption(player));
        return true;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        var owner = Owner;
        if (owner == null)
            return;

        var enchantment = ModelDb.Enchantment<TetraWarforgeEnchantment>();
        var eligibleCards = EventDeckCardHelper.GetRunDeckCards(owner)
            .Where(card => card.Enchantment == null && enchantment.CanEnchant(card))
            .ToList();
        var targetCount = Math.Min(2, eligibleCards.Count);
        if (targetCount <= 0)
            return;

        var selectedCards = await CardSelectCmd.FromDeckForEnchantment(
            owner,
            enchantment,
            targetCount,
            static card => CanSelectWarforgeCard(card),
            new CardSelectorPrefs(SelectionPrompt, targetCount, targetCount)
            {
                Cancelable = false,
                RequireManualConfirmation = true
            });
        var selectedList = selectedCards.ToList();
        if (selectedList.Count != targetCount)
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] Tetra Holo Sphere selection returned unexpected count | owner={owner.NetId} | expected={targetCount} | actual={selectedList.Count}");
            return;
        }

        await EventDeckCardMutationHelper.Enchant<TetraWarforgeEnchantment>(
            owner,
            selectedList,
            "tetra_holo_sphere.after_obtained");
    }

    public bool CanUseWarforgeRestSiteOption(Player player)
    {
        if (Owner == null || player != Owner)
            return false;

        var enchantment = ModelDb.Enchantment<TetraWarforgeEnchantment>();
        return EventDeckCardHelper.GetRunDeckCards(player)
            .Any(card => card.Enchantment == null && enchantment.CanEnchant(card));
    }

    private static bool CanSelectWarforgeCard(CardModel? card)
    {
        return card is { Enchantment: null } && ModelDb.Enchantment<TetraWarforgeEnchantment>().CanEnchant(card);
    }
}
