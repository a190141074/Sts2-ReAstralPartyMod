using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class SpeedRollerRelicBase : AstralPartyRelicModel
{
    protected abstract decimal CombatStartDexterityBonus { get; }
    protected abstract int FloorsPerFlight { get; }

    [SavedProperty] public int AstralParty_SpeedRollerFloorProgress { get; set; }
    [SavedProperty] public int AstralParty_SpeedRollerFlightCharges { get; set; }

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override Task AfterObtained()
    {
        AstralParty_SpeedRollerFloorProgress = 0;
        AstralParty_SpeedRollerFlightCharges = 0;
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (FloorsPerFlight <= 0 || Owner?.RunState == null)
            return Task.CompletedTask;

        AstralParty_SpeedRollerFloorProgress++;
        if (AstralParty_SpeedRollerFloorProgress < FloorsPerFlight)
            return Task.CompletedTask;

        AstralParty_SpeedRollerFloorProgress %= FloorsPerFlight;
        AstralParty_SpeedRollerFlightCharges++;
        Flash();
        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;
        if (CombatStartDexterityBonus <= 0m)
            return;

        Flash();
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, CombatStartDexterityBonus, Owner.Creature, null);
    }

    public bool HasFlightCharge()
    {
        return AstralParty_SpeedRollerFlightCharges > 0;
    }

    public bool TryConsumeFlightChargeForDestination(MapCoord destination)
    {
        if (!HasFlightCharge() || Owner?.RunState is not RunState runState || runState.Map == null)
            return false;

        if (!IsFlightDestination(runState.Map, runState.VisitedMapCoords, destination))
            return false;

        AstralParty_SpeedRollerFlightCharges--;
        return true;
    }

    public static bool IsFlightDestination(ActMap map, IReadOnlyList<MapCoord> visitedMapCoords, MapCoord destination)
    {
        if (visitedMapCoords.Count == 0)
            return false;

        var currentCoord = visitedMapCoords[^1];
        if (destination.row != currentCoord.row + 1)
            return false;

        var currentPoint = map.GetPoint(currentCoord);
        var destinationPoint = map.GetPoint(destination);
        if (currentPoint == null || destinationPoint == null)
            return false;

        return !currentPoint.Children.Any(child => child.coord.Equals(destination));
    }
}
