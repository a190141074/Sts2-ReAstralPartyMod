using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropStoneFluxPauldron : MoonPropStackableRelicBase
{
    private const decimal DamageTakenMultiplierPerStack = 1.5m;

    [SavedProperty] public string AstralParty_MoonPropStoneFluxBaseMaxHpSnapshotSerialized { get; set; } = string.Empty;
    [SavedProperty] public int AstralParty_MoonPropStoneFluxGrantedStacks { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("BaseMaxHpBonusPercent", GetBaseMaxHpBonusPercentText()),
        new StringVar("DamageTakenBonusPercent", GetDamageTakenBonusPercentText())
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (IsMelted || Owner?.Creature == null)
            return;

        EnsureBaseMaxHpSnapshot(Owner.Creature);
        await GrantMissingStacksAsync();
        RefreshDynamicState();
    }

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature || amount <= 0m)
            return amount;

        return amount * GetDamageTakenMultiplier();
    }

    protected override Task AfterStacksChangedAsync()
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;

        return GrantMissingStacksAsync();
    }

    private decimal GetBaseMaxHpSnapshot()
    {
        return StableNumericStateHelper.DeserializeDecimal(AstralParty_MoonPropStoneFluxBaseMaxHpSnapshotSerialized);
    }

    private decimal EnsureBaseMaxHpSnapshot(Creature creature)
    {
        var snapshot = GetBaseMaxHpSnapshot();
        if (snapshot > 0m)
            return snapshot;

        snapshot = creature.MaxHp;
        AstralParty_MoonPropStoneFluxBaseMaxHpSnapshotSerialized = StableNumericStateHelper.SerializeDecimal(snapshot);
        return snapshot;
    }

    private decimal GetDamageTakenMultiplier()
    {
        return MoonPropFormulaHelper.GetRepeatedMultiplier(DamageTakenMultiplierPerStack, GetStacks());
    }

    private async Task GrantMissingStacksAsync()
    {
        if (Owner?.Creature == null)
            return;

        var snapshot = EnsureBaseMaxHpSnapshot(Owner.Creature);
        var targetGrantedStacks = GetStacks();
        var missingStacks = Math.Max(0, targetGrantedStacks - AstralParty_MoonPropStoneFluxGrantedStacks);
        if (missingStacks <= 0 || snapshot <= 0m)
        {
            RefreshDynamicState();
            return;
        }

        AstralParty_MoonPropStoneFluxGrantedStacks = targetGrantedStacks;
        RefreshDynamicState();
        await CreatureCmd.GainMaxHp(Owner.Creature, snapshot * missingStacks);
    }

    private string GetBaseMaxHpBonusPercentText()
    {
        return FormatPercent(GetStacks());
    }

    private string GetDamageTakenBonusPercentText()
    {
        return FormatPercent(GetDamageTakenMultiplier() - 1m);
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("BaseMaxHpBonusPercent", GetBaseMaxHpBonusPercentText());
        SetDynamicString("DamageTakenBonusPercent", GetDamageTakenBonusPercentText());
    }
}
