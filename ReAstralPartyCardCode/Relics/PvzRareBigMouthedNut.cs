using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PvzRareBigMouthedNut : AstralPartyRelicModel
{
    private const int CooldownRounds = 40;
    private const decimal HealAmount = 20m;

    [SavedProperty] public int AstralParty_PvzRareBigMouthedNutLastTriggeredRound { get; set; }

    protected override string RelicId => "pvz_rare_big_mouthed_nut";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetRemainingCooldownRounds();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PvzRareBigMouthedNutLastTriggeredRound = -CooldownRounds;
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeCombatStart()
    {
        if (AstralParty_PvzRareBigMouthedNutLastTriggeredRound < -CooldownRounds)
            AstralParty_PvzRareBigMouthedNutLastTriggeredRound = -CooldownRounds;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null)
            return Task.CompletedTask;

        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
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
        if (result.UnblockedDamage <= 0m || dealer == null || !dealer.IsAlive)
            return;
        if (dealer.Side == ownerCreature.Side)
            return;
        if (IsEliteOrBoss(dealer))
            return;

        var currentRound = ownerCreature.CombatState?.RoundNumber ?? 0;
        if (!PvzNutRelicHelper.CanTriggerThisRound(
                currentRound,
                AstralParty_PvzRareBigMouthedNutLastTriggeredRound,
                CooldownRounds))
            return;

        AstralParty_PvzRareBigMouthedNutLastTriggeredRound = PvzNutRelicHelper.MarkTriggeredThisRound(currentRound);
        InvokeDisplayAmountChanged();
        Flash();
        await CreatureCmd.Damage(choiceContext, dealer, dealer.CurrentHp + dealer.Block + 999m, ValueProp.Move, ownerCreature, null);
        await CreatureCmd.Heal(ownerCreature, HealAmount, true);
        MainFile.Logger.Info(
            $"[PvzRareBigMouthedNut] Triggered execution | owner={Owner?.NetId} | target={dealer?.GetType().Name} | round={currentRound}");
    }

    private static bool IsEliteOrBoss(Creature creature)
    {
        var roomType = creature.CombatState?.Encounter?.RoomType;
        return roomType is RoomType.Elite or RoomType.Boss;
    }

    private int GetRemainingCooldownRounds()
    {
        var currentRound = Owner?.Creature?.CombatState?.RoundNumber ?? 0;
        return PvzNutRelicHelper.GetRemainingCooldownRounds(
            currentRound,
            AstralParty_PvzRareBigMouthedNutLastTriggeredRound,
            CooldownRounds);
    }
}
