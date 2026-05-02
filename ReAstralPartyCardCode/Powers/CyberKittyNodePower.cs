using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class CyberKittyNodePower : AstralPartyPowerModel
{
    private const int DefaultNodeValue = 4;
    private const int MinimumNodeValue = 1;
    private const int MaximumNodeValue = 10;
    private const float PaddingDistanceFromMonster = 450f;
    private const float PaddingDistanceFromOriginal = 50f;
    private const float TweenTime = 0.25f;

    private sealed class Data
    {
        public int InitialAmount;
        public float InitialTargetPosition;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromCard<SkillMudTruckCrash>()
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || TestMode.IsOn || ShouldSuppressVisualOffset())
            return Task.CompletedTask;

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null)
            return Task.CompletedTask;

        var data = GetInternalData<Data>();
        data.InitialAmount = Math.Clamp(Amount, MinimumNodeValue, MaximumNodeValue);
        data.InitialTargetPosition = creatureNode.GlobalPosition.X;
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this)
            return;

        if (TestMode.IsOn || ShouldSuppressVisualOffset())
            return;

        await UpdateCreaturePosition();
    }

    private bool ShouldSuppressVisualOffset()
    {
        var roomType = Owner?.Player?.RunState?.CurrentRoom?.RoomType;
        return roomType == RoomType.Boss;
    }

    private async Task UpdateCreaturePosition()
    {
        if (Owner == null || NCombatRoom.Instance == null)
            return;

        var ownerNode = NCombatRoom.Instance.GetCreatureNode(Owner);
        if (ownerNode == null)
            return;

        var data = GetInternalData<Data>();
        if (data.InitialAmount <= 0)
        {
            data.InitialAmount = DefaultNodeValue;
            data.InitialTargetPosition = ownerNode.GlobalPosition.X;
        }

        var anchorX = ownerNode.GlobalPosition.X - PaddingDistanceFromMonster;
        var upperBoundX = data.InitialTargetPosition + PaddingDistanceFromOriginal;
        var slotSpacing = (upperBoundX - anchorX) / data.InitialAmount;
        var clampedAmount = Math.Clamp(Amount, MinimumNodeValue, MaximumNodeValue);
        var mirroredAmount = MaximumNodeValue - clampedAmount + MinimumNodeValue;
        var usableAmount = Math.Min(mirroredAmount, data.InitialAmount);
        var overflowAmount = Math.Max(mirroredAmount - data.InitialAmount, 0);
        var targetX = ownerNode.GlobalPosition.X - 400f
                      + slotSpacing * usableAmount
                      + slotSpacing * (overflowAmount / (overflowAmount + 2f));
        var playerNode = NCombatRoom.Instance.GetCreatureNode(Owner);
        if (playerNode == null || Math.Abs(targetX - playerNode.GlobalPosition.X) <= 5f)
            return;

        var tween = NCombatRoom.Instance.CreateTween()
            .SetParallel()
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);

        tween.TweenProperty(playerNode, "global_position:x", targetX, TweenTime);
        await tween.ToSignal(tween, Tween.SignalName.Finished);
    }
}
