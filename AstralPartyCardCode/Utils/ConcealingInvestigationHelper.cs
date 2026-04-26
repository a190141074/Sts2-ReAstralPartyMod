using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Relics;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
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
    private static readonly string[] BonnieTokens = ["BONNIE", "BUNNY", "\u90A6\u5C3C"];
    private const int StageAdvanceThreshold = 6;
    private const decimal BonnieTriggerStarLightAmount = 5m;
    private static readonly AsyncLocal<Player?> CurrentControllerPlayer = new();
    private static readonly AsyncLocal<Player?> CurrentTriggerPlayer = new();

    public static async Task ResolveInvestigationTrigger(
        PlayerChoiceContext choiceContext,
        Player controllerPlayer,
        Player triggerPlayer
    )
    {
        var combatState = triggerPlayer.Creature?.CombatState;
        if (combatState == null)
            return;

        var poisonedApple = controllerPlayer.GetRelic<PersonPoisonedApple>();
        var stage = poisonedApple?.GetCurrentInvestigationStage() ?? InvestigationStage.Stage1;
        var cardModel = GetInvestigationCardForStage(stage);
        var cardToPlay = combatState.CreateCard(cardModel, triggerPlayer);

        var previousController = CurrentControllerPlayer.Value;
        var previousTrigger = CurrentTriggerPlayer.Value;
        CurrentControllerPlayer.Value = controllerPlayer;
        CurrentTriggerPlayer.Value = triggerPlayer;

        try
        {
            await CardCmd.AutoPlay(choiceContext, cardToPlay, triggerPlayer.Creature, AutoPlayType.Default, false,
                true);
        }
        finally
        {
            CurrentControllerPlayer.Value = previousController;
            CurrentTriggerPlayer.Value = previousTrigger;
        }

        if (poisonedApple != null)
        {
            if (controllerPlayer.Creature != null && controllerPlayer.Creature.IsAlive)
                await PowerCmd.Apply<StarLightPower>(
                    controllerPlayer.Creature,
                    BonnieTriggerStarLightAmount,
                    controllerPlayer.Creature,
                    null,
                    false
                );

            poisonedApple.RecordInvestigationTrigger();
        }
    }

    public static async Task TryTriggerTruthUnveiledOnSpecialTarget(
        PlayerChoiceContext choiceContext,
        Player controllerPlayer,
        Player triggerPlayer,
        CardModel? source
    )
    {
        var poisonedApple = controllerPlayer.GetRelic<PersonPoisonedApple>();
        if (poisonedApple == null || !poisonedApple.HasCompletedTruthRevealProgress())
            return;

        var roomType = triggerPlayer.Creature?.CombatState?.Encounter?.RoomType;
        if (roomType is not (RoomType.Elite or RoomType.Boss))
            return;

        var cardToPlay =
            triggerPlayer.Creature!.CombatState!.CreateCard(ModelDb.Card<EventsConcealingInvestigationD>(),
                triggerPlayer);
        await CardCmd.AutoPlay(choiceContext, cardToPlay, triggerPlayer.Creature, AutoPlayType.Default, false, true);
    }

    public static async Task ApplyInvestigationTarget(
        Creature target,
        Player controllerPlayer,
        CardModel? source
    )
    {
        if (controllerPlayer.Creature == null)
            return;

        await PowerCmd.Apply<InvestigationTargetPower>(target, 1m, controllerPlayer.Creature, source, false);
    }

    public static InvestigationStage GetInvestigationStageForTriggerCount(int triggerCount)
    {
        return triggerCount switch
        {
            < StageAdvanceThreshold => InvestigationStage.Stage1,
            < StageAdvanceThreshold * 2 => InvestigationStage.Stage2,
            < StageAdvanceThreshold * 3 => InvestigationStage.Stage3,
            _ => InvestigationStage.TruthUnveiled
        };
    }

    public static int GetProgressWithinStage(int triggerCount)
    {
        return Math.Min(triggerCount % StageAdvanceThreshold, StageAdvanceThreshold);
    }

    public static int GetStageAdvanceThreshold()
    {
        return StageAdvanceThreshold;
    }

    public static CardModel GetInvestigationCardForStage(InvestigationStage stage)
    {
        return stage switch
        {
            InvestigationStage.Stage1 => ModelDb.Card<EventsConcealingInvestigationA>(),
            InvestigationStage.Stage2 => ModelDb.Card<EventsConcealingInvestigationB>(),
            InvestigationStage.Stage3 => ModelDb.Card<EventsConcealingInvestigationC>(),
            _ => ModelDb.Card<EventsConcealingInvestigationD>()
        };
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
            if (player.Creature != null && player.Creature.IsAlive && !creatures.Contains(player.Creature))
                creatures.Add(player.Creature);

        foreach (var enemy in combatState.GetOpponentsOf(ownerCreature))
            if (enemy.IsAlive && !creatures.Contains(enemy))
                creatures.Add(enemy);

        return creatures;
    }

    private static IEnumerable<Player> GetTriggerAndBonniePlayers(Player triggerPlayer)
    {
        var players = new List<Player>();
        var runtimeTriggerPlayer = CurrentTriggerPlayer.Value ?? triggerPlayer;
        var bonniePlayer = CurrentControllerPlayer.Value ?? FindBonniePlayer(triggerPlayer);
        if (bonniePlayer != null)
            players.Add(bonniePlayer);
        if (!players.Contains(runtimeTriggerPlayer))
            players.Add(runtimeTriggerPlayer);

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

    public enum InvestigationStage
    {
        Stage1,
        Stage2,
        Stage3,
        TruthUnveiled
    }
}