using System.Globalization;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class MoonPropStackableRelicBase : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_MoonPropStacks { get; set; } = 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralMoonPropId)
    ];

    public override bool IsStackable => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount
    {
        get
        {
            RefreshDynamicState();
            return GetStacks();
        }
    }

    public int GetStacks()
    {
        return Math.Max(AstralParty_MoonPropStacks, 1);
    }

    public void AddStacks(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_MoonPropStacks = Math.Max(1, AstralParty_MoonPropStacks + amount);
        RefreshDynamicState();
        Flash();
        InvokeDisplayAmountChanged();
        TaskHelper.RunSafely(AfterStacksChangedAsync());
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        var existing = Owner?.Relics
            .OfType<MoonPropStackableRelicBase>()
            .FirstOrDefault(relic =>
                !ReferenceEquals(relic, this)
                && !relic.IsMelted
                && GetCanonicalRelicId(relic) == GetCanonicalRelicId(this));
        if (existing != null)
        {
            existing.AddStacks(GetStacks());
            await RelicCmd.Remove(this);
            return;
        }

        AstralParty_MoonPropStacks = Math.Max(1, AstralParty_MoonPropStacks);
        RefreshDynamicState();
        InvokeDisplayAmountChanged();
    }

    protected virtual Task AfterStacksChangedAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual void RefreshDynamicState()
    {
    }

    protected void SetDynamicString(string key, string value)
    {
        if (DynamicVars.TryGetValue(key, out var dynamicVar) && dynamicVar is StringVar stringVar)
            stringVar.StringValue = value;
    }

    protected static string FormatValue(decimal value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    protected static string FormatPercent(decimal ratio)
    {
        return $"{FormatValue(ratio * 100m)}%";
    }

    private static ModelId GetCanonicalRelicId(RelicModel relic)
    {
        return (relic.CanonicalInstance ?? relic).Id;
    }
}
