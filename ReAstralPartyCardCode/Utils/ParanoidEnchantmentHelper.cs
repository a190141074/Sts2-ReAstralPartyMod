using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class ParanoidEnchantmentHelper
{
    private static readonly HashSet<CardModel> AutoPlayingCards = new(CardReferenceComparer.Instance);

    public static bool HasParanoidEnchantment(CardModel? card)
    {
        return card?.Enchantment is EssenceParanoidEnchantment;
    }

    public static bool ShouldForceCombatHooks(CardModel? card)
    {
        return HasParanoidEnchantment(card);
    }

    public static bool ShouldBlockManualPlay(CardModel? card)
    {
        return HasParanoidEnchantment(card);
    }

    public static async Task TryAutoPlayOnOwnerHpLoss(CardModel? card, Creature creature, decimal delta)
    {
        if (!HasParanoidEnchantment(card))
            return;
        if (card?.Owner?.Creature == null || creature != card.Owner.Creature)
            return;
        if (delta >= 0m)
            return;
        if (card.Pile == null)
            return;
        if (!TryEnterAutoPlay(card))
            return;

        try
        {
            await CardCmd.AutoPlay(
                new ThrowingPlayerChoiceContext(),
                card,
                card.Owner.Creature,
                MegaCrit.Sts2.Core.Entities.Cards.AutoPlayType.Default,
                true,
                false);
        }
        finally
        {
            ExitAutoPlay(card);
        }
    }

    private static bool TryEnterAutoPlay(CardModel card)
    {
        lock (AutoPlayingCards)
        {
            return AutoPlayingCards.Add(card);
        }
    }

    private static void ExitAutoPlay(CardModel card)
    {
        lock (AutoPlayingCards)
        {
            AutoPlayingCards.Remove(card);
        }
    }

    private sealed class CardReferenceComparer : IEqualityComparer<CardModel>
    {
        public static CardReferenceComparer Instance { get; } = new();

        public bool Equals(CardModel? x, CardModel? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(CardModel obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
