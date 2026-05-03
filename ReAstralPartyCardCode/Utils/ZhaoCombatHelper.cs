using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class ZhaoCombatHelper
{
    private const float ChaseVisualExtraOffset = 300f;

    public static async Task<CardModel?> AutoPlayRandomAttackForZhao(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature target,
        decimal bonusDamage,
        AbstractModel source)
    {
        if (owner.Creature?.CombatState == null || !target.IsAlive)
            return null;

        var attackCard = FindRandomAttackCard(owner);
        if (attackCard == null)
            return null;

        var extraAttackPower = owner.Creature.GetPower<ExtraAttackPower>();
        if (extraAttackPower == null)
            return null;

        ApplyBygoneEffigyStyleChaseVisual(owner.Creature, target);
        extraAttackPower.BeginTriggeredAttack(attackCard, target, bonusDamage);
        try
        {
            await CardCmd.AutoPlay(choiceContext, attackCard, target, AutoPlayType.Default, false, true);
            return attackCard;
        }
        finally
        {
            extraAttackPower.EndTriggeredAttack(attackCard);
        }
    }

    private static void ApplyBygoneEffigyStyleChaseVisual(Creature? attacker, Creature target)
    {
        if (TestMode.IsOn || attacker == null || NCombatRoom.Instance == null)
            return;

        var attackerNode = NCombatRoom.Instance.GetCreatureNode(attacker);
        var targetNode = NCombatRoom.Instance.GetCreatureNode(target);
        if (attackerNode == null || targetNode == null)
            return;

        var specialNode = attackerNode.GetSpecialNode<Node2D>("Visuals/SpineBoneNode");
        if (specialNode == null)
            return;

        NCombatRoom.Instance.RadialBlur(VfxPosition.Left);

        var horizontalDistance = Mathf.Abs(targetNode.GlobalPosition.X - attackerNode.GlobalPosition.X);
        var offsetMagnitude = horizontalDistance + ChaseVisualExtraOffset;
        var attackerIsLeftOfTarget = attackerNode.GlobalPosition.X <= targetNode.GlobalPosition.X;

        // Reuse Bygone Effigy's pre-slash lunge, but mirror it based on battlefield side.
        specialNode.Position = (attackerIsLeftOfTarget ? Vector2.Left : Vector2.Right) * offsetMagnitude;
    }

    private static CardModel? FindRandomAttackCard(Player owner)
    {
        var combatState = owner.Creature?.CombatState;
        if (combatState == null)
            return null;

        var drawPileAttacks = PileType.Draw
            .GetPile(owner)
            .Cards
            .Where(card => card.Type == CardType.Attack)
            .ToList();
        if (drawPileAttacks.Count > 0)
            return drawPileAttacks[owner.RunState.Rng.CombatTargets.NextInt(drawPileAttacks.Count)];

        var discardPileAttacks = PileType.Discard
            .GetPile(owner)
            .Cards
            .Where(card => card.Type == CardType.Attack)
            .ToList();
        if (discardPileAttacks.Count > 0)
            return discardPileAttacks[owner.RunState.Rng.CombatTargets.NextInt(discardPileAttacks.Count)];

        return null;
    }
}
