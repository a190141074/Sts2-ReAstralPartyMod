using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class PvzSimpleNut : AstralPartyRelicModel
{
    private const decimal DamageThreshold = 80m;
    private decimal _runDamageTaken;

    [SavedProperty] public bool AstralParty_PvzSimpleNutGrantedThisRun { get; set; }
    [SavedProperty] public bool AstralParty_PvzSimpleNutReachedThresholdThisRun { get; set; }
    [SavedProperty]
    private string AstralParty_PvzSimpleNutRunDamageTaken
    {
        get => PvzNutRelicHelper.SerializeDecimal(_runDamageTaken);
        set => _runDamageTaken = PvzNutRelicHelper.DeserializeDecimal(value);
    }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(0, (int)decimal.Floor(_runDamageTaken));

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PvzSimpleNutGrantedThisRun = false;
        _runDamageTaken = 0m;
        AstralParty_PvzSimpleNutReachedThresholdThisRun = false;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null)
            return Task.CompletedTask;
        if (!PvzNutRelicHelper.IsOwnedByTarget(target, ownerCreature))
            return Task.CompletedTask;
        if (result.UnblockedDamage <= 0m)
            return Task.CompletedTask;

        _runDamageTaken += result.UnblockedDamage;
        InvokeDisplayAmountChanged();
        if (!AstralParty_PvzSimpleNutReachedThresholdThisRun &&
            _runDamageTaken >= DamageThreshold)
        {
            AstralParty_PvzSimpleNutReachedThresholdThisRun = true;
            Flash();
            MainFile.Logger.Info(
                $"[PvzSimpleNut] Run damage threshold reached | owner={Owner?.NetId} | damage={_runDamageTaken}");
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null)
            return;

        if (!AstralParty_PvzSimpleNutGrantedThisRun && AstralParty_PvzSimpleNutReachedThresholdThisRun)
        {
            AstralParty_PvzSimpleNutGrantedThisRun = true;

            if (Owner.GetRelic<PvzHyperTemporalNut>() == null)
            {
                await RewardSyncHelper.ObtainRelicAsRewardMultiplayerSafe(Owner, ModelDb.Relic<PvzHyperTemporalNut>());
                MainFile.Logger.Info($"[PvzSimpleNut] Granted Hyper Temporal Nut via safe reward path | owner={Owner.NetId}");
            }
            else
            {
                MainFile.Logger.Info($"[PvzSimpleNut] Threshold reached but owner already has Hyper Temporal Nut | owner={Owner.NetId}");
            }
        }
    }
}
