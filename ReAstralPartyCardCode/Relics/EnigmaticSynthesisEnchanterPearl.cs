using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEnchanterPearl : EnigmaticNonStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticSynthesisEnchanterPearlCurseRollSequence { get; set; }

    protected override string RelicId => "enigmatic_synthesis_enchanter_pearl";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || !HasSevenCurses())
            return false;

        var smithOption = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smithOption == null)
            return false;

        smithOption.SmithCount += 1;
        return true;
    }

    internal void OnRestSiteOptionResolved(bool usedSmith)
    {
        if (!usedSmith || Owner == null || !HasSevenCurses())
            return;

        TaskHelper.RunSafely(OnRestSiteOptionResolvedAsync());
    }

    private async Task OnRestSiteOptionResolvedAsync()
    {
        if (Owner == null)
            return;

        var curseCard = CreateRandomCurseCard();
        if (curseCard == null)
            return;

        curseCard.FloorAddedToDeck = Math.Max(Owner.RunState?.TotalFloor ?? 1, 1);
        if (!await EventDeckCardHelper.AddCardToRunDeckAsync(Owner, curseCard))
            return;

        Flash();
    }

    public static Task<IReadOnlyList<EnigmaticSynthesisEnchanterPearl>> GrantCopies(Player owner, int amount)
    {
        return EnigmaticNonStackableUniqueMaterialRelicBase.GrantCopies<EnigmaticSynthesisEnchanterPearl>(owner, amount);
    }

    private bool HasSevenCurses()
    {
        return Owner?.GetRelic<EnigmaticSevenCurses>() != null;
    }

    private CardModel? CreateRandomCurseCard()
    {
        var candidates = ModelDb.AllCards
            .Where(card => card.Type == CardType.Curse)
            .Where(card => !EnigmaticAcknowledgmentDeckHelper.IsInfinitumCard(card))
            .GroupBy(card => card.CanonicalInstance?.Id ?? card.Id)
            .Select(group => group.First())
            .OrderBy(card => (card.CanonicalInstance?.Id ?? card.Id).Entry, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
            return null;

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            candidates.Count,
            MainFile.ModId,
            RelicId,
            "smith_random_curse",
            Owner?.NetId ?? 0UL,
            Owner?.RunState?.TotalFloor ?? 0,
            AstralParty_EnigmaticSynthesisEnchanterPearlCurseRollSequence);
        AstralParty_EnigmaticSynthesisEnchanterPearlCurseRollSequence++;

        return candidates[selectedIndex].ToMutable();
    }

}
