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
using MegaCrit.Sts2.Core.Nodes.Combat;
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

    public override string? CustomVisualsPath => SceneHelper.GetScenePath("creature_visuals/fallback");

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        var osty = ModelDb.Monsters.FirstOrDefault(monster =>
            string.Equals(monster.Id.Entry, "osty", StringComparison.OrdinalIgnoreCase));
        return osty?.CreateVisuals();
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var idle = new MoveState("NOTHING", _ => Task.CompletedTask, new HiddenIntent());
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine([idle], idle);
    }
}

public static class AbyssalTentacleCombatHelper
{
    private const string AttackAnimation = "Attack";
    private static readonly AsyncLocal<int> IsolatedDamageDepth = new();

    public static bool IsIsolatedDamageActive => IsolatedDamageDepth.Value > 0;

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
        if (!tentacle.IsAlive || !target.IsAlive || tentacle.Monster is not AbyssalTentacleMonster model)
            return;

        var damage = 1m + decimal.Floor(Math.Max(0m, target.GetPowerAmount<AbyssalErosionMarkPower>()) / 3m);
        using (BeginIsolatedDamage())
        {
            await DamageCmd.Attack(damage)
                .FromMonster(model)
                .Targeting(target)
                .Unpowered()
                .WithAttackerAnim(AttackAnimation, 0.18f, tentacle)
                .Execute(choiceContext);
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

    public static void Cleanup(Player owner)
    {
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
            node.Show();
            node.Visuals.Show();
            node.Position = ownerNode.Position + offset;
            node.SetDefaultScaleTo(0.54f, 0f);
            node.ToggleIsInteractable(false);
            node.Visuals.Bounds.Visible = false;
            node.IntentContainer.Visible = false;
        }
    }

    private static Vector2 GetOffset(int index)
    {
        var column = index % 4;
        var row = index / 4;
        return new Vector2(92f + column * 66f, -72f + (column % 2) * 112f - row * 54f);
    }

    private static IDisposable BeginIsolatedDamage()
    {
        IsolatedDamageDepth.Value++;
        return new IsolatedDamageScope();
    }

    private sealed class IsolatedDamageScope : IDisposable
    {
        public void Dispose()
        {
            IsolatedDamageDepth.Value = Math.Max(0, IsolatedDamageDepth.Value - 1);
        }
    }
}
