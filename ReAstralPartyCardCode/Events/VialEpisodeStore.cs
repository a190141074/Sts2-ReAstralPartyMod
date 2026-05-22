using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Potions;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Glory))]
public sealed class VialEpisodeStore : AstralPartyEventModel
{
    private const decimal MinimumGoldRequiredPerPlayer = 150m;
    private const decimal AnomalyPotionCost = 1m;
    private const decimal NeutralPotionCost = 15m;
    private const decimal GoodPotionCost = 37m;
    private const decimal DeusPotionCost = 89m;

    protected override string EventId => "vial_episode_store";

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(player => player.Gold >= MinimumGoldRequiredPerPlayer);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [new EventOption(this, OpenStorefront, InitialOptionKey("FOUND_ONE_GOLD"))];
    }

    private async Task OpenStorefront()
    {
        ArgumentNullException.ThrowIfNull(Owner);
        await PersonaMultiplayerEffectHelper.GainGoldDeterministic(1m, Owner);
        SetEventState(PageDescription("STOREFRONT"), CreateStorefrontOptions());
    }

    private IReadOnlyList<EventOption> CreateStorefrontOptions()
    {
        return
        [
            CreatePurchaseOption<VialAnomalyEventPotion>(AnomalyPotionCost, BuyAnomalyPotion, "ANOMALY"),
            CreatePurchaseOption<VialNeutralEventPotion>(NeutralPotionCost, BuyNeutralPotion, "NEUTRAL"),
            CreatePurchaseOption<VialGoodEventPotion>(GoodPotionCost, BuyGoodPotion, "GOOD"),
            CreatePurchaseOption<VialDeusExMachinaPotion>(DeusPotionCost, BuyDeusPotion, "DEUS_EX_MACHINA")
        ];
    }

    private EventOption CreatePurchaseOption<TPotion>(decimal cost, Func<Task> onChosen, string optionKey)
        where TPotion : PotionModel
    {
        var canAfford = Owner != null && Owner.Gold >= cost;
        return new EventOption(this, canAfford ? onChosen : null, ModOptionKey("STOREFRONT", optionKey));
    }

    private Task BuyAnomalyPotion()
    {
        return CompletePurchase<VialAnomalyEventPotion>(AnomalyPotionCost, "ANOMALY_FINISH");
    }

    private Task BuyNeutralPotion()
    {
        return CompletePurchase<VialNeutralEventPotion>(NeutralPotionCost, "NEUTRAL_FINISH");
    }

    private Task BuyGoodPotion()
    {
        return CompletePurchase<VialGoodEventPotion>(GoodPotionCost, "GOOD_FINISH");
    }

    private Task BuyDeusPotion()
    {
        return CompletePurchase<VialDeusExMachinaPotion>(DeusPotionCost, "DEUS_EX_MACHINA_FINISH");
    }

    private async Task CompletePurchase<TPotion>(decimal goldCost, string finishPageName)
        where TPotion : PotionModel
    {
        ArgumentNullException.ThrowIfNull(Owner);
        if (Owner.Gold < goldCost)
        {
            SetEventState(PageDescription("STOREFRONT"), CreateStorefrontOptions());
            return;
        }

        await PersonaMultiplayerEffectHelper.LoseGoldDeterministic(goldCost, Owner, GoldLossType.Spent);
        var potionReward = new PotionReward(ModelDb.Potion<TPotion>().ToMutable(), Owner);
        await RewardsCmd.OfferCustom(Owner, [potionReward]);
        SetEventFinished(PageDescription(finishPageName));
    }
}
