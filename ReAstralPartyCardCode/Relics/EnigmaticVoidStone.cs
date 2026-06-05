using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.RestSite;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class EnigmaticVoidStone : EnigmaticNonStackableUniqueMaterialRelicBase
{
    private HashSet<string>? _obliviatedCardEntries;

    [SavedProperty]
    private string AstralParty_EnigmaticVoidStoneObliviatedCardEntriesSerialized
    {
        get => EnigmaticOblivionDeckHelper.SerializeIdSet(GetObliviatedCardEntries());
        set => _obliviatedCardEntries = EnigmaticOblivionDeckHelper.DeserializeIdSet(value);
    }

    protected override string RelicId => "enigmatic_void_stone";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
            return false;
        if (options.Any(static option => option is EnigmaticOblivionRestSiteOption))
            return false;

        options.Add(new EnigmaticOblivionRestSiteOption(player));
        return true;
    }

    internal bool CanUseOblivion(Player? player)
    {
        return player == Owner && EventDeckCardHelper.GetRunDeckCards(player).Count > 0;
    }

    internal void RecordObliviatedCard(CardModel? card)
    {
        var entry = EnigmaticOblivionDeckHelper.GetCanonicalCardEntry(card);
        if (string.IsNullOrWhiteSpace(entry))
            return;

        GetObliviatedCardEntries().Add(entry);
    }

    internal bool ContainsObliviatedCard(CardModel? card)
    {
        var entry = EnigmaticOblivionDeckHelper.GetCanonicalCardEntry(card);
        return !string.IsNullOrWhiteSpace(entry) && GetObliviatedCardEntries().Contains(entry);
    }

    private HashSet<string> GetObliviatedCardEntries()
    {
        _obliviatedCardEntries ??= new HashSet<string>(StringComparer.Ordinal);
        return _obliviatedCardEntries;
    }
}
