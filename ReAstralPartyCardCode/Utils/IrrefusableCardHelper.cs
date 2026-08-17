using System.Text.Json;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Models;
using STS2RitsuLib.RunData;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class IrrefusableCardHelper
{
    private const string RunDataKey = "irrefusable_cards";
    private const int StandardCombatsRemaining = 3;
    private const int CurseCombatsRemaining = StandardCombatsRemaining + 2;
    private static readonly HashSet<CardModel> PlayedFromHand = [];
    private static readonly HashSet<CardModel> DiscardedFromHand = [];
    private static RunSavedData<IrrefusableRunData> _runData = null!;
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            _runData = RitsuLibFramework.GetRunSavedDataStore(MainFile.ModId).Register(
                RunDataKey,
                static () => new IrrefusableRunData(),
                new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });
        }

        RitsuLibFramework.GetContentRegistry(MainFile.ModId).RegisterSingleton<IrrefusableCardLifecycle>();
        _registered = true;
    }

    public static void MarkTransferredCard(Player target, CardModel card)
    {
        if (target.RunState is not RunState runState)
            return;

        if (!card.Keywords.Contains(AstralKeywords.Irrefusable))
            CardCmd.ApplyKeyword(card, AstralKeywords.Irrefusable);

        var deckCards = EventDeckCardHelper.GetRunDeckCards(target);
        var index = deckCards.IndexOf(card);
        if (index < 0)
            return;

        var data = Get(runState);
        data.Cards.RemoveAll(record => record.PlayerNetId == target.NetId && record.DeckIndex == index);
        data.Cards.Add(new IrrefusableCardRecord
        {
            PlayerNetId = target.NetId,
            DeckIndex = index,
            CardId = (card.CanonicalInstance ?? card).Id.ToString(),
            UpgradeLevel = card.CurrentUpgradeLevel,
            FloorAddedToDeck = card.FloorAddedToDeck ?? 0,
            CombatsRemaining = card.Type == CardType.Curse
                ? CurseCombatsRemaining
                : StandardCombatsRemaining
        });
        _runData.Set(runState, data);

        MainFile.Logger.Info(
            $"[Irrefusable] marked permanent target deck card; no combat copy created | owner={target.NetId} | card={card.Id.Entry}#{card.GetHashCode()} | deck_index={index} | deck_count={target.Deck?.Cards.Count ?? -1} | in_run_state={runState.ContainsCard(card)} | combats_remaining={data.Cards[^1].CombatsRemaining}");
    }

    public static void TrackHandPileExit(CardModel card, PileType oldPileType)
    {
        if (!card.Keywords.Contains(AstralKeywords.Irrefusable) || oldPileType != PileType.Hand)
            return;

        switch (card.Pile?.Type)
        {
            case PileType.Play:
                PlayedFromHand.Add(card);
                break;
            case PileType.Discard:
                DiscardedFromHand.Add(card);
                break;
        }
    }

    public static async Task HandleCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (!PlayedFromHand.Remove(card)
            || !card.Keywords.Contains(AstralKeywords.Irrefusable)
            || card.Owner?.Creature == null)
            return;

        await ConsumeAndGrantStarlight(choiceContext, card, "played");
    }

    public static async Task HandleCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (!DiscardedFromHand.Remove(card)
            || !card.Keywords.Contains(AstralKeywords.Irrefusable)
            || card.Owner?.Creature == null)
            return;

        await ConsumeAndGrantStarlight(choiceContext, card, "discarded");
    }

    public static bool HasIrrefusableCard(Player? owner)
    {
        return owner != null && EventDeckCardHelper.GetRunDeckCards(owner)
            .Any(card => card.Keywords.Contains(AstralKeywords.Irrefusable));
    }

    public static async Task HandleCombatEnd(CombatRoom room, RunState? runState)
    {
        PlayedFromHand.Clear();
        DiscardedFromHand.Clear();

        if (runState == null)
            return;

        var data = Get(runState);
        foreach (var record in data.Cards.ToList())
        {
            record.CombatsRemaining--;
            if (record.CombatsRemaining > 0)
                continue;

            var owner = runState.Players.FirstOrDefault(player => player.NetId == record.PlayerNetId);
            var card = owner == null ? null : ResolveDeckCard(owner, record);
            if (owner != null && card != null)
                await EventDeckCardHelper.RemoveCardFromRunDeck(owner, card, false);

            data.Cards.Remove(record);
            MainFile.Logger.Info(
                $"[Irrefusable] expired card | owner={record.PlayerNetId} | card={record.CardId} | removed={card != null}");
        }

        _runData.Set(runState, data);
    }

    private static async Task ConsumeAndGrantStarlight(
        PlayerChoiceContext choiceContext,
        CardModel card,
        string trigger)
    {
        var owner = card.Owner;
        if (owner?.Creature == null)
            return;

        await CardCmd.Exhaust(choiceContext, card, false, false);
        await TokenEternalStarlight.GrantStacks(owner, 3);
        MainFile.Logger.Info(
            $"[Irrefusable] {trigger} from hand and exhausted | owner={owner.NetId} | card={card.Id.Entry}");
    }

    private static IrrefusableRunData Get(RunState runState)
    {
        if (!_registered)
            Register();
        return _runData.TryGet(runState, out var data) && data != null
            ? data
            : new IrrefusableRunData();
    }

    private static CardModel? ResolveDeckCard(Player owner, IrrefusableCardRecord record)
    {
        var cards = EventDeckCardHelper.GetRunDeckCards(owner);
        if (record.DeckIndex >= 0 && record.DeckIndex < cards.Count)
        {
            var indexed = cards[record.DeckIndex];
            if (Matches(indexed, record))
                return indexed;
        }

        return cards.FirstOrDefault(card => Matches(card, record));
    }

    private static bool Matches(CardModel card, IrrefusableCardRecord record)
    {
        return string.Equals((card.CanonicalInstance ?? card).Id.ToString(), record.CardId,
                   StringComparison.Ordinal)
               && card.CurrentUpgradeLevel == record.UpgradeLevel
               && (card.FloorAddedToDeck ?? 0) == record.FloorAddedToDeck
               && card.Keywords.Contains(AstralKeywords.Irrefusable);
    }
}

internal sealed class IrrefusableCardLifecycle : HookedSingletonModel
{
    public IrrefusableCardLifecycle() : base(HookType.Combat)
    {
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return IrrefusableCardHelper.HandleCardPlayed(choiceContext, cardPlay);
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        IrrefusableCardHelper.TrackHandPileExit(card, oldPileType);
        return Task.CompletedTask;
    }

    public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        return IrrefusableCardHelper.HandleCardDiscarded(choiceContext, card);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        return IrrefusableCardHelper.HandleCombatEnd(room, CurrentRunState as RunState);
    }
}

internal sealed class IrrefusableRunData
{
    public List<IrrefusableCardRecord> Cards { get; set; } = [];
}

internal sealed class IrrefusableCardRecord
{
    public ulong PlayerNetId { get; set; }
    public int DeckIndex { get; set; }
    public string CardId { get; set; } = string.Empty;
    public int UpgradeLevel { get; set; }
    public int FloorAddedToDeck { get; set; }
    public int CombatsRemaining { get; set; }
}
