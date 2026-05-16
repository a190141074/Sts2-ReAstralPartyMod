using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Underdocks))]
public sealed class AstralRelicStore : AstralPartyEventModel
{
    private const decimal MinimumGoldRequiredPerPlayer = 150m;
    private const decimal BluePackCost = 50m;
    private const decimal PurplePackCost = 75m;
    private const decimal GoldPackCost = 100m;
    private const decimal InitialPointCost = 1m;
    private static readonly HashSet<string> ConsumedActs = [];

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 1
               && !HasBeenConsumedThisAct(runState)
               && runState.Players.All(player => player.Gold >= MinimumGoldRequiredPerPlayer);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        MarkConsumedForCurrentAct();
        MainFile.Logger.Info(
            $"AstralRelicStore GenerateInitialOptions | owner={Owner?.NetId.ToString() ?? "null"} | gold={Owner?.Gold.ToString() ?? "null"}");

        return [new EventOption(this, OpenStorefront, InitialOptionKey("FOUND_ONE_GOLD"))];
    }

    private async Task OpenStorefront()
    {
        ArgumentNullException.ThrowIfNull(Owner);
        var owner = Owner;

        MainFile.Logger.Info(
            $"AstralRelicStore OpenStorefront begin | owner={owner.NetId} | gold_before={owner.Gold}");

        await PersonaMultiplayerEffectHelper.GainGoldDeterministic(1m, owner);

        MainFile.Logger.Info(
            $"AstralRelicStore OpenStorefront success | owner={owner.NetId} | gold_after={owner.Gold}");

        SetEventState(PageDescription("STOREFRONT"), CreateStorefrontOptions());
    }

    private IReadOnlyList<EventOption> CreateStorefrontOptions()
    {
        return
        [
            CreatePurchaseOption<PackRandomTokenBlue>(BluePackCost, BuyBluePack, "BLUE_PACK"),
            CreatePurchaseOption<PackRandomTokenPurple>(PurplePackCost, BuyPurplePack, "PURPLE_PACK"),
            CreatePurchaseOption<PackRandomTokenGold>(GoldPackCost, BuyGoldPack, "GOLD_PACK"),
            CreatePurchaseOption<TokenGoldInitialPoint>(InitialPointCost, BuyInitialPoint, "INITIAL_POINT")
        ];
    }

    private EventOption CreatePurchaseOption<TRelic>(decimal cost, Func<Task> onChosen, string optionKey)
        where TRelic : RelicModel
    {
        var canAfford = Owner != null && Owner.Gold >= cost;

        MainFile.Logger.Info(
            $"AstralRelicStore CreatePurchaseOption | relic={typeof(TRelic).Name} | cost={cost} | option={optionKey} | can_afford={canAfford}");

        return new EventOption(this, canAfford ? onChosen : null, ModOptionKey("STOREFRONT", optionKey));
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
            $"AstralRelicStore CompletePurchase begin | owner={owner.NetId} | relic={typeof(TRelic).Name} | gold_before={owner.Gold} | cost={goldCost}");

        if (owner.Gold < goldCost)
        {
            MainFile.Logger.Warn(
                $"AstralRelicStore CompletePurchase insufficient gold | owner={owner.NetId} | relic={typeof(TRelic).Name} | gold_before={owner.Gold} | cost={goldCost}");
            SetEventState(PageDescription("STOREFRONT"), CreateStorefrontOptions());
            return;
        }

        await PersonaMultiplayerEffectHelper.LoseGoldDeterministic(goldCost, owner, GoldLossType.Spent);
        await GrantStorePurchase(owner, ModelDb.Relic<TRelic>());
        MainFile.Logger.Info(
            $"AstralRelicStore CompletePurchase success | owner={owner.NetId} | relic={typeof(TRelic).Name} | gold_after={owner.Gold}");
        SetEventFinished(PageDescription(finishPageName));
    }

    private static async Task GrantStorePurchase(Player owner, RelicModel relic)
    {
        var canonicalRelic = relic.CanonicalInstance ?? relic;
        if (canonicalRelic.Id == ModelDb.GetId<TokenGoldInitialPoint>())
        {
            if (owner.GetRelic<TokenGoldInitialPoint>() != null)
                await ObtainDuplicateInitialPointFallbackAsReward(owner);
            else
                await RewardSyncHelper.ObtainRelicAsRewardMultiplayerSafe(owner, canonicalRelic);

            return;
        }

        await RewardsCmd.OfferCustom(owner, [new RelicReward(canonicalRelic.ToMutable(), owner)]);
    }

    private static async Task<RelicModel> ObtainDuplicateInitialPointFallbackAsReward(Player owner)
    {
        var eternalStarlight = owner.GetRelic<TokenEternalStarlight>();
        if (eternalStarlight == null)
            eternalStarlight = await RewardSyncHelper.ObtainRelicAsRewardMultiplayerSafe(owner, ModelDb.Relic<TokenEternalStarlight>())
                                   as TokenEternalStarlight
                               ?? owner.GetRelic<TokenEternalStarlight>();

        if (eternalStarlight != null)
        {
            await CardGainAttribution.RunWithSource(null, () =>
            {
                eternalStarlight.AddStacks(3);
                return Task.CompletedTask;
            });
            return eternalStarlight;
        }

        return owner.GetRelic<TokenGoldInitialPoint>()!;
    }

    internal static bool HasBeenConsumedThisAct(IRunState? runState)
    {
        if (runState == null)
            return false;

        return ConsumedActs.Contains(GetCurrentActConsumptionKey(runState));
    }

    internal static void MarkConsumedForCurrentAct(IRunState? runState)
    {
        if (runState == null)
            return;

        ConsumedActs.Add(GetCurrentActConsumptionKey(runState));
    }

    private void MarkConsumedForCurrentAct()
    {
        MarkConsumedForCurrentAct(Owner?.RunState);
    }

    private static string GetCurrentActConsumptionKey(IRunState runState)
    {
        return $"{runState.Rng.StringSeed}|act:{runState.CurrentActIndex}";
    }
}
