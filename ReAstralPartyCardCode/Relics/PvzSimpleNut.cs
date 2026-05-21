using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class PvzSimpleNut : AstralPartyRelicModel
{
    private const decimal DamageThreshold = 80m;

    [SavedProperty] public bool AstralParty_PvzSimpleNutGrantedThisRun { get; set; }
    [SavedProperty] public decimal AstralParty_PvzSimpleNutRunDamageTaken { get; set; }
    [SavedProperty] public bool AstralParty_PvzSimpleNutReachedThresholdThisRun { get; set; }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PvzSimpleNutGrantedThisRun = false;
        AstralParty_PvzSimpleNutRunDamageTaken = 0m;
        AstralParty_PvzSimpleNutReachedThresholdThisRun = false;
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

        AstralParty_PvzSimpleNutRunDamageTaken += result.UnblockedDamage;
        if (!AstralParty_PvzSimpleNutReachedThresholdThisRun &&
            AstralParty_PvzSimpleNutRunDamageTaken >= DamageThreshold)
        {
            AstralParty_PvzSimpleNutReachedThresholdThisRun = true;
            Flash();
            MainFile.Logger.Info(
                $"[PvzSimpleNut] Run damage threshold reached | owner={Owner?.NetId} | damage={AstralParty_PvzSimpleNutRunDamageTaken}");
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null)
            return Task.CompletedTask;

        if (!AstralParty_PvzSimpleNutGrantedThisRun && AstralParty_PvzSimpleNutReachedThresholdThisRun)
        {
            AstralParty_PvzSimpleNutGrantedThisRun = true;

            if (Owner.GetRelic<PvzHyperTemporalNut>() == null)
            {
                room.AddExtraReward(Owner, new RelicReward(ModelDb.Relic<PvzHyperTemporalNut>().ToMutable(), Owner));
                MainFile.Logger.Info($"[PvzSimpleNut] Added Hyper Temporal Nut reward | owner={Owner.NetId}");
            }
            else
            {
                MainFile.Logger.Info($"[PvzSimpleNut] Threshold reached but owner already has Hyper Temporal Nut | owner={Owner.NetId}");
            }
        }
        return Task.CompletedTask;
    }
}
