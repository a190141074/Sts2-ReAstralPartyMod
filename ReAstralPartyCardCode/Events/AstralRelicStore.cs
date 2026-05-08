using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Underdocks))]
public sealed class AstralRelicStore : AstralPartyEventModel
{
    private const string ImplementationVersion = "2026-05-08-safe-2";
    private const decimal BluePackCost = 50m;
    private const decimal GoldPackCost = 75m;
    private const decimal PurplePackCost = 100m;
    private const decimal InitialPointCost = 1m;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 1
               || runState.Act is Hive
               || runState.Act is Underdocks;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        MainFile.Logger.Info(
            $"AstralRelicStore GenerateInitialOptions | impl={ImplementationVersion} | owner={Owner?.NetId.ToString() ?? "null"} | gold={Owner?.Gold.ToString() ?? "null"}");

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
        var hoverTips = HoverTipFactory.FromRelic(ModelDb.Relic<TRelic>()).ToArray();
        var canAfford = Owner != null && Owner.Gold >= cost;

        MainFile.Logger.Info(
            $"AstralRelicStore CreatePurchaseOption | impl={ImplementationVersion} | relic={typeof(TRelic).Name} | cost={cost} | option={optionKey} | can_afford={canAfford}");

        return new EventOption(this, canAfford ? onChosen : null, InitialOptionKey(optionKey), hoverTips);
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
        var owner = Owner;

        MainFile.Logger.Info(
            $"AstralRelicStore CompletePurchase begin | impl={ImplementationVersion} | owner={owner.NetId} | relic={typeof(TRelic).Name} | gold_before={owner.Gold} | cost={goldCost}");

        if (owner.Gold < goldCost)
        {
            MainFile.Logger.Warn(
                $"AstralRelicStore CompletePurchase insufficient gold | impl={ImplementationVersion} | owner={owner.NetId} | relic={typeof(TRelic).Name} | gold_before={owner.Gold} | cost={goldCost}");
            SetEventState(PageDescription("INITIAL"), GenerateInitialOptions());
            return;
        }

        await PersonaMultiplayerEffectHelper.LoseGoldDeterministic(goldCost, owner, GoldLossType.Spent);
        await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(owner, ModelDb.Relic<TRelic>());
        MainFile.Logger.Info(
            $"AstralRelicStore CompletePurchase success | impl={ImplementationVersion} | owner={owner.NetId} | relic={typeof(TRelic).Name} | gold_after={owner.Gold}");
        SetEventFinished(PageDescription(finishPageName));
    }
}
