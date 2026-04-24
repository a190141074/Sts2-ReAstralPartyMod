using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Relics;
using AstralPartyMod.AstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class ConcealingInvestigationHelper
{
    private static readonly CardModel[] InvestigationCards =
    [
        ModelDb.Card<EventsConcealingInvestigationA>(),
        ModelDb.Card<EventsConcealingInvestigationB>(),
        ModelDb.Card<EventsConcealingInvestigationC>(),
        ModelDb.Card<EventsConcealingInvestigationD>()
    ];

    private static readonly string[] BonnieTokens = ["BONNIE", "BUNNY", "\u90A6\u5C3C"];

    public static async Task TriggerRandomInvestigationEvent(PlayerChoiceContext choiceContext, Player triggerPlayer)
    {
        var combatState = triggerPlayer.Creature?.CombatState;
        if (combatState == null || InvestigationCards.Length == 0)
            return;

        var cardModel = InvestigationCards[triggerPlayer.RunState.Rng.CombatTargets.NextInt(InvestigationCards.Length)];
        var cardToPlay = combatState.CreateCard(cardModel, triggerPlayer);
        await CardCmd.AutoPlay(choiceContext, cardToPlay, triggerPlayer.Creature, AutoPlayType.Default, false, true);
    }

    public static async Task ApplyMarkToRandomEligibleEnemy(
        Player triggerPlayer,
        decimal amount,
        bool allowElite,
        bool allowBoss,
        CardModel? source
    )
    {
        var target = GetRandomEligibleEnemy(triggerPlayer, allowElite, allowBoss);
        if (target == null || triggerPlayer.Creature == null)
            return;

        await PowerCmd.Apply<MarkLockPower>(target, amount, triggerPlayer.Creature, source, false);
    }

    public static async Task ApplyMarkToAllLivingUnits(Player triggerPlayer, decimal amount, CardModel? source)
    {
        if (triggerPlayer.Creature == null)
            return;

        foreach (var creature in GetAllLivingUnits(triggerPlayer))
            await PowerCmd.Apply<MarkLockPower>(creature, amount, triggerPlayer.Creature, source, false);
    }

    public static async Task GrantAttackCardOrEnergyToTriggerAndBonnie(Player triggerPlayer, CardModel? source)
    {
        foreach (var player in GetTriggerAndBonniePlayers(triggerPlayer))
            await GrantAttackCardOrEnergy(player, source);
    }

    public static async Task GrantAttackCardOrEnergyToAllPlayers(Player triggerPlayer, CardModel? source)
    {
        foreach (var player in GetAllPlayers(triggerPlayer))
            await GrantAttackCardOrEnergy(player, source);
    }

    public static async Task ApplyConcealingToTriggerAndBonnie(Player triggerPlayer, CardModel? source)
    {
        if (triggerPlayer.Creature == null)
            return;

        foreach (var player in GetTriggerAndBonniePlayers(triggerPlayer))
        {
            if (player.Creature == null || !player.Creature.IsAlive)
                continue;

            await PowerCmd.Apply<ConcealingPower>(player.Creature, 1m, triggerPlayer.Creature, source, false);
        }
    }

    public static async Task ApplyConcealingToAllPlayers(Player triggerPlayer, CardModel? source)
    {
        if (triggerPlayer.Creature == null)
            return;

        foreach (var player in GetAllPlayers(triggerPlayer))
        {
            if (player.Creature == null || !player.Creature.IsAlive)
                continue;

            await PowerCmd.Apply<ConcealingPower>(player.Creature, 1m, triggerPlayer.Creature, source, false);
        }
    }

    private static Creature? GetRandomEligibleEnemy(Player triggerPlayer, bool allowElite, bool allowBoss)
    {
        var ownerCreature = triggerPlayer.Creature;
        var combatState = ownerCreature?.CombatState;
        if (ownerCreature == null || combatState == null)
            return null;

        var roomType = combatState.Encounter?.RoomType;
        if (!allowBoss && roomType == RoomType.Boss)
            return null;
        if (!allowElite && roomType == RoomType.Elite)
            return null;

        var enemies = combatState
            .GetOpponentsOf(ownerCreature)
            .Where(creature => creature.IsAlive)
            .ToList();
        if (enemies.Count == 0)
            return null;

        var targetIndex = triggerPlayer.RunState.Rng.CombatTargets.NextInt(enemies.Count);
        return enemies[targetIndex];
    }

    private static IEnumerable<Creature> GetAllLivingUnits(Player triggerPlayer)
    {
        var combatState = triggerPlayer.Creature?.CombatState;
        var ownerCreature = triggerPlayer.Creature;
        if (combatState == null || ownerCreature == null)
            return [];

        var creatures = new List<Creature>();

        foreach (var player in combatState.Players)
        {
            if (player.Creature != null && player.Creature.IsAlive && !creatures.Contains(player.Creature))
                creatures.Add(player.Creature);
        }

        foreach (var enemy in combatState.GetOpponentsOf(ownerCreature))
        {
            if (enemy.IsAlive && !creatures.Contains(enemy))
                creatures.Add(enemy);
        }

        return creatures;
    }

    private static IEnumerable<Player> GetTriggerAndBonniePlayers(Player triggerPlayer)
    {
        var players = new List<Player>();
        var bonniePlayer = FindBonniePlayer(triggerPlayer);
        if (bonniePlayer != null)
            players.Add(bonniePlayer);
        if (!players.Contains(triggerPlayer))
            players.Add(triggerPlayer);

        return players;
    }

    private static IEnumerable<Player> GetAllPlayers(Player triggerPlayer)
    {
        return triggerPlayer.Creature?.CombatState?.Players ?? [];
    }

    private static Player? FindBonniePlayer(Player triggerPlayer)
    {
        return triggerPlayer.Creature?.CombatState?.Players.FirstOrDefault(IsBonniePlayer);
    }

    private static bool IsBonniePlayer(Player player)
    {
        return MatchesBonnieToken(player)
               || MatchesBonnieToken(ReadPropertyValue(player, "Character"))
               || MatchesBonnieToken(ReadPropertyValue(player, "CharacterModel"));
    }

    private static bool MatchesBonnieToken(object? value)
    {
        if (value == null)
            return false;

        foreach (var candidate in EnumerateCandidateStrings(value))
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            var normalized = candidate.Trim();
            if (BonnieTokens.Any(token => normalized.Contains(token, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> EnumerateCandidateStrings(object value)
    {
        if (!string.IsNullOrWhiteSpace(value.ToString()))
            yield return value.ToString()!;

        foreach (var propertyName in new[] { "Id", "Entry", "EntryKey", "Name", "Title", "ModelId", "CharacterId" })
        {
            var propertyValue = ReadPropertyValue(value, propertyName);
            if (propertyValue == null)
                continue;

            if (!string.IsNullOrWhiteSpace(propertyValue.ToString()))
                yield return propertyValue.ToString()!;

            var nestedEntry = ReadPropertyValue(propertyValue, "Entry");
            if (!string.IsNullOrWhiteSpace(nestedEntry?.ToString()))
                yield return nestedEntry.ToString()!;
        }
    }

    private static object? ReadPropertyValue(object source, string propertyName)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

        var property = source.GetType().GetProperty(propertyName, Flags);
        if (property?.CanRead != true)
            return null;

        try
        {
            return property.GetValue(source);
        }
        catch
        {
            return null;
        }
    }

    private static async Task GrantAttackCardOrEnergy(Player player, CardModel? source)
    {
        var drawPileAttack = FindAttackCard(PileType.Draw.GetPile(player).Cards);
        if (drawPileAttack != null)
        {
            await MoveCardToHand(player, drawPileAttack);
            return;
        }

        var discardPileAttack = FindAttackCard(PileType.Discard.GetPile(player).Cards);
        if (discardPileAttack != null)
        {
            await MoveCardToHand(player, discardPileAttack);
            return;
        }

        await PlayerCmd.GainEnergy(1m, player);
    }

    private static CardModel? FindAttackCard(IEnumerable<CardModel> cards)
    {
        return cards.FirstOrDefault(card => card.Type == CardType.Attack);
    }

    private static async Task MoveCardToHand(Player recipient, CardModel card)
    {
        await CardPileCmd.Add(card, PileType.Hand.GetPile(recipient));

        if (recipient.Creature?.CombatState == null)
            return;

        foreach (var player in recipient.Creature.CombatState.Players)
        {
            var relic = player.GetRelic<PersonShadowScion>();
            if (relic == null)
                continue;

            await relic.HandleObservedCardGain(recipient, card);
        }
    }
}
