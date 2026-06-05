using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class EnigmaticUniqueMaterialRelicBase : AstralPartyRelicModel
{
    protected virtual int StoredStacks
    {
        get => 1;
        set { }
    }

    protected new virtual IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralUniqueMaterialId)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => ExtraHoverTips;
    public virtual bool IsStackableForSynthesis => true;
    public virtual int SynthesisAmount => IsStackableForSynthesis ? Stacks : 1;
    public int Stacks => Math.Max(StoredStacks, 0);

    public override bool ShowCounter => IsStackableForSynthesis;

    public override int DisplayAmount => IsStackableForSynthesis ? Stacks : 0;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (!IsStackableForSynthesis)
            return;

        var existing = Owner?.Relics
            .OfType<EnigmaticUniqueMaterialRelicBase>()
            .FirstOrDefault(relic =>
                !ReferenceEquals(relic, this)
                && !relic.IsMelted
                && relic.IsStackableForSynthesis
                && GetCanonicalRelicId(relic) == GetCanonicalRelicId(this));
        if (existing != null)
        {
            existing.AddStacks(Math.Max(StoredStacks, 1));
            await RelicCmd.Remove(this);
            return;
        }

        StoredStacks = Math.Max(1, StoredStacks);
        InvokeDisplayAmountChanged();
    }

    public void AddStacks(int amount)
    {
        if (amount <= 0 || !IsStackableForSynthesis)
            return;

        StoredStacks = Math.Max(0, StoredStacks + amount);
        InvokeDisplayAmountChanged();
    }

    public virtual async Task ConsumeForSynthesisAsync(int amount)
    {
        if (amount <= 0)
            return;
        if (!IsStackableForSynthesis)
        {
            if (!IsMelted)
                await RelicCmd.Remove(this);
            return;
        }

        StoredStacks = Math.Max(0, StoredStacks - amount);
        InvokeDisplayAmountChanged();
        if (StoredStacks == 0 && !IsMelted)
            await RelicCmd.Remove(this);
    }

    public Task ConsumeStacksAsync(int amount)
    {
        return ConsumeForSynthesisAsync(amount);
    }

    public static async Task<T?> GrantStacks<T>(Player owner, int amount)
        where T : EnigmaticStackableUniqueMaterialRelicBase
    {
        if (amount <= 0)
            return owner.GetRelic<T>();

        var relic = owner.GetRelic<T>();
        if (relic != null)
        {
            relic.AddStacks(amount);
            return relic;
        }

        await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(owner, ModelDb.Relic<T>());
        relic = owner.GetRelic<T>();
        relic?.AddStacks(amount - 1);
        return relic;
    }

    private static ModelId GetCanonicalRelicId(RelicModel relic)
    {
        return (relic.CanonicalInstance ?? relic).Id;
    }
}

public abstract class EnigmaticStackableUniqueMaterialRelicBase : EnigmaticUniqueMaterialRelicBase
{
}

public abstract class EnigmaticNonStackableUniqueMaterialRelicBase : EnigmaticUniqueMaterialRelicBase
{
    public override bool IsStackableForSynthesis => false;

    public static async Task<IReadOnlyList<T>> GrantCopies<T>(Player owner, int amount)
        where T : EnigmaticNonStackableUniqueMaterialRelicBase
    {
        var granted = new List<T>();
        if (amount <= 0)
            return granted;

        var canonicalRelic = ModelDb.Relic<T>();
        for (var i = 0; i < amount; i++)
        {
            var obtained = await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(owner, canonicalRelic);
            if (obtained is T typed)
                granted.Add(typed);
        }

        return granted;
    }
}
