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
    protected abstract int StoredStacks { get; set; }

    public override bool ShowCounter => true;

    public override int DisplayAmount => Stacks;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralUniqueMaterialId)
    ];

    public int Stacks => Math.Max(StoredStacks, 0);

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        var existing = Owner?.Relics
            .OfType<EnigmaticUniqueMaterialRelicBase>()
            .FirstOrDefault(relic =>
                !ReferenceEquals(relic, this)
                && !relic.IsMelted
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
        if (amount <= 0)
            return;

        StoredStacks = Math.Max(0, StoredStacks + amount);
        InvokeDisplayAmountChanged();
    }

    public async Task ConsumeStacksAsync(int amount)
    {
        if (amount <= 0)
            return;

        StoredStacks = Math.Max(0, StoredStacks - amount);
        InvokeDisplayAmountChanged();
        if (StoredStacks == 0 && !IsMelted)
            await RelicCmd.Remove(this);
    }

    public static async Task<T?> GrantStacks<T>(Player owner, int amount)
        where T : EnigmaticUniqueMaterialRelicBase
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
