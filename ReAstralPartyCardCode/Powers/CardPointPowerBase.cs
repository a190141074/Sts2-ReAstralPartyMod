using System.Collections.Concurrent;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public abstract class CardPointPowerBase : AstralPartyPowerModel
{
    private static readonly MethodInfo? SetAmountMethodDefinition = ResolveSetAmountMethodDefinition();
    private static readonly ConcurrentDictionary<Type, MethodInfo?> ClosedSetAmountMethods = new();

    private sealed class PendingPointSpend
    {
        public required CardModel Card { get; init; }
        public required decimal Amount { get; init; }
    }

    private sealed class Data
    {
        public List<PendingPointSpend> CurrentSpendPreviews { get; } = [];
        public List<PendingPointSpend> PendingSpends { get; } = [];
    }

    private bool _isRepairingUnauthorizedState;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldReceiveCombatHooks => true;

    public override int DisplayAmount => (int)Amount;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    protected abstract decimal LastAcceptedAmount { get; set; }

    protected virtual decimal GetMinimumCostAfterPriorityPointSpend(CardModel card)
    {
        return 0m;
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected abstract bool QualifiesForPointSpend(CardModel card);

    internal bool CanResolveForCard(CardModel? card)
    {
        return card != null
               && Amount > 0m
               && QualifiesForPointSpend(card);
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (target != Owner || amount <= 0m || canonicalPower.GetType() != GetType())
            return false;

        if (PointPowerAuthorizationHelper.IsAuthorizedPositiveGrant(target, GetType()))
            return false;

        modifiedAmount = 0m;
        return true;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (!CanPreviewPointCost(card))
            return false;
        if (!ReferenceEquals(CardPointPowerResolver.ResolveHighestPriorityPointPower(Owner, card), this))
            return false;

        if (!TryGetPriorityPointModifiedCost(
                originalCost,
                Amount,
                GetMinimumCostAfterPriorityPointSpend(card),
                out modifiedCost,
                out var pointAmountToSpend))
        {
            RemoveCurrentSpendPreview(card);
            return false;
        }

        TrackCurrentSpendPreview(card, pointAmountToSpend);
        return modifiedCost != originalCost;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await ReconcileAcceptedAmountAfterChange();
        RefreshHandEnergyCosts(Owner?.Player);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || power.GetType() != GetType())
            return;

        await ReconcileAcceptedAmountAfterChange();
        RefreshHandEnergyCosts(Owner?.Player);
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!CanTrackPlayedCard(cardPlay.Card))
            return Task.CompletedTask;
        if (!ReferenceEquals(CardPointPowerResolver.ResolveHighestPriorityPointPower(Owner, cardPlay.Card), this))
            return Task.CompletedTask;

        var pointAmountToSpend = ConsumeCurrentSpendPreview(cardPlay.Card);
        if (pointAmountToSpend <= 0m)
        {
            var totalCost = GetPrePointResolvedCost(cardPlay.Card);
            pointAmountToSpend = CalculatePriorityPointSpend(
                totalCost,
                Amount,
                GetMinimumCostAfterPriorityPointSpend(cardPlay.Card));
        }

        if (pointAmountToSpend <= 0m)
            return Task.CompletedTask;

        var pendingSpends = GetInternalData<Data>().PendingSpends;
        pendingSpends.RemoveAll(entry => ReferenceEquals(entry.Card, cardPlay.Card));
        pendingSpends.Add(new PendingPointSpend
        {
            Card = cardPlay.Card,
            Amount = pointAmountToSpend
        });

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!CanTrackPlayedCard(cardPlay.Card))
            return;

        var spentPointAmount = ConsumePendingPointSpend(cardPlay.Card);
        if (spentPointAmount <= 0m &&
            ReferenceEquals(CardPointPowerResolver.ResolveHighestPriorityPointPower(Owner, cardPlay.Card), this))
        {
            spentPointAmount = CalculatePriorityPointSpend(
                GetPrePointResolvedCost(cardPlay.Card),
                Amount,
                GetMinimumCostAfterPriorityPointSpend(cardPlay.Card));
        }
        if (spentPointAmount <= 0m)
            return;

        var nextAmount = Math.Max(0m, Amount - spentPointAmount);
        LastAcceptedAmount = nextAmount;

        if (nextAmount <= 0m)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(this, -spentPointAmount, Owner, cardPlay.Card, true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        ClearTrackedSpends();
        LastAcceptedAmount = 0m;
        await PowerCmd.Remove(this);
    }

    public override Task AfterRemoved(Creature? oldOwner)
    {
        ClearTrackedSpends();
        LastAcceptedAmount = 0m;
        RefreshHandEnergyCosts(oldOwner?.Player);
        return Task.CompletedTask;
    }

    protected static async Task GrantFromRelic<TPointPower>(Creature target, decimal amount, RelicModel sourceRelic)
        where TPointPower : CardPointPowerBase
    {
        if (target.Player == null || amount <= 0m)
            return;

        await PointPowerAuthorizationHelper.RunAuthorizedGrant(target, sourceRelic, typeof(TPointPower), () =>
            PowerCmd.Apply(
                ModelDb.Power<TPointPower>().ToMutable(),
                target,
                amount,
                sourceRelic.Owner?.Creature,
                null,
                false));
    }

    protected static bool TryGetPriorityPointModifiedCost(
        decimal originalCost,
        decimal availablePointAmount,
        decimal minimumCostAfterPointSpend,
        out decimal modifiedCost,
        out decimal pointAmountToSpend)
    {
        modifiedCost = originalCost;
        pointAmountToSpend = 0m;
        if (originalCost <= 0m || availablePointAmount <= 0m)
            return false;

        pointAmountToSpend = CalculatePriorityPointSpend(
            originalCost,
            availablePointAmount,
            minimumCostAfterPointSpend);
        modifiedCost = Math.Max(Math.Max(0m, minimumCostAfterPointSpend), originalCost - pointAmountToSpend);
        return modifiedCost != originalCost;
    }

    protected static bool TryGetShortfallPointModifiedCost(
        decimal originalCost,
        decimal availableEnergy,
        decimal availablePointAmount,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (originalCost <= availableEnergy || availablePointAmount <= 0m)
            return false;

        var deficit = originalCost - availableEnergy;
        if (deficit <= 0m || deficit > availablePointAmount)
            return false;

        modifiedCost = availableEnergy;
        return modifiedCost != originalCost;
    }

    protected static decimal CalculatePriorityPointSpend(decimal totalCost, decimal availablePointAmount)
    {
        return CalculatePriorityPointSpend(totalCost, availablePointAmount, 0m);
    }

    protected static decimal CalculatePriorityPointSpend(
        decimal totalCost,
        decimal availablePointAmount,
        decimal minimumCostAfterPointSpend)
    {
        if (totalCost <= 0m || availablePointAmount <= 0m)
            return 0m;

        var maximumSpend = Math.Max(0m, totalCost - Math.Max(0m, minimumCostAfterPointSpend));
        return maximumSpend <= 0m
            ? 0m
            : Math.Min(maximumSpend, availablePointAmount);
    }

    protected static decimal GetPrePointResolvedCost(CardModel card)
    {
        if (card.EnergyCost.CostsX)
            return 0m;

        return Math.Max(0m, card.EnergyCost.GetWithModifiers(CostModifiers.Local));
    }

    private bool CanPreviewPointCost(CardModel card)
    {
        if (!CanTrackPlayedCard(card))
            return false;
        if (card.Pile?.Type != PileType.Hand)
            return false;

        return true;
    }

    private bool CanTrackPlayedCard(CardModel card)
    {
        if (Owner?.Player?.PlayerCombatState == null)
            return false;
        if (card.Owner != Owner.Player)
            return false;
        if (card.EnergyCost.CostsX)
            return false;

        return CanResolveForCard(card);
    }

    private async Task ReconcileAcceptedAmountAfterChange()
    {
        if (_isRepairingUnauthorizedState || Owner == null)
            return;

        if (Amount <= 0m)
        {
            LastAcceptedAmount = 0m;
            return;
        }

        var currentAmount = Amount;
        var acceptedAmount = Math.Max(0m, LastAcceptedAmount);
        var isAuthorizedPositiveGrant = PointPowerAuthorizationHelper.IsAuthorizedPositiveGrant(Owner, GetType());

        if (currentAmount > acceptedAmount)
        {
            if (isAuthorizedPositiveGrant)
            {
                LastAcceptedAmount = currentAmount;
                return;
            }

            _isRepairingUnauthorizedState = true;
            try
            {
                if (acceptedAmount <= 0m)
                    await PowerCmd.Remove(this);
                else
                    await SetAmountForCurrentPowerType(Owner, acceptedAmount);
            }
            finally
            {
                _isRepairingUnauthorizedState = false;
            }

            return;
        }

        LastAcceptedAmount = currentAmount;
    }

    private void TrackCurrentSpendPreview(CardModel card, decimal amount)
    {
        var previews = GetInternalData<Data>().CurrentSpendPreviews;
        previews.RemoveAll(entry => ReferenceEquals(entry.Card, card));
        if (amount <= 0m)
            return;

        previews.Add(new PendingPointSpend
        {
            Card = card,
            Amount = amount
        });
    }

    private void RemoveCurrentSpendPreview(CardModel card)
    {
        GetInternalData<Data>().CurrentSpendPreviews.RemoveAll(entry => ReferenceEquals(entry.Card, card));
    }

    private decimal ConsumeCurrentSpendPreview(CardModel card)
    {
        var previews = GetInternalData<Data>().CurrentSpendPreviews;
        for (var i = previews.Count - 1; i >= 0; i--)
        {
            var preview = previews[i];
            if (!ReferenceEquals(preview.Card, card))
                continue;

            previews.RemoveAt(i);
            return Math.Min(Math.Max(0m, preview.Amount), Math.Max(0m, Amount));
        }

        return 0m;
    }

    private decimal ConsumePendingPointSpend(CardModel card)
    {
        var pendingSpends = GetInternalData<Data>().PendingSpends;
        for (var i = pendingSpends.Count - 1; i >= 0; i--)
        {
            var pendingSpend = pendingSpends[i];
            if (!ReferenceEquals(pendingSpend.Card, card))
                continue;

            pendingSpends.RemoveAt(i);
            return Math.Min(Math.Max(0m, pendingSpend.Amount), Math.Max(0m, Amount));
        }

        return 0m;
    }

    private void ClearTrackedSpends()
    {
        var data = GetInternalData<Data>();
        data.CurrentSpendPreviews.Clear();
        data.PendingSpends.Clear();
    }

    private async Task SetAmountForCurrentPowerType(Creature target, decimal amount)
    {
        var closedMethod = ClosedSetAmountMethods.GetOrAdd(
            GetType(),
            static powerType =>
            {
                if (SetAmountMethodDefinition == null || !typeof(PowerModel).IsAssignableFrom(powerType))
                    return null;

                try
                {
                    return SetAmountMethodDefinition.MakeGenericMethod(powerType);
                }
                catch
                {
                    return null;
                }
            });
        if (closedMethod?.Invoke(null, [target, amount, target, null]) is Task task)
            await task;
    }

    private static MethodInfo? ResolveSetAmountMethodDefinition()
    {
        return typeof(PowerCmd)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(static method =>
            {
                if (method.Name != nameof(PowerCmd.SetAmount) ||
                    !method.IsGenericMethodDefinition ||
                    method.GetGenericArguments().Length != 1)
                    return false;

                var parameters = method.GetParameters();
                return parameters.Length == 4 &&
                       parameters[0].ParameterType == typeof(Creature) &&
                       parameters[1].ParameterType == typeof(decimal) &&
                       parameters[2].ParameterType == typeof(Creature) &&
                       parameters[3].ParameterType == typeof(CardModel);
            });
    }

    private static void RefreshHandEnergyCosts(Player? player)
    {
        foreach (var card in player?.PlayerCombatState?.Hand?.Cards ?? [])
            card.InvokeEnergyCostChanged();
    }
}
