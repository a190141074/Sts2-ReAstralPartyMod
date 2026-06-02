using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PvzRareSunshineNut : AstralPartyRelicModel
{
    private const decimal DamageThreshold = 30m;
    private decimal _runDamageTaken;

    [SavedProperty]
    private string AstralParty_PvzRareSunshineNutRunDamageTaken
    {
        get => PvzNutRelicHelper.SerializeDecimal(_runDamageTaken);
        set => _runDamageTaken = PvzNutRelicHelper.DeserializeDecimal(value);
    }

    [SavedProperty] public int AstralParty_PvzRareSunshineNutResolvedRewardCount { get; set; }

    protected override string RelicId => "pvz_rare_sunshine_nut";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => PvzNutRelicHelper.GetThresholdProgress(_runDamageTaken, DamageThreshold);

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        _runDamageTaken = 0m;
        AstralParty_PvzRareSunshineNutResolvedRewardCount = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null || !PvzNutRelicHelper.IsOwnedByTarget(target, ownerCreature))
            return;
        if (result.UnblockedDamage <= 0m)
            return;

        var triggered = PvzNutRelicHelper.TryAccumulateRunDamageForThreshold(
            _runDamageTaken,
            AstralParty_PvzRareSunshineNutResolvedRewardCount,
            result.UnblockedDamage,
            DamageThreshold,
            out var nextTotalDamageTaken,
            out var nextResolvedRewardCount,
            out var newlyResolvedCount);
        _runDamageTaken = nextTotalDamageTaken;
        AstralParty_PvzRareSunshineNutResolvedRewardCount = nextResolvedRewardCount;
        InvokeDisplayAmountChanged();

        if (!triggered)
            return;

        for (var i = 0; i < newlyResolvedCount; i++)
        {
            Flash();
            if (Owner != null)
                await PlayerCmd.GainEnergy(1m, Owner);
            MainFile.Logger.Info(
                $"[PvzRareSunshineNut] Granted energy from cumulative damage | owner={Owner?.NetId} | damage={_runDamageTaken} | resolved={AstralParty_PvzRareSunshineNutResolvedRewardCount}");
        }
    }
}
