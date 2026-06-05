using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class EnigmaticSynthesisHeavenScroll : AstralPartyRelicModel
{
    private const int BaseCombatWindowFloors = 4;
    private const int SevenCursesCombatWindowFloors = 8;
    private const decimal FlutterAmount = 16m;

    [SavedProperty] public int AstralParty_EnigmaticSynthesisHeavenScrollActiveUntilFloor { get; set; }

    protected override string RelicId => "enigmatic_synthesis_heaven_scroll";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount
    {
        get
        {
            if (Owner?.RunState == null)
                return Math.Max(0, AstralParty_EnigmaticSynthesisHeavenScrollActiveUntilFloor);

            return Math.Max(0, AstralParty_EnigmaticSynthesisHeavenScrollActiveUntilFloor - Owner.RunState.TotalFloor);
        }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null || !IsWindowActive())
            return;

        await PowerCmd.Apply<FlutterPower>(Owner.Creature, FlutterAmount, Owner.Creature, null, false);
        Flash();
    }

    internal void OnRestSiteOptionResolved(bool usedSmith)
    {
        if (Owner?.RunState == null)
            return;

        if (usedSmith)
        {
            ClearWindow();
            return;
        }

        AstralParty_EnigmaticSynthesisHeavenScrollActiveUntilFloor = Owner.RunState.TotalFloor + GetCombatWindowFloors();
        InvokeDisplayAmountChanged();
        Flash();
    }

    private bool IsWindowActive()
    {
        return Owner?.RunState != null
               && AstralParty_EnigmaticSynthesisHeavenScrollActiveUntilFloor >= Owner.RunState.TotalFloor;
    }

    private void ClearWindow()
    {
        if (AstralParty_EnigmaticSynthesisHeavenScrollActiveUntilFloor <= 0)
            return;

        AstralParty_EnigmaticSynthesisHeavenScrollActiveUntilFloor = 0;
        InvokeDisplayAmountChanged();
        Flash();
    }

    private int GetCombatWindowFloors()
    {
        return Owner?.GetRelic<EnigmaticSevenCurses>() != null
            ? SevenCursesCombatWindowFloors
            : BaseCombatWindowFloors;
    }
}
