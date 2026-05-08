using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Underdocks))]
public sealed class AstralRelicStore : ModEventTemplate
{
    private const decimal BluePackCost = 50m;
    private const decimal GoldPackCost = 75m;
    private const decimal PurplePackCost = 100m;
    private const decimal InitialPointCost = 1m;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://ReAstralPartyMod/images/events/astral_relic_store.png");

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 1
               || runState.Act is Hive
               || runState.Act is Underdocks;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            CreatePurchaseOption<PackRandomTokenBlue>(BluePackCost, BuyBluePack, "BLUE_PACK"),
            CreatePurchaseOption<PackRandomTokenGold>(GoldPackCost, BuyGoldPack, "GOLD_PACK"),
            CreatePurchaseOption<PackRandomTokenPurple>(PurplePackCost, BuyPurplePack, "PURPLE_PACK"),
            CreatePurchaseOption<TokenGoldInitialPoint>(InitialPointCost, BuyInitialPoint, "INITIAL_POINT")
        ];
    }

    private EventOption CreatePurchaseOption<TRelic>(decimal cost, Func<Task> onChosen, string optionKey)
        where TRelic : RelicModel
    {
        var canAfford = Owner?.Gold >= cost;
        return new EventOption(this, canAfford ? onChosen : null, InitialOptionKey(optionKey))
            .WithRelic<TRelic>(Owner);
    }

    private Task BuyBluePack()
    {
        return CompletePurchase<PackRandomTokenBlue>(BluePackCost, "BLUE_PACK_FINISH");
    }

    private Task BuyGoldPack()
    {
        return CompletePurchase<PackRandomTokenGold>(GoldPackCost, "GOLD_PACK_FINISH");
    }

    private Task BuyPurplePack()
    {
        return CompletePurchase<PackRandomTokenPurple>(PurplePackCost, "PURPLE_PACK_FINISH");
    }

    private Task BuyInitialPoint()
    {
        return CompletePurchase<TokenGoldInitialPoint>(InitialPointCost, "INITIAL_POINT_FINISH");
    }

    private async Task CompletePurchase<TRelic>(decimal goldCost, string finishPageName)
        where TRelic : RelicModel
    {
        ArgumentNullException.ThrowIfNull(Owner);

        if (Owner.Gold < goldCost)
        {
            SetEventState(PageDescription("INITIAL"), GenerateInitialOptions());
            return;
        }

        await PersonaMultiplayerEffectHelper.LoseGoldDeterministic(goldCost, Owner, GoldLossType.Spent);
        await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(Owner, ModelDb.Relic<TRelic>());
        SetEventFinished(PageDescription(finishPageName));
    }
}
