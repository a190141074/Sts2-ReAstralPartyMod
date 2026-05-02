using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class TokenBlueDieRelicBase : AstralPartyRelicModel
{
    private const int BaseTriggerCountForNewDieRelic = 3;
    private const int TwoDieSetCount = 2;
    private const int FiveDieSetCount = 5;
    private const int TwoDieSetReduction = 1;
    private const int FiveDieSetReduction = 2;

    [SavedProperty] public int AstralParty_PendingBlueDieStarLight { get; set; }
    [SavedProperty] public int AstralParty_BlueDieTriggerProgress { get; set; }

    protected abstract int TriggerRoundMultiple { get; }

    protected abstract int StarLightRewardAmount { get; }

    protected virtual int EnergyRewardAmount => 0;

    protected virtual int CardsToDrawAmount => 0;

    protected virtual int DamageToAllEnemiesAmount => 0;

    protected virtual int BlockRewardAmount => 0;

    protected virtual int HealRewardAmount => 0;

    protected virtual bool GainOtherDiceCombatStartEffects => false;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_BlueDieTriggerProgress;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralDiceSetId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PendingBlueDieStarLight = 0;
        AstralParty_BlueDieTriggerProgress = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null)
            return;

        var roundNumber = room.CombatState.RoundNumber;
        if (roundNumber != TriggerRoundMultiple)
            return;

        AstralParty_PendingBlueDieStarLight += StarLightRewardAmount;
        AstralParty_BlueDieTriggerProgress = Math.Min(
            AstralParty_BlueDieTriggerProgress + 1,
            GetRequiredTriggerCountForNewDieRelic()
        );
        Flash();

        if (AstralParty_BlueDieTriggerProgress >= GetRequiredTriggerCountForNewDieRelic()
            && await TryObtainMissingDieRelic())
            AstralParty_BlueDieTriggerProgress = 0;

        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var pendingTriggerCount = GetPendingTriggerCount();
        if (pendingTriggerCount <= 0)
            return;

        AstralParty_PendingBlueDieStarLight = 0;

        Flash();
        for (var i = 0; i < pendingTriggerCount; i++)
        {
            await ApplyCombatStartEffectBundle(
                StarLightRewardAmount,
                EnergyRewardAmount,
                CardsToDrawAmount,
                DamageToAllEnemiesAmount,
                BlockRewardAmount,
                HealRewardAmount
            );

            if (GainOtherDiceCombatStartEffects)
            {
                await ApplyCombatStartEffectBundle(4, 2, 0, 0, 0, 0);
                await ApplyCombatStartEffectBundle(6, 0, 3, 0, 0, 0);
                await ApplyCombatStartEffectBundle(8, 0, 0, 5, 0, 0);
                await ApplyCombatStartEffectBundle(10, 0, 0, 0, 7, 0);
                await ApplyCombatStartEffectBundle(12, 0, 0, 0, 0, 9);
            }
        }
    }

    private int GetPendingTriggerCount()
    {
        if (StarLightRewardAmount <= 0 || AstralParty_PendingBlueDieStarLight <= 0)
            return 0;

        return AstralParty_PendingBlueDieStarLight / StarLightRewardAmount;
    }

    private async Task ApplyCombatStartEffectBundle(
        int starLightAmount,
        int energyAmount,
        int cardsToDraw,
        int damageToAllEnemies,
        int blockAmount,
        int healAmount)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        if (starLightAmount > 0)
            await PowerCmd.Apply(
                ModelDb.Power<StarLightPower>().ToMutable(),
                Owner.Creature,
                starLightAmount,
                Owner.Creature,
                null,
                false
            );

        if (energyAmount > 0)
            await PlayerCmd.GainEnergy(energyAmount, Owner);

        if (cardsToDraw > 0)
            await CardGainAttribution.RunWithSource(this,
                () => CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), cardsToDraw, Owner));

        if (damageToAllEnemies > 0)
        {
            var enemies = Owner.Creature.CombatState
                .GetOpponentsOf(Owner.Creature)
                .Where(creature => creature.IsAlive)
                .ToList();

            foreach (var enemy in enemies)
                await CreatureCmd.Damage(
                    new ThrowingPlayerChoiceContext(),
                    enemy,
                    damageToAllEnemies,
                    ValueProp.Unpowered,
                    Owner.Creature,
                    null
                );
        }

        if (blockAmount > 0)
            await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, null);

        if (healAmount > 0)
            await CreatureCmd.Heal(Owner.Creature, healAmount, true);
    }

    private async Task<bool> TryObtainMissingDieRelic()
    {
        if (Owner == null)
            return false;

        if (await TryObtainMissingDieRelic<TokenBlueDie4>())
            return true;
        if (await TryObtainMissingDieRelic<TokenBlueDie6>())
            return true;
        if (await TryObtainMissingDieRelic<TokenBlueDie8>())
            return true;
        if (await TryObtainMissingDieRelic<TokenBlueDie10>())
            return true;
        if (await TryObtainMissingDieRelic<TokenBlueDie12>())
            return true;
        if (await TryObtainMissingDieRelic<TokenBlueDie20>())
            return true;

        return false;
    }

    private async Task<bool> TryObtainMissingDieRelic<T>()
        where T : AstralPartyRelicModel
    {
        if (Owner == null || Owner.GetRelic<T>() != null)
            return false;

        await RewardSyncHelper.ObtainRelicAsReward(Owner, ModelDb.Relic<T>());
        return true;
    }

    private int GetRequiredTriggerCountForNewDieRelic()
    {
        var ownedDiceCount = CountOwnedDieRelics();
        var reduction = ownedDiceCount >= FiveDieSetCount
            ? FiveDieSetReduction
            : ownedDiceCount >= TwoDieSetCount
                ? TwoDieSetReduction
                : 0;

        return Math.Max(1, BaseTriggerCountForNewDieRelic - reduction);
    }

    private int CountOwnedDieRelics()
    {
        if (Owner == null)
            return 0;

        var count = 0;
        if (Owner.GetRelic<TokenBlueDie4>() != null)
            count++;
        if (Owner.GetRelic<TokenBlueDie6>() != null)
            count++;
        if (Owner.GetRelic<TokenBlueDie8>() != null)
            count++;
        if (Owner.GetRelic<TokenBlueDie10>() != null)
            count++;
        if (Owner.GetRelic<TokenBlueDie12>() != null)
            count++;
        if (Owner.GetRelic<TokenBlueDie20>() != null)
            count++;

        return count;
    }
}