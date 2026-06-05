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
public class PvzUltimateSunshineEmperorNut : AstralPartyRelicModel
{
    private const decimal DamageThreshold = 20m;
    private decimal _runDamageTaken;

    [SavedProperty]
    private string AstralParty_PvzUltimateSunshineEmperorNutRunDamageTaken
    {
        get => StableNumericStateHelper.SerializeDecimal(_runDamageTaken);
        set => _runDamageTaken = StableNumericStateHelper.DeserializeDecimal(value);
    }

    [SavedProperty] public int AstralParty_PvzUltimateSunshineEmperorNutResolvedRewardCount { get; set; }

    protected override string RelicId => "pvz_ultimate_sunshine_emperor_nut";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => PvzNutRelicHelper.GetThresholdProgress(_runDamageTaken, DamageThreshold);

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        _runDamageTaken = 0m;
        AstralParty_PvzUltimateSunshineEmperorNutResolvedRewardCount = 0;
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
            AstralParty_PvzUltimateSunshineEmperorNutResolvedRewardCount,
            result.UnblockedDamage,
            DamageThreshold,
            out var nextTotalDamageTaken,
            out var nextResolvedRewardCount,
            out var newlyResolvedCount);
        _runDamageTaken = nextTotalDamageTaken;
        AstralParty_PvzUltimateSunshineEmperorNutResolvedRewardCount = nextResolvedRewardCount;
        InvokeDisplayAmountChanged();

        if (!triggered)
            return;

        for (var i = 0; i < newlyResolvedCount; i++)
        {
            Flash();
            if (Owner != null)
                await PlayerCmd.GainEnergy(1m, Owner);
            MainFile.Logger.Info(
                $"[PvzUltimateSunshineEmperorNut] Granted energy from cumulative damage | owner={Owner?.NetId} | damage={_runDamageTaken} | resolved={AstralParty_PvzUltimateSunshineEmperorNutResolvedRewardCount}");
        }
    }
}
