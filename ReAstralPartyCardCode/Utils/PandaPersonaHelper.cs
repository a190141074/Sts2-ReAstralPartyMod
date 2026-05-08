using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class PandaPersonaHelper
{
    private static readonly IReadOnlyList<CardModel> FoodCards =
    [
        ModelDb.Card<BaseAbilityChocolateCake>(),
        ModelDb.Card<BaseAbilityHamburger>()
    ];

    private static readonly string[] IntentMemberNames =
    [
        "Intent",
        "CurrentIntent",
        "VisibleIntent",
        "Move",
        "CurrentMove"
    ];

    private static readonly string[] DamageMemberNames =
    [
        "AttackDamage",
        "Damage",
        "BaseDamage",
        "IntentDamage",
        "Value",
        "Amount"
    ];

    private static readonly string[] HitCountMemberNames =
    [
        "Hits",
        "Times",
        "HitCount",
        "RepeatCount",
        "Count"
    ];

    public static bool IsFoodCard(CardModel? card)
    {
        return card is BaseAbilityChocolateCake or BaseAbilityHamburger;
    }

    public static bool IsHamburger(CardModel? card)
    {
        return card is BaseAbilityHamburger;
    }

    public static CardModel GetDeterministicFoodCardModel(Player owner)
    {
        var selectedIndex = DeterministicSelectionHelper.PickDistinctIndices(
            1,
            FoodCards.Count,
            MainFile.ModId,
            nameof(PersonPandaMeng),
            owner.NetId,
            PileType.Hand.GetPile(owner).Cards.Count,
            PileType.Draw.GetPile(owner).Cards.Count,
            PileType.Discard.GetPile(owner).Cards.Count)
            .FirstOrDefault();
        return FoodCards[selectedIndex];
    }

    public static decimal GetEnemyAttackIntentSum(Creature ownerCreature)
    {
        var combatState = ownerCreature.CombatState;
        if (combatState == null)
            return 0m;

        decimal total = 0m;
        foreach (var enemy in combatState.GetOpponentsOf(ownerCreature).Where(creature => creature.IsAlive))
            total += TryReadAttackIntentDamage(enemy);

        return total;
    }

    public static bool HasAttackIntent(Creature creature)
    {
        var intent = TryReadIntentObject(creature);
        return intent != null && intent.GetType().Name.Contains("Attack", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal TryReadAttackIntentDamage(Creature creature)
    {
        var intent = TryReadIntentObject(creature);
        if (intent == null)
            return 0m;

        var intentType = intent.GetType();
        if (!intentType.Name.Contains("Attack", StringComparison.OrdinalIgnoreCase))
            return 0m;

        var damage = ReadNumericMember(intent, DamageMemberNames);
        if (damage <= 0m)
            return 0m;

        var hitCount = Math.Max(1, (int)ReadNumericMember(intent, HitCountMemberNames));
        return damage * hitCount;
    }

    private static object? TryReadIntentObject(object source)
    {
        foreach (var memberName in IntentMemberNames)
        {
            var value = ReadMemberValue(source, memberName);
            if (value != null)
                return value;
        }

        foreach (var member in source.GetType()
                     .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     .Where(member => member.MemberType is MemberTypes.Property or MemberTypes.Field)
                     .Where(member => member.Name.Contains("Intent", StringComparison.OrdinalIgnoreCase)
                                      || member.Name.Contains("Move", StringComparison.OrdinalIgnoreCase)))
        {
            var value = ReadMemberValue(source, member.Name);
            if (value != null)
                return value;
        }

        return null;
    }

    private static decimal ReadNumericMember(object source, IEnumerable<string> memberNames)
    {
        foreach (var memberName in memberNames)
        {
            var value = ReadMemberValue(source, memberName);
            if (TryConvertToDecimal(value, out var numericValue))
                return numericValue;
        }

        return 0m;
    }

    private static object? ReadMemberValue(object source, string memberName)
    {
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var property = source.GetType().GetProperty(memberName, bindingFlags);
        if (property != null && property.GetIndexParameters().Length == 0)
            return property.GetValue(source);

        var field = source.GetType().GetField(memberName, bindingFlags);
        return field?.GetValue(source);
    }

    private static bool TryConvertToDecimal(object? value, out decimal numericValue)
    {
        switch (value)
        {
            case byte byteValue:
                numericValue = byteValue;
                return true;
            case short shortValue:
                numericValue = shortValue;
                return true;
            case int intValue:
                numericValue = intValue;
                return true;
            case long longValue:
                numericValue = longValue;
                return true;
            case float floatValue:
                numericValue = (decimal)floatValue;
                return true;
            case double doubleValue:
                numericValue = (decimal)doubleValue;
                return true;
            case decimal decimalValue:
                numericValue = decimalValue;
                return true;
            default:
                numericValue = 0m;
                return false;
        }
    }
}
