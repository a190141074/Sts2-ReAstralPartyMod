using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class TyrantFormAutoPlayHelper
{
    private static readonly HashSet<CardModel> AutoPlayingCards = new(CardReferenceComparer.Instance);

    public static bool TryEnterAutoPlay(CardModel card)
    {
        lock (AutoPlayingCards)
        {
            return AutoPlayingCards.Add(card);
        }
    }

    public static void ExitAutoPlay(CardModel card)
    {
        lock (AutoPlayingCards)
        {
            AutoPlayingCards.Remove(card);
        }
    }

    public static bool IsCurrentlyAutoPlaying(CardModel card)
    {
        lock (AutoPlayingCards)
        {
            return AutoPlayingCards.Contains(card);
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
