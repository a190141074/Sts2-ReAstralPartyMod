using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

[RegisterMonster]
public sealed class AbyssalTentacleMonster : ModMonsterTemplate
{
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int AstralParty_AbyssalTentacleIndex { get; set; }

    public override LocString Title => MonsterModel.L10NMonsterLookup("RE_ASTRAL_PARTY_MOD_MONSTER_ABYSSAL_TENTACLE.name");

    public override int MinInitialHp => 1;

    public override int MaxInitialHp => 1;

    public override bool CanChangeScale => true;

    public override bool HasDeathSfx => false;

    public override bool HasHurtSfx => false;

    public override bool IsHealthBarVisible => false;

    public override string? CustomVisualsPath => SceneHelper.GetScenePath("creature_visuals/osty");

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var idle = new MoveState("NOTHING", _ => Task.CompletedTask, new HiddenIntent());
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine([idle], idle);
    }
}

public static class AbyssalTentacleCombatHelper
{
    private static readonly AsyncLocal<IsolatedDamageContext?> _isolatedDamageContext = new();

    public static async Task<Creature?> SummonAsync(Player owner, CardModel? source)
    {
        var combatState = owner.Creature.CombatState;
        if (combatState == null || owner.Creature.IsDead)
            return null;

        var model = (AbyssalTentacleMonster)ModelDb.Monster<AbyssalTentacleMonster>().ToMutable();
        model.AstralParty_AbyssalTentacleIndex = GetLivingTentacles(owner).Count;
        var tentacle = combatState.CreateCreature(model, owner.Creature.Side, slot: null);
        await PlayerCmd.AddPet(tentacle, owner);
        await MegaCrit.Sts2.Core.Commands.CreatureCmd.SetMaxHp(tentacle, 1m);
        await MegaCrit.Sts2.Core.Commands.CreatureCmd.SetCurrentHp(tentacle, 1m);
        await PowerCmd.Apply<AbyssalTentaclesPower>(tentacle, 1m, owner.Creature, source, false);
        await RefreshOwnerTentacleCountAsync(owner, source);
        PositionTentacles(owner);
        return tentacle;
    }

    public static IReadOnlyList<Creature> GetLivingTentacles(Player owner)
    {
        return owner.Creature.Pets
            .Where(creature => creature.IsAlive && creature.Monster is AbyssalTentacleMonster)
            .OrderBy(creature => ((AbyssalTentacleMonster)creature.Monster!).AstralParty_AbyssalTentacleIndex)
            .ThenBy(creature => creature.CombatId ?? uint.MaxValue)
            .ToList();
    }

    public static async Task ActivateAllAsync(PlayerChoiceContext choiceContext, Player owner, CardModel? source)
    {
        await RefreshOwnerTentacleCountAsync(owner, source);
        foreach (var tentacle in GetLivingTentacles(owner).ToList())
        {
            var targets = CombatTargetOrdering.GetLivingOpponentsStable(owner.Creature)
                .Where(target => target.GetPower<AbyssalRealmPower>() != null)
                .ToList();
            if (targets.Count == 0 || !tentacle.IsAlive)
                continue;

            var target = targets[owner.RunState.Rng.CombatTargets.NextInt(targets.Count)];
            await AttackAsync(choiceContext, owner, tentacle, target, source);
        }
    }

    public static async Task AttackAsync(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature tentacle,
        Creature target,
        CardModel? source)
    {
        if (!tentacle.IsAlive || !target.IsAlive || tentacle.Monster is not AbyssalTentacleMonster)
            return;

        var damage = 1m + decimal.Floor(Math.Max(0m, target.GetPowerAmount<AbyssalErosionMarkPower>()) / 3m);
        AnimateAttack(tentacle, target);
        using (BeginIsolatedDamage(target, damage, owner.Creature, source))
        {
            // A custom pet cannot complete Osty's built-in attacker animation await chain.
            // Keep its visual lunge independent, then resolve damage through the normal creature command.
            await MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage(
                choiceContext,
                target,
                damage,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim,
                owner.Creature,
                source);
        }

        if (target.IsAlive && target.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>() < 0m)
            await PowerCmd.Apply<AbyssalErosionMarkPower>(target, 1m, owner.Creature, source, false);
    }

    public static void PositionTentacle(Creature tentacle)
    {
        if (tentacle.PetOwner != null)
            PositionTentacles(tentacle.PetOwner);
    }

    public static void PositionTentacles(Player owner)
    {
        PositionTentaclesNow(owner);
        Callable.From(() => PositionTentaclesNow(owner)).CallDeferred();
    }

