using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonBlueWhale : CooldownPersonaRelicBase
{
    private const int FateWeakImprintKillGold = 3;
    private const int ExactRoundRewardBase = 6;
    private const int ExactRoundRewardBonusPerRepeat = 2;
    private const int ExactRoundTarget = 6;

    [SavedProperty] public int AstralParty_PersonBlueWhaleCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonBlueWhalePendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonBlueWhaleExactRound6RewardCount { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonBlueWhaleCounter;
        set => AstralParty_PersonBlueWhaleCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonBlueWhalePendingCombatStartCard;
        set => AstralParty_PersonBlueWhalePendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFateWeakMprint>(),
        HoverTipFactory.FromPower<FateWeakImprintPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonBlueWhaleExactRound6RewardCount = 0;
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature target,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || Owner == null || Owner.Creature == null)
            return;

        if (target.Side == Owner.Creature.Side)
            return;

        if (!target.HasPower<FateWeakImprintPower>())
            return;

        Flash();
        await PersonaMultiplayerEffectHelper.GainGoldDeterministic(FateWeakImprintKillGold, Owner);
    }

    protected override async Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        if (Owner == null)
            return;

        if (room.CombatState.RoundNumber == ExactRoundTarget)
        {
            // Each exact-turn clear increases future payouts by 2 gold.
            AstralParty_PersonBlueWhaleExactRound6RewardCount++;
            var goldToGain =
                ExactRoundRewardBase
                + (AstralParty_PersonBlueWhaleExactRound6RewardCount - 1) * ExactRoundRewardBonusPerRepeat;
            Flash();
            await PersonaMultiplayerEffectHelper.GainGoldDeterministic(goldToGain, Owner);
        }
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFateWeakMprint>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }
}
