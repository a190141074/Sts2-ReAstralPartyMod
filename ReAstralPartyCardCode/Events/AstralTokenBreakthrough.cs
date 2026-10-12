using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

[RegisterActEvent(typeof(Glory))]
public sealed class AstralTokenBreakthrough : AstralPartyEventModel
{
    private const decimal MinimumGold = 1760m;
    private const int MaxBreakthroughs = 4;
    [SavedProperty] public int AstralParty_TokenBreakthroughCount { get; set; }
    protected override string EventId => "astral_token_breakthrough";

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 2 && runState.Players.All(p => p.Gold >= MinimumGold);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var canBreakthrough = Owner != null && CreateCandidates(Owner, 0).Count == 3;
        return
        [
            new EventOption(this, canBreakthrough ? OpenBreakthrough : null, InitialOptionKey("BREAKTHROUGH")),
            new EventOption(this, GiveGoldAndFinish, InitialOptionKey("GOLD"))
        ];
    }

    private async Task GiveGoldAndFinish()
    {
        if (Owner != null)
            await PersonMultiplayerEffectHelper.GainGoldDeterministic(110m, Owner);
        SetEventFinished(PageDescription("GOLD_FINISH"));
    }

    private Task OpenBreakthrough()
    {
        SetEventState(PageDescription("BREAKTHROUGH"), CreateBreakthroughOptions());
        return Task.CompletedTask;
    }

    private IReadOnlyList<EventOption> CreateBreakthroughOptions()
    {
        if (Owner == null)
            return [new EventOption(this, Finish, ModOptionKey("BREAKTHROUGH", "LEAVE"))];
        var count = GetCount(Owner);
        if (count >= MaxBreakthroughs)
            return [new EventOption(this, Finish, ModOptionKey("BREAKTHROUGH", "LEAVE"))];

        var candidates = CreateCandidates(Owner, count);
        if (candidates.Count != 3)
            return [new EventOption(this, Finish, ModOptionKey("BREAKTHROUGH", "LEAVE"))];
        var options = candidates.Select((candidate, index) => CreateCandidateOption(candidate, count, index)).ToList();
        options.Add(new EventOption(this, Finish, ModOptionKey("BREAKTHROUGH", "LEAVE")));
        return options;
    }

    private static List<TokenBreakthroughCandidate> CreateCandidates(MegaCrit.Sts2.Core.Entities.Players.Player owner,
        int count)
    {
        var tokenCandidates = Enumerable.Range(1, 2)
            .SelectMany(slot => TokenBreakthroughHelper.CreateCandidates(owner, count, slot))
            .DistinctBy(candidate => $"{candidate.Source.Id.Entry}|{candidate.Target.Id.Entry}")
            .ToList();
        if (tokenCandidates.Count == 0)
        {
            var normalCandidates = TokenBreakthroughHelper.CreateNormalRelicCandidates(owner, count, 0);
            var normalOptions = new List<TokenBreakthroughCandidate>();
            foreach (var candidate in normalCandidates)
            {
                if (normalOptions.Any(existing => existing.Source.Id == candidate.Source.Id
                                                  || existing.Target.Id == candidate.Target.Id))
                    continue;
                normalOptions.Add(candidate);
                if (normalOptions.Count == 3)
                    return normalOptions;
            }

            return normalOptions;
        }

        var candidates = TokenBreakthroughHelper.CreateCandidates(owner, count, 0).ToList();
        foreach (var candidate in tokenCandidates)
        {
            if (candidates.Any(existing => existing.Source.Id == candidate.Source.Id
                                           || existing.Target.Id == candidate.Target.Id))
                continue;
            candidates.Add(candidate);
            if (candidates.Count == 3)
                break;
        }

        return candidates;
    }

    private EventOption CreateCandidateOption(TokenBreakthroughCandidate candidate, int count, int slot)
    {
        var cost = TokenBreakthroughHelper.Cost(candidate, count);
        var sourceName = candidate.Source.Title.GetFormattedText();
        var targetName = candidate.Target.Title.GetFormattedText();
        var title = new LocString("events", "RE_ASTRAL_PARTY_MOD_EVENT_ASTRAL_TOKEN_BREAKTHROUGH.option.title");
        var description = new LocString("events",
            "RE_ASTRAL_PARTY_MOD_EVENT_ASTRAL_TOKEN_BREAKTHROUGH.option.description");
        title.Add("Source", sourceName);
        title.Add("Target", targetName);
        description.Add("Source", sourceName);
        description.Add("Target", targetName);
        description.Add("Cost", cost);
        var sourceId = candidate.Source.CanonicalInstance?.Id ?? candidate.Source.Id;
        var ownedSource = Owner?.Relics.FirstOrDefault(relic =>
            (relic.CanonicalInstance?.Id ?? relic.Id) == sourceId) ?? candidate.Source;
        return new EventOption(this, Owner != null && Owner.Gold >= cost ? () => Resolve(candidate) : null, title,
            description,
            ModOptionKey("BREAKTHROUGH", $"SLOT_{slot}"), ownedSource.HoverTips.Concat(candidate.Target.HoverTips));
    }

    private async Task Resolve(TokenBreakthroughCandidate candidate)
    {
        if (Owner == null)
            return;
        var owner = Owner;
        var count = GetCount(owner);
        var sourceId = candidate.Source.CanonicalInstance?.Id ?? candidate.Source.Id;
        var targetId = candidate.Target.CanonicalInstance?.Id ?? candidate.Target.Id;
        var source = owner.Relics.FirstOrDefault(r => (r.CanonicalInstance?.Id ?? r.Id) == sourceId);
        var target = candidate.Target.CanonicalInstance ?? candidate.Target;
        var cost = TokenBreakthroughHelper.Cost(candidate, count);
        if (count >= MaxBreakthroughs || source == null || owner.Gold < cost ||
            owner.Relics.Any(r => (r.CanonicalInstance ?? r).Id == targetId))
        {
            SetEventState(PageDescription("INVALID"), CreateBreakthroughOptions());
            return;
        }

        await PersonMultiplayerEffectHelper.LoseGoldDeterministic(cost, owner, GoldLossType.Spent);
        await RelicCmd.Remove(source);
        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(owner, target);
        SetCount(owner, count + 1);
        if (count + 1 >= MaxBreakthroughs)
            SetEventState(PageDescription("COMPLETE"),
                [new EventOption(this, Finish, ModOptionKey("BREAKTHROUGH", "LEAVE"))]);
        else
            SetEventState(PageDescription("BREAKTHROUGH"), CreateBreakthroughOptions());
    }

    private Task Finish()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private int GetCount(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        return AstralParty_TokenBreakthroughCount;
    }

    private void SetCount(MegaCrit.Sts2.Core.Entities.Players.Player owner, int count)
    {
        AstralParty_TokenBreakthroughCount = count;
    }
}
