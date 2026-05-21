using System.Collections.Generic;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PvzHyperTemporalNut : AstralPartyRelicModel
{
    private List<decimal> _hpSnapshots = [];

    [SavedProperty]
    private string AstralParty_PvzHyperTemporalNutHpSnapshotsJson
    {
        get => PvzNutRelicHelper.SerializeHpSnapshots(_hpSnapshots);
        set => _hpSnapshots = PvzNutRelicHelper.DeserializeHpSnapshots(value);
    }

    [SavedProperty] public bool AstralParty_PvzHyperTemporalNutUsedThisRun { get; set; }
    [SavedProperty] public bool AstralParty_PvzHyperTemporalNutFusionProcessed { get; set; }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFusionClueId)
    ];

    public override bool IsUsedUp => AstralParty_PvzHyperTemporalNutUsedThisRun;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        _hpSnapshots.Clear();
        AstralParty_PvzHyperTemporalNutUsedThisRun = false;
        AstralParty_PvzHyperTemporalNutFusionProcessed = false;
    }

    public override Task BeforeCombatStart()
    {
        _hpSnapshots.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return Task.CompletedTask;

        _hpSnapshots.Add(Owner.Creature.CurrentHp);
        while (_hpSnapshots.Count > 3)
            _hpSnapshots.RemoveAt(0);

        return Task.CompletedTask;
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner?.Creature)
            return true;
        if (AstralParty_PvzHyperTemporalNutUsedThisRun)
            return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (Owner?.Creature != creature)
            return;

        Flash();
        AstralParty_PvzHyperTemporalNutUsedThisRun = true;
        Status = RelicStatus.Disabled;

        var restoreAmount = ResolveRestoredHp(creature);
        MainFile.Logger.Info(
            $"[PvzHyperTemporalNut] Prevented death | owner={Owner?.NetId} | restoreHp={restoreAmount} | snapshots={_hpSnapshots.Count}");
        await CreatureCmd.Heal(creature, Math.Max(1m, restoreAmount - creature.CurrentHp));
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null || AstralParty_PvzHyperTemporalNutFusionProcessed)
            return;
        if (Owner.GetRelic<PvzUltimateHyperSpacetimeNut>() != null)
        {
            AstralParty_PvzHyperTemporalNutFusionProcessed = true;
            return;
        }
        if (!PvzNutRelicHelper.CanFuseUltimateNut(Owner, out var fusionRelics))
            return;

        AstralParty_PvzHyperTemporalNutFusionProcessed = true;
        MainFile.Logger.Info($"[PvzHyperTemporalNut] Fusing into ultimate nut | owner={Owner.NetId}");
        await PvzNutRelicHelper.MeltRelicsAsync(fusionRelics);
        await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(Owner, ModelDb.Relic<PvzUltimateHyperSpacetimeNut>());
    }

    private decimal ResolveRestoredHp(Creature creature)
    {
        if (_hpSnapshots.Count >= 3)
            return ClampToPositive(_hpSnapshots[0], creature.MaxHp);
        if (_hpSnapshots.Count == 2)
            return ClampToPositive(_hpSnapshots[0], creature.MaxHp);
        if (_hpSnapshots.Count == 1)
            return ClampToPositive(_hpSnapshots[0], creature.MaxHp);

        MainFile.Logger.Warn($"[PvzHyperTemporalNut] No snapshots available; falling back to 1 HP | owner={Owner?.NetId}");
        return 1m;
    }

    private static decimal ClampToPositive(decimal value, decimal maxHp)
    {
        return Math.Clamp(value, 1m, maxHp);
    }
}
