using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class KingsPowerStrengthPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public int RemainingTurns = 3;
        public bool GrantedThisTurn;
        public decimal AppliedStrength;
    }

    private const decimal StrengthAmount = 5m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override bool IsInstanced => true;

    public override int DisplayAmount => GetInternalData<Data>().RemainingTurns;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await GrantStrength(applier, cardSource);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Player != player)
            return;

        await GrantStrength(Owner, null);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        var data = GetInternalData<Data>();
        if (data.AppliedStrength > 0m)
            await PowerCmd.Apply<StrengthPower>(Owner, -data.AppliedStrength, Owner, null, true);

        data.AppliedStrength = 0m;
        data.GrantedThisTurn = false;
        data.RemainingTurns--;
        if (data.RemainingTurns <= 0)
            await PowerCmd.Remove(this);
    }

    private async Task GrantStrength(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        if (data.RemainingTurns <= 0 || data.GrantedThisTurn)
            return;

        data.GrantedThisTurn = true;
        data.AppliedStrength = StrengthAmount;
        await PowerCmd.Apply<StrengthPower>(Owner, StrengthAmount, applier, cardSource, true);
    }
}
