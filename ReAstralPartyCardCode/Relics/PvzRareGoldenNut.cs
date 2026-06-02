using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PvzRareGoldenNut : AstralPartyRelicModel
{
    private const decimal GoldThreshold = 100m;
    private const decimal HealAmount = 5m;
    private const decimal MaxHpAmount = 5m;

    [SavedProperty] public decimal AstralParty_PvzRareGoldenNutSpentGoldProgress { get; set; }
    [SavedProperty] public decimal AstralParty_PvzRareGoldenNutLastObservedGold { get; set; }

    protected override string RelicId => "pvz_rare_golden_nut";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(0, (int)decimal.Floor(AstralParty_PvzRareGoldenNutSpentGoldProgress));

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PvzRareGoldenNutSpentGoldProgress = 0m;
        AstralParty_PvzRareGoldenNutLastObservedGold = Owner?.Gold ?? 0m;
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_PvzRareGoldenNutLastObservedGold = Owner?.Gold ?? 0m;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;
        await SyncGoldDecreaseAsync("after_player_turn_start");
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
            return;
        await SyncGoldDecreaseAsync("after_card_played");
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.Side != side)
            return;
        await SyncGoldDecreaseAsync("after_turn_end");
    }

    public override Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        AstralParty_PvzRareGoldenNutLastObservedGold = Owner?.Gold ?? 0m;
        return Task.CompletedTask;
    }

    private async Task SyncGoldDecreaseAsync(string sourceTag)
    {
        if (Owner == null)
            return;

        var currentGold = Owner.Gold;
        var lostAmount = Math.Max(0m, AstralParty_PvzRareGoldenNutLastObservedGold - currentGold);
        AstralParty_PvzRareGoldenNutLastObservedGold = currentGold;
        if (lostAmount <= 0m)
            return;

        AstralParty_PvzRareGoldenNutSpentGoldProgress += lostAmount;
        InvokeDisplayAmountChanged();

        var triggerCount = 0;
        while (AstralParty_PvzRareGoldenNutSpentGoldProgress >= GoldThreshold)
        {
            AstralParty_PvzRareGoldenNutSpentGoldProgress -= GoldThreshold;
            triggerCount++;
        }

        if (triggerCount <= 0 || Owner.Creature == null)
            return;

        InvokeDisplayAmountChanged();
        for (var i = 0; i < triggerCount; i++)
        {
            Flash();
            await CreatureCmd.Heal(Owner.Creature, HealAmount, true);
            await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpAmount);
        }

        MainFile.Logger.Info(
            $"[PvzRareGoldenNut] Triggered from gold loss | owner={Owner?.NetId} | source={sourceTag} | lostAmount={lostAmount} | triggerCount={triggerCount} | remainingProgress={AstralParty_PvzRareGoldenNutSpentGoldProgress}");
    }
}
