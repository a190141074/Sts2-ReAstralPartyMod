using MegaCrit.Sts2.Core.Commands;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Combat;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PvzRareDestructionNut : AstralPartyRelicModel
{
    private const int CooldownRounds = 40;
    private const decimal CurseHealAmount = 2m;
    private const decimal DeathDamageAmount = 36m;

    [SavedProperty] public int AstralParty_PvzRareDestructionNutLastTriggeredRound { get; set; }

    protected override string RelicId => "pvz_rare_destruction_nut";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetRemainingCooldownRounds();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PvzRareDestructionNutLastTriggeredRound = -CooldownRounds;
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeCombatStart()
    {
        if (AstralParty_PvzRareDestructionNutLastTriggeredRound < -CooldownRounds)
            AstralParty_PvzRareDestructionNutLastTriggeredRound = -CooldownRounds;
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
        if (ownerCreature == null || target != ownerCreature)
            return;
        if (result.UnblockedDamage <= 0m)
            return;
        if (cardSource?.Type != CardType.Curse)
            return;

        Flash();
        await CreatureCmd.Heal(ownerCreature, CurseHealAmount, true);
        MainFile.Logger.Info(
            $"[PvzRareDestructionNut] Converted curse damage to heal | owner={Owner?.NetId} | damage={result.UnblockedDamage} | heal={CurseHealAmount} | source={cardSource.Id.Entry}");
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature target,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null || target != ownerCreature || wasRemovalPrevented)
            return;

        var currentRound = ownerCreature.CombatState?.RoundNumber ?? 0;
        if (!PvzNutRelicHelper.CanTriggerThisRound(
                currentRound,
                AstralParty_PvzRareDestructionNutLastTriggeredRound,
                CooldownRounds))
            return;

        AstralParty_PvzRareDestructionNutLastTriggeredRound = PvzNutRelicHelper.MarkTriggeredThisRound(currentRound);
        InvokeDisplayAmountChanged();
        Flash();
        foreach (var enemy in CombatTargetSnapshotHelper.GetAliveNonAlliedCreatures(ownerCreature.CombatState!, ownerCreature))
            await CreatureCmd.Damage(choiceContext, enemy, DeathDamageAmount, ValueProp.Unpowered, ownerCreature, null);

        MainFile.Logger.Info(
            $"[PvzRareDestructionNut] Triggered death explosion | owner={Owner?.NetId} | round={currentRound} | damage={DeathDamageAmount}");
    }

    private int GetRemainingCooldownRounds()
    {
        var currentRound = Owner?.Creature?.CombatState?.RoundNumber ?? 0;
        return PvzNutRelicHelper.GetRemainingCooldownRounds(
            currentRound,
            AstralParty_PvzRareDestructionNutLastTriggeredRound,
            CooldownRounds);
    }
}