    public static async Task CleanupAsync(Player owner)
    {
        var displayPower = owner.Creature.GetPower<AbyssalTentaclesPower>();
        if (displayPower != null)
            await PowerCmd.Remove(displayPower);

        var room = NCombatRoom.Instance;
        if (room == null)
            return;

        foreach (var tentacle in owner.Creature.Pets.Where(creature => creature.Monster is AbyssalTentacleMonster).ToList())
        {
            var node = room.GetCreatureNode(tentacle);
            if (node == null)
                continue;

            node.ToggleIsInteractable(false);
            node.IntentContainer.Visible = false;
            node.Hide();
        }
    }

    private static async Task RefreshOwnerTentacleCountAsync(Player owner, CardModel? source)
    {
        var count = GetLivingTentacles(owner).Count;
        var displayPower = owner.Creature.GetPower<AbyssalTentaclesPower>();
        if (count <= 0)
        {
            if (displayPower != null)
                await PowerCmd.Remove(displayPower);
            return;
        }

        if (displayPower?.Amount == count)
            return;

        await PowerCmd.SetAmount<AbyssalTentaclesPower>(owner.Creature, count, owner.Creature, source);
    }

    private static void PositionTentaclesNow(Player owner)
    {
        var room = NCombatRoom.Instance;
        var ownerNode = room?.GetCreatureNode(owner.Creature);
        if (room == null || ownerNode == null)
            return;

        foreach (var tentacle in GetLivingTentacles(owner))
        {
            if (tentacle.Monster is not AbyssalTentacleMonster model)
                continue;
            var node = room.GetCreatureNode(tentacle);
            if (node == null)
                continue;

            var offset = GetOffset(model.AstralParty_AbyssalTentacleIndex);
            var parent = node.GetParent();
            if (parent == ownerNode.GetParent())
                parent.MoveChild(node, Math.Max(0, ownerNode.GetIndex()));

            node.Show();
            node.Modulate = Colors.White;
            node.Visuals.Show();
            node.Visuals.Modulate = new Color(1f, 1f, 1f, 0.62f);
            node.Position = ownerNode.Position + offset;
            node.SetDefaultScaleTo(0.54f, 0f);
            node.ToggleIsInteractable(false);
            node.Visuals.Bounds.Visible = false;
            node.IntentContainer.Visible = false;
        }
    }

    private static void AnimateAttack(Creature tentacle, Creature target)
    {
        var room = NCombatRoom.Instance;
        var tentacleNode = room?.GetCreatureNode(tentacle);
        var targetNode = room?.GetCreatureNode(target);
        if (tentacleNode == null || targetNode == null)
            return;

        var start = tentacleNode.Position;
        var direction = targetNode.Position - start;
        if (direction.LengthSquared() < 1f)
        {
            tentacleNode.AnimShake();
            return;
        }

        var tween = tentacleNode.CreateTween();
        var lunge = start + direction.Normalized() * 55f;
        tween.TweenProperty(tentacleNode, "position", lunge, 0.08f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(tentacleNode, "position", start, 0.14f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private static Vector2 GetOffset(int index)
    {
        var column = index % 4;
        var row = index / 4;
        return new Vector2(92f + column * 66f, -72f + (column % 2) * 112f - row * 54f);
    }

    public static bool IsIsolatedDamage(
        Creature? target,
        decimal amount,
        Creature? dealer,
        CardModel? cardSource)
    {
        var context = _isolatedDamageContext.Value;
        return context != null
               && ReferenceEquals(context.Target, target)
               && ReferenceEquals(context.Dealer, dealer)
               && ReferenceEquals(context.CardSource, cardSource)
               && context.RawDamage == amount;
    }

    private static IDisposable BeginIsolatedDamage(
        Creature target,
        decimal rawDamage,
        Creature dealer,
        CardModel? cardSource)
    {
        var previous = _isolatedDamageContext.Value;
        _isolatedDamageContext.Value = new IsolatedDamageContext(target, rawDamage, dealer, cardSource);
        return new IsolatedDamageScope(previous);
    }

    private sealed class IsolatedDamageScope : IDisposable
    {
        private readonly IsolatedDamageContext? _previous;

        public IsolatedDamageScope(IsolatedDamageContext? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            _isolatedDamageContext.Value = _previous;
        }
    }

    private sealed record IsolatedDamageContext(
        Creature Target,
        decimal RawDamage,
        Creature Dealer,
        CardModel? CardSource);
}
