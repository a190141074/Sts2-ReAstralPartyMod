using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class ProblemStudentPower : AstralPartyPowerModel
{
    private const decimal DexterityBonus = 4m;
    private const decimal ExtraDamageTaken = 2m;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override async Task AfterApplied(Creature? applier, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (Owner == null)
            return;

        await PowerCmd.Apply<DexterityPower>(Owner, DexterityBonus, applier, cardSource, true);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m)
            return 0m;

        return ExtraDamageTaken;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (Owner == null || target != Owner)
            return;
        if (result.TotalDamage <= 0m || result.UnblockedDamage <= 0m)
            return;
        if (dealer == null || dealer.Side == Owner.Side)
            return;
        if (Owner.CombatState?.Encounter?.RoomType != MegaCrit.Sts2.Core.Rooms.RoomType.Boss)
            return;

        await PowerCmd.Apply<DexterityPower>(Owner, -DexterityBonus, Owner, null, true);
        await PowerCmd.Remove(this);
    }
}