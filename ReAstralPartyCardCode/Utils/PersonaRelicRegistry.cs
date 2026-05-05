using System.Collections.Generic;
using System.Linq;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class PersonaRelicRegistry
{
    private static readonly IReadOnlyList<RelicModel> PersonaRelics =
    [
        ModelDb.Relic<PersonWeirdEgg>(),
        ModelDb.Relic<PersonSamuraiPrawn>(),
        ModelDb.Relic<PersonSlimeLulu>(),
        ModelDb.Relic<PersonBionicJasmine>(),
        ModelDb.Relic<PersonProprietress>(),
        ModelDb.Relic<PersonMousyLian>(),
        ModelDb.Relic<PersonBlueWhale>(),
        ModelDb.Relic<PersonOasisQueen>(),
        ModelDb.Relic<PersonInkShadowHunter>(),
        ModelDb.Relic<PersonMascotGirlMimi>(),
        ModelDb.Relic<PersonSupermanMegas>(),
        ModelDb.Relic<PersonXiaoLei>(),
        ModelDb.Relic<PersonSocialFearNun>(),
        ModelDb.Relic<PersonJillSteinle>(),
        ModelDb.Relic<PersonShadowScion>(),
        ModelDb.Relic<PersonPoisonedApple>(),
        ModelDb.Relic<PersonMidnightFlash>(),
        ModelDb.Relic<PersonVampire>(),
        ModelDb.Relic<PersonCyberKitty>(),
        ModelDb.Relic<PersonNinja>(),
        ModelDb.Relic<PersonZhao>(),
        ModelDb.Relic<PersonUnclePederman>(),
        ModelDb.Relic<PersonFeng>()
    ];

    public static IReadOnlyList<RelicModel> GetCanonicalPersonaRelics()
    {
        return PersonaRelics
            .DistinctBy(relic => relic.CanonicalInstance?.Id ?? relic.Id)
            .ToList();
    }

    public static bool IsPersonaRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return PersonaRelics.Any(candidate => (candidate.CanonicalInstance?.Id ?? candidate.Id) == id);
    }

    public static IReadOnlyList<RelicModel> GetAvailablePersonaRelics(Player owner)
    {
        var ownedRelicIds = owner.Relics
            .Select(relic => relic.CanonicalInstance.Id)
            .ToHashSet();

        return GetCanonicalPersonaRelics()
            .Where(relic => !ownedRelicIds.Contains(relic.Id))
            .ToList();
    }
}
