using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonSykes : CooldownPersonRelicBase
{
    [SavedProperty] public int AstralParty_PersonSykesCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonSykesPendingCombatStartCard { get; set; }

    private readonly HashSet<Creature> _abyssalErosionTargetsThisCard = [];
    private CardModel? _trackedAbyssalErosionCard;
    private bool _abyssalErosionAppliedToFirstTarget;

    protected override int CounterValue
    {
        get => AstralParty_PersonSykesCounter;
        set => AstralParty_PersonSykesCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonSykesPendingCombatStartCard;
        set => AstralParty_PersonSykesPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 3;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool SpawnsPets => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<cards.SkillAbyssalTransformation>(),
        HoverTipFactory.FromPower<AbyssalRealmPower>(),
        HoverTipFactory.FromPower<AbyssalTentaclesPower>(),
        HoverTipFactory.FromPower<AbyssalErosionMarkPower>()
    ];

    public override Task BeforeCombatStart()
    {
        ResetAbyssalErosionTracking();
        return Owner == null
            ? Task.CompletedTask
            : AbyssalTentacleCombatHelper.SummonAsync(Owner, null);
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;

        TrackAbyssalErosionCard(cardPlay.Card);
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return;

        var enemies = CombatTargetOrdering.GetLivingOpponentsStable(Owner.Creature);
        if (enemies.Count == 0)
            return;

        var count = Owner.RunState.Rng.CombatTargets.NextInt(enemies.Count + 1);
        var selected = enemies.ToList();
        for (var i = 0; i < count; i++)
        {
            var index = Owner.RunState.Rng.CombatTargets.NextInt(selected.Count);
            var target = selected[index];
            selected.RemoveAt(index);
            await PowerCmd.Apply<AbyssalRealmPower>(target, 1m, Owner.Creature, null, false);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await base.AfterTurnEnd(choiceContext, side);
        if (Owner?.Creature == null || side != Owner.Creature.Side || Owner.Creature.IsDead)
            return;

        await AbyssalTentacleCombatHelper.SummonAsync(Owner, null);
        await AbyssalTentacleCombatHelper.ActivateAllAsync(choiceContext, Owner, null);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature || target.Side == Owner.Creature.Side)
            return;
        if (cardSource?.Owner != Owner || cardSource.Type != CardType.Attack || result.TotalDamage <= 0m)
            return;

        if (!ReferenceEquals(_trackedAbyssalErosionCard, cardSource))
            TrackAbyssalErosionCard(cardSource);

        if (cardSource.TargetType == TargetType.AllEnemies)
        {
            if (!_abyssalErosionTargetsThisCard.Add(target))
                return;
        }
        else
        {
            if (_abyssalErosionAppliedToFirstTarget)
                return;

            _abyssalErosionAppliedToFirstTarget = true;
        }

        await PowerCmd.Apply<AbyssalErosionMarkPower>(target, 1m, Owner.Creature, cardSource, false);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        ResetAbyssalErosionTracking();
        if (Owner?.Creature != null)
        {
            AbyssalTentacleCombatHelper.Cleanup(Owner);
        }

        await base.AfterCombatEnd(room);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(
            ModelDb.Card<cards.SkillAbyssalTransformation>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }

    private void TrackAbyssalErosionCard(CardModel card)
    {
        _trackedAbyssalErosionCard = card;
        _abyssalErosionTargetsThisCard.Clear();
        _abyssalErosionAppliedToFirstTarget = false;
    }

    private void ResetAbyssalErosionTracking()
    {
        _trackedAbyssalErosionCard = null;
        _abyssalErosionTargetsThisCard.Clear();
        _abyssalErosionAppliedToFirstTarget = false;
    }
}
