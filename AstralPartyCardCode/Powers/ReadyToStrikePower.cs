using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class ReadyToStrikePower : AstralPartyPowerModel
{
    private static readonly FieldInfo? DamagePropsField =
        typeof(AttackCommand).GetField("<DamageProps>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private sealed class Data
    {
        public bool IsReplacingCard;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromCard<Omnislice>()
    ];

    public override Task BeforeAttack(AttackCommand command)
    {
        if (Owner?.Player == null)
            return Task.CompletedTask;
        if (command.Attacker != Owner)
            return Task.CompletedTask;
        if (command.TargetSide == Owner.Side)
            return Task.CompletedTask;
        if (command.ModelSource is not CardModel cardSource)
            return Task.CompletedTask;
        if (cardSource.Owner != Owner.Player || cardSource.Type != CardType.Attack)
            return Task.CompletedTask;

        DamagePropsField?.SetValue(command, command.DamageProps | ValueProp.Unblockable);
        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!ShouldReplaceHandEntry(card, oldPileType))
            return;

        var data = GetInternalData<Data>();
        data.IsReplacingCard = true;
        try
        {
            Flash();
            await CardCmd.Discard(new ThrowingPlayerChoiceContext(), card);

            var omnislice = MidnightFlashHelper.CreateOmnisliceCard(Owner!.Player!);
            if (omnislice != null)
                await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(omnislice, true);
        }
        finally
        {
            data.IsReplacingCard = false;
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    private bool ShouldReplaceHandEntry(CardModel card, PileType oldPileType)
    {
        if (Owner?.Player == null)
            return false;
        if (GetInternalData<Data>().IsReplacingCard)
            return false;
        if (card.Owner != Owner.Player)
            return false;
        if (card.Pile?.Type != PileType.Hand || oldPileType == PileType.Hand)
            return false;

        return !MidnightFlashHelper.IsOmnisliceCard(card);
    }
}
