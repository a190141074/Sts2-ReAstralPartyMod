using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.TestSupport;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class BaseStarterCardReplacementHelper
{
    public static bool IsBaseStrike(Player? owner, CardModel? card)
    {
        return card != null && GetCanonicalCardId(card) == GetBaseStrikeId(owner);
    }

    public static bool IsBaseDefend(Player? owner, CardModel? card)
    {
        return card != null && GetCanonicalCardId(card) == GetBaseDefendId(owner);
    }

    public static ModelId GetBaseStrikeId(Player? owner)
    {
        return GetBaseStarterCardId(owner, CardTag.Strike, GetFallbackStrikeId);
    }

    public static ModelId GetBaseDefendId(Player? owner)
    {
        return GetBaseStarterCardId(owner, CardTag.Defend, GetFallbackDefendId);
    }

    public static ModelId GetCanonicalCardId(CardModel card)
    {
        return card.CanonicalInstance?.Id ?? card.Id;
    }

    private static ModelId GetBaseStarterCardId(
        Player? owner,
        CardTag tag,
        Func<Player?, ModelId> fallbackFactory)
    {
        var character = owner?.Character;
        var byCharacterPool = character?.CardPool.AllCards.FirstOrDefault(card =>
            card.Rarity == CardRarity.Basic && card.Tags.Contains(tag))?.Id;
        if (byCharacterPool is { } poolId && poolId != ModelId.none)
            return poolId;

        if (owner != null)
        {
            var byRunDeck = EventDeckCardHelper.GetRunDeckCards(owner)
                .FirstOrDefault(card =>
                    GetCanonicalCard(card).Rarity == CardRarity.Basic
                    && GetCanonicalCard(card).Tags.Contains(tag));
            if (byRunDeck != null)
                return GetCanonicalCardId(byRunDeck);
        }

        return fallbackFactory(owner);
    }

    private static CardModel GetCanonicalCard(CardModel card)
    {
        return card.CanonicalInstance ?? card;
    }

    private static ModelId GetFallbackStrikeId(Player? owner)
    {
        var character = owner?.Character;
        if (character == null)
            return ModelId.none;
        if (TestMode.IsOn && character is Deprived)
            return ModelDb.GetId<StrikeIronclad>();

        return character switch
        {
            Ironclad => ModelDb.GetId<StrikeIronclad>(),
            Silent => ModelDb.GetId<StrikeSilent>(),
            Defect => ModelDb.GetId<StrikeDefect>(),
            Necrobinder => ModelDb.GetId<StrikeNecrobinder>(),
            Regent => ModelDb.GetId<StrikeRegent>(),
            _ => ModelId.none
        };
    }

    private static ModelId GetFallbackDefendId(Player? owner)
    {
        var character = owner?.Character;
        if (character == null)
            return ModelId.none;
        if (TestMode.IsOn && character is Deprived)
            return ModelDb.GetId<DefendIronclad>();

        return character switch
        {
            Ironclad => ModelDb.GetId<DefendIronclad>(),
            Silent => ModelDb.GetId<DefendSilent>(),
            Defect => ModelDb.GetId<DefendDefect>(),
            Necrobinder => ModelDb.GetId<DefendNecrobinder>(),
            Regent => ModelDb.GetId<DefendRegent>(),
            _ => ModelId.none
        };
    }
}
