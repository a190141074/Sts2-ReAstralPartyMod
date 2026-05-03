using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonZhao : CooldownPersonaRelicBase
{
    private const int FoxfireCostThreshold = 2;

    [SavedProperty] public int AstralParty_PersonZhaoCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonZhaoPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_PersonZhaoAccumulatedAttackCostThisCombat { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonZhaoCounter;
        set => AstralParty_PersonZhaoCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonZhaoPendingCombatStartCard;
        set => AstralParty_PersonZhaoPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillInvokeSpirits>(),
        HoverTipFactory.FromPower<FoxfirePower>(),
        HoverTipFactory.FromPower<InvokeSpiritsPower>(),
        HoverTipFactory.FromPower<ExtraAttackPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        AstralParty_PersonZhaoAccumulatedAttackCostThisCombat = 0;

        if (Owner.Creature.GetPower<ExtraAttackPower>() == null)
        {
            await PowerCmd.Apply<ExtraAttackPower>(Owner.Creature, 1m, Owner.Creature, null, false);
        }
    }

    protected override Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonZhaoAccumulatedAttackCostThisCombat = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var playedByObservedPlayer = cardPlay.Card.Owner == Owner || IsCurrentInvokeTarget(cardPlay.Card.Owner);
        if (!playedByObservedPlayer)
            return;
        if (cardPlay.Card.Type != CardType.Attack)
            return;

        var extraAttackPower = Owner.Creature.GetPower<ExtraAttackPower>();
        if (extraAttackPower != null && extraAttackPower.IsTriggeredAttack(cardPlay.Card))
            return;

        AstralParty_PersonZhaoAccumulatedAttackCostThisCombat += AttackCardCostHelper.GetPlayedCost(cardPlay);
        while (AstralParty_PersonZhaoAccumulatedAttackCostThisCombat >= FoxfireCostThreshold)
        {
            AstralParty_PersonZhaoAccumulatedAttackCostThisCombat -= FoxfireCostThreshold;
            Flash();
            await PowerCmd.Apply<FoxfirePower>(Owner.Creature, 1m, Owner.Creature, cardPlay.Card, false);
        }
    }

    public async Task ReplaceInvokeTarget(Player targetPlayer, CardModel sourceCard)
    {
        if (Owner?.Creature == null)
            return;

        foreach (var player in Owner.Creature.CombatState?.Players ?? Enumerable.Empty<Player>())
        {
            if (player == targetPlayer)
                continue;

            var existingPower = player.Creature?.GetPower<InvokeSpiritsPower>();
            if (existingPower?.AstralParty_InvokeSpiritsZhaoPlayerNetId == Owner.NetId)
                await PowerCmd.Remove(existingPower);
        }

        var power = (InvokeSpiritsPower)ModelDb.Power<InvokeSpiritsPower>().ToMutable();
        power.AstralParty_InvokeSpiritsZhaoPlayerNetId = Owner.NetId;
        power.AstralParty_InvokeSpiritsHasReachedNextOwnerTurn = false;
        await PowerCmd.Apply(power, targetPlayer.Creature!, 1m, Owner.Creature, sourceCard, false);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillInvokeSpirits>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private bool IsCurrentInvokeTarget(Player? player)
    {
        if (Owner?.Creature?.CombatState == null || player?.Creature == null)
            return false;

        var power = player.Creature.GetPower<InvokeSpiritsPower>();
        return power?.AstralParty_InvokeSpiritsZhaoPlayerNetId == Owner.NetId;
    }
}
