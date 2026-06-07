using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class AbsoluteFormHelper
{
    private const string ContextId = "ultimate_skill_absolute_form";

    private static readonly string[] FormEntries =
    [
        "DEMON_FORM",
        "SERPENT_FORM",
        "VOID_FORM",
        "REAPER_FORM",
        "ECHO_FORM"
    ];

    private static readonly Lazy<IReadOnlyList<CardModel>> FormCards = new(ResolveFormCards);
    private static readonly Lazy<IReadOnlyList<CardModel>> BaseGameCurses = new(ResolveBaseGameCurses);

    public static async Task AutoPlayRandomFormForPlayer(
        PlayerChoiceContext choiceContext,
        Player player,
        int effectIndex)
    {
        if (player.Creature?.CombatState == null)
            return;

        var forms = FormCards.Value;
        if (forms.Count == 0)
            return;

        var selected = forms[
            DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                forms.Count,
                MainFile.ModId,
                ContextId,
                nameof(AutoPlayRandomFormForPlayer),
                player.Creature.CombatState.RoundNumber,
                player.NetId,
                effectIndex)];

        var cardToPlay = player.Creature.CombatState.CreateCard(selected.CanonicalInstance ?? selected, player);
        await CardCmd.AutoPlay(choiceContext, cardToPlay, player.Creature, AutoPlayType.Default, false, true);
    }

    public static async Task AddRandomCurseToDiscard(Player player, AbstractModel source, int roundNumber)
    {
        if (player.Creature?.CombatState == null)
            return;

        var curses = BaseGameCurses.Value;
        if (curses.Count == 0)
            return;

        var selected = curses[
            DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                curses.Count,
                MainFile.ModId,
                ContextId,
                nameof(AddRandomCurseToDiscard),
                roundNumber,
                player.NetId)];

        var createdCard = player.Creature.CombatState.CreateCard(selected.CanonicalInstance ?? selected, player);
        await CardPileCmd.Add(createdCard, PileType.Discard, CardPilePosition.Bottom, source, false);
    }

    private static IReadOnlyList<CardModel> ResolveFormCards()
    {
        var result = new List<CardModel>(FormEntries.Length);
        foreach (var entry in FormEntries)
        {
            if (!OptionalModModelResolver.TryFindCardByEntry(entry, out var card))
            {
                MainFile.Logger.Warn(
                    $"[{MainFile.ModId}] Absolute Form missing base-game form card '{entry}'. Skipping this entry.");
                continue;
            }

            result.Add(card.CanonicalInstance ?? card);
        }

        return result;
    }

    private static IReadOnlyList<CardModel> ResolveBaseGameCurses()
    {
        return ModelDb.AllCards
            .Where(card => card.GetType().Assembly == typeof(CardModel).Assembly)
            .Where(card => card.Type == CardType.Curse)
            .GroupBy(card => card.CanonicalInstance?.Id ?? card.Id)
            .Select(group => group.First().CanonicalInstance ?? group.First())
            .OrderBy(card => (card.CanonicalInstance?.Id ?? card.Id).Entry, StringComparer.Ordinal)
            .ToList();
    }
}
