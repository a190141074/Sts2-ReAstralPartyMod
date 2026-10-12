using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenExclusiveOriginalConsciousness : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_TokenExclusiveOriginalConsciousnessLastObservedMind { get; set; }
    [SavedProperty] public int AstralParty_TokenExclusiveOriginalConsciousnessResolutionSerial { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override string RelicId => "token_exclusive_original_consciousness";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MindPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFinalArenaSeriesId)
    ];

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenExclusiveOriginalConsciousnessLastObservedMind =
            (int)Math.Max(0m, Owner?.Creature?.GetPowerAmount<MindPower>() ?? 0m);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_TokenExclusiveOriginalConsciousnessLastObservedMind = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;
        if (power.Owner != Owner.Creature)
            return;
        if (power is not MindPower)
            return;

        var currentMind = (int)Math.Max(0m, amount);
        var previousMind = Math.Max(0, AstralParty_TokenExclusiveOriginalConsciousnessLastObservedMind);
        if (currentMind == previousMind)
            return;

        AstralParty_TokenExclusiveOriginalConsciousnessLastObservedMind = currentMind;
        var delta = currentMind - previousMind;
        if (delta > 0)
        {
            Flash();
            await CreatureCmd.GainMaxHp(Owner.Creature, delta);
            return;
        }

        await UpgradeRandomAttackCards(-delta);
    }

    private async Task UpgradeRandomAttackCards(int count)
    {
        if (Owner == null || count <= 0)
            return;

        var candidates = EventDeckCardHelper.GetRunDeckCards(Owner)
            .Select((card, index) => new DeckCardEntry(card, index))
            .Where(static entry => entry.Card.Type == CardType.Attack && entry.Card.IsUpgradable)
            .ToList();
        if (candidates.Count == 0)
            return;

        var orderedCandidates = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
            candidates,
            static entry =>
                $"{entry.Index:D4}|{(entry.Card.CanonicalInstance ?? entry.Card).Id.Entry}|{entry.Card.CurrentUpgradeLevel:D2}",
            MainFile.ModId,
            Id.Entry,
            Owner.RunState?.Rng.StringSeed,
            Owner.NetId,
            AstralParty_TokenExclusiveOriginalConsciousnessResolutionSerial,
            count);
        var selectedCards = orderedCandidates
            .Take(count)
            .Select(static entry => entry.Card)
            .ToList();
        if (selectedCards.Count == 0)
            return;

        AstralParty_TokenExclusiveOriginalConsciousnessResolutionSerial++;
        Flash();
        await EventDeckCardMutationHelper.Upgrade(
            Owner,
            selectedCards,
            $"{Id.Entry}_mind_loss_{AstralParty_TokenExclusiveOriginalConsciousnessResolutionSerial}");
    }

    private readonly record struct DeckCardEntry(CardModel Card, int Index);
}
