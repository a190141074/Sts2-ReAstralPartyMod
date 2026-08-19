using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Scaffolding.Combat;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class RoyalPrerogativePower : AstralPartyPowerModel
{
    private static bool _rightClickBindingRegistered;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => 0;

    public override bool ShouldReceiveCombatHooks => true;

    public static void RegisterRightClickBinding()
    {
        if (_rightClickBindingRegistered)
            return;

        ModRightClickRegistry.Register<CardModel>(
            MainFile.ModId,
            "royal_prerogative",
            MoveTemporaryCardToDrawTop,
            canHandleLocal: CanHandleRightClick,
            canExecute: CanExecuteRightClick);
        _rightClickBindingRegistered = true;
    }

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Player != player)
            return;

        await PowerCmd.Remove(this);
    }

    private static bool CanHandleRightClick(ModRightClickContext context)
    {
        return context.Model is CardModel card && IsEligible(context.Player, card, context.Trigger);
    }

    private static bool CanExecuteRightClick(ModRightClickExecutionContext context)
    {
        return context.Model is CardModel card && IsEligible(context.Player, card, context.Trigger);
    }

    private static async Task MoveTemporaryCardToDrawTop(ModRightClickExecutionContext context)
    {
        if (context.Model is not CardModel card || !IsEligible(context.Player, card, context.Trigger))
            return;

        var power = context.Player.Creature!.GetPower<RoyalPrerogativePower>();
        var result = await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, power);
        if (result.success)
            power!.Flash();
    }

    private static bool IsEligible(Player player, CardModel card, ModRightClickTrigger trigger)
    {
        return !trigger.IsController
               && trigger.Source == ModRightClickSource.HandCard
               && card.Owner == player
               && card.Pile?.Type == PileType.Hand
               && card.Keywords.Contains(AstralKeywords.AstralTemporary)
               && card.IsOwnerPlayPhase()
               && player.Creature?.GetPower<RoyalPrerogativePower>() != null;
    }
}
