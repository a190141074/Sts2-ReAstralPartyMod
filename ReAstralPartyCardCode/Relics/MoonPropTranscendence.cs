using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropTranscendence : MoonPropStackableRelicBase
{
    private const decimal BaseMaxHpBonusRatio = 0.5m;
    private const decimal ExtraStackMaxHpBonusRatio = 0.25m;
    private const decimal OstyHealRatio = 0.12m;

    [SavedProperty] public string AstralParty_MoonPropTranscendenceBaseMaxHpSnapshotSerialized { get; set; } = string.Empty;
    [SavedProperty] public int AstralParty_MoonPropTranscendenceGrantedStacks { get; set; }
    [SavedProperty] public string AstralParty_MoonPropTranscendenceRecordedSummonSerialized { get; set; } = string.Empty;
    [SavedProperty] public int AstralParty_MoonPropTranscendenceSafeTurnCount { get; set; }
    [SavedProperty] public bool AstralParty_MoonPropTranscendenceDamagedThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("BaseMaxHpBonusPercent", GetBaseMaxHpBonusPercentText()),
        new StringVar("OstyHealPercent", GetOstyHealPercentText()),
        new StringVar("RecordedSummon", GetRecordedSummonText())
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

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        AstralParty_MoonPropTranscendenceSafeTurnCount = 0;
        AstralParty_MoonPropTranscendenceDamagedThisTurn = false;
        RefreshDynamicState();

        var creature = Owner.Creature;
        var hpBeforeClamp = creature.CurrentHp;
        var hpToConvert = Math.Max(0m, hpBeforeClamp - 1m);
        if (hpBeforeClamp > 1m)
            await CreatureCmd.SetCurrentHp(creature, 1m);

        var summonCap = GetOpeningSummonCap(creature);
        var summonToGain = StableNumericStateHelper.ClampCeilingToInt(
            hpToConvert + GetRecordedSummonAmount(),
            0m,
            summonCap);
        if (summonToGain <= 0)
            return;

        Flash();
        await OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner, summonToGain, this);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_MoonPropTranscendenceDamagedThisTurn = false;
        AstralParty_MoonPropTranscendenceSafeTurnCount = 0;

        var osty = Owner?.Osty;
        var recordedSummon = osty is { IsAlive: true } ? osty.CurrentHp : 0m;
        SetRecordedSummonAmount(recordedSummon);
        RefreshDynamicState();
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return Task.CompletedTask;
        if (result.UnblockedDamage <= 0m)
            return Task.CompletedTask;

        AstralParty_MoonPropTranscendenceDamagedThisTurn = true;
        AstralParty_MoonPropTranscendenceSafeTurnCount = 0;
        RefreshDynamicState();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        if (!AstralParty_MoonPropTranscendenceDamagedThisTurn)
            AstralParty_MoonPropTranscendenceSafeTurnCount++;

        var shouldHealOsty = AstralParty_MoonPropTranscendenceSafeTurnCount >= 4;
        AstralParty_MoonPropTranscendenceDamagedThisTurn = false;
        RefreshDynamicState();

        var osty = Owner?.Osty;
        if (!shouldHealOsty || osty is not { IsAlive: true })
            return;

        var healAmount = osty.MaxHp * OstyHealRatio;
        if (healAmount <= 0m)
            return;

        Flash();
        await CreatureCmd.Heal(osty, healAmount, true);
    }

    protected override Task AfterStacksChangedAsync()
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;

        return GrantMissingStacksAsync();
    }

    private decimal GetBaseMaxHpSnapshot()
    {
        return StableNumericStateHelper.DeserializeDecimal(AstralParty_MoonPropTranscendenceBaseMaxHpSnapshotSerialized);
    }

    private decimal EnsureBaseMaxHpSnapshot(Creature creature)
    {
        var snapshot = GetBaseMaxHpSnapshot();
        if (snapshot > 0m)
            return snapshot;

        snapshot = creature.MaxHp;
        AstralParty_MoonPropTranscendenceBaseMaxHpSnapshotSerialized = StableNumericStateHelper.SerializeDecimal(snapshot);
        return snapshot;
    }

    private decimal GetBaseMaxHpBonusRatio()
    {
        return BaseMaxHpBonusRatio + ExtraStackMaxHpBonusRatio * Math.Max(0, GetStacks() - 1);
    }

    private async Task GrantMissingStacksAsync()
    {
        if (Owner?.Creature == null)
            return;

        var snapshot = EnsureBaseMaxHpSnapshot(Owner.Creature);
        var totalDesiredBonus = snapshot * GetBaseMaxHpBonusRatio();
        var perGrantedStackBonus = snapshot * ExtraStackMaxHpBonusRatio;
        var alreadyGrantedBonus = AstralParty_MoonPropTranscendenceGrantedStacks switch
        {
            <= 0 => 0m,
            1 => snapshot * BaseMaxHpBonusRatio,
            _ => snapshot * BaseMaxHpBonusRatio + snapshot * ExtraStackMaxHpBonusRatio * (AstralParty_MoonPropTranscendenceGrantedStacks - 1)
        };

        var missingBonus = totalDesiredBonus - alreadyGrantedBonus;
        if (missingBonus <= 0m || snapshot <= 0m)
        {
            RefreshDynamicState();
            return;
        }

        AstralParty_MoonPropTranscendenceGrantedStacks = GetStacks();
        RefreshDynamicState();
        await CreatureCmd.GainMaxHp(Owner.Creature, missingBonus);
    }

    private decimal GetRecordedSummonAmount()
    {
        return StableNumericStateHelper.DeserializeDecimal(AstralParty_MoonPropTranscendenceRecordedSummonSerialized);
    }

    private void SetRecordedSummonAmount(decimal amount)
    {
        AstralParty_MoonPropTranscendenceRecordedSummonSerialized =
            StableNumericStateHelper.SerializeDecimal(Math.Max(0m, amount));
    }

    private string GetBaseMaxHpBonusPercentText()
    {
        return FormatPercent(GetBaseMaxHpBonusRatio());
    }

    private string GetOstyHealPercentText()
    {
        return FormatPercent(OstyHealRatio);
    }

    private string GetRecordedSummonText()
    {
        return FormatValue(GetRecordedSummonAmount());
    }

    private static decimal GetOpeningSummonCap(Creature creature)
    {
        decimal capBase = creature.MaxHp;
        if (MoonPropShapedGlassHelper.HasActiveCap(creature))
            capBase = Math.Min(capBase, MoonPropShapedGlassHelper.GetCurrentHpCap(creature));

        return Math.Max(0m, capBase - 1m);
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("BaseMaxHpBonusPercent", GetBaseMaxHpBonusPercentText());
        SetDynamicString("OstyHealPercent", GetOstyHealPercentText());
        SetDynamicString("RecordedSummon", GetRecordedSummonText());
    }
}
