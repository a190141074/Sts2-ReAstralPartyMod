using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using STS2RitsuLib;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class TerraprismaHelper
{
    internal static PileType MountedPileType { get; private set; }

    private static readonly MethodInfo? UpdateHoverTipMethod = typeof(NSovereignBladeVfx).GetMethod(
        "UpdateHoverTip",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly HashSet<CardModel> AutoPlayingBlades = new(CardReferenceComparer.Instance);
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        MountedPileType = ModCardPileRegistry.For(MainFile.ModId)
            .RegisterOwned("terraprisma_mounted", new ModCardPileSpec
            {
                Scope = ModCardPileScope.CombatOnly,
                Style = ModCardPileUiStyle.Headless,
                CardShouldBeVisible = false
            })
            .PileType;

        RitsuLibFramework.GetContentRegistry(MainFile.ModId)
            .RegisterModelCapability<TerraprismaLoadedBladeCapability>();
        _registered = true;
    }

    public static async Task<bool> CreateLoadedBladeAsync(
        Player owner,
        CardModel source,
        decimal baseDamage)
    {
        if (owner.Creature is not { } creature
            || creature.CombatState == null
            || creature.IsDead)
            return false;

        var mountedPower = await TerraprismaMountedPower.GetOrCreate(owner);
        if (mountedPower == null)
            return false;

        var blade = await CreateMountedBladeAsync(owner, source, baseDamage);
        if (blade == null)
            return false;

        MainFile.Logger.Info(
            $"[Terraprisma] mounted blade registered in official custom pile | owner={owner.NetId} | " +
            $"source={source.Id.Entry} | damage={baseDamage} | mountedCount={GetMountedBlades(owner).Count} | " +
            $"bladePile={blade.Pile?.Type.ToString() ?? "None"}.");
        return true;
    }

    internal static async Task AutoPlayMountedBladesAsync(
        PlayerChoiceContext choiceContext,
        Player owner)
    {
        if (owner.Creature?.IsDead != false || owner.Creature.CombatState == null)
            return;

        var mountedPile = MountedPileType.GetPile(owner);
        if (mountedPile == null)
        {
            MainFile.Logger.Error($"[Terraprisma] mounted pile unavailable at turn end | owner={owner.NetId}.");
            return;
        }

        var blades = GetMountedBlades(owner);
        MainFile.Logger.Info(
            $"[Terraprisma] mounted pile turn-end scan | owner={owner.NetId} | mounted={blades.Count}.");

        foreach (var blade in blades)
        {
            if (blade.Owner != owner || !TryEnterAutoPlay(blade))
                continue;

            try
            {
                var target = FindNearestLivingEnemy(owner);
                if (target == null)
                    continue;

                MainFile.Logger.Info(
                    $"[Terraprisma] mounted blade moving to play pile | owner={owner.NetId} | " +
                    $"card={blade.Id.Entry} | oldPile={blade.Pile?.Type.ToString() ?? "None"} | " +
                    $"target={target.CombatId?.ToString() ?? target.ModelId.ToString()}.");
                await CardPileCmd.Add(blade, PileType.Play, CardPilePosition.Top, null, true);
                if (blade.Pile?.Type != PileType.Play)
                {
                    MainFile.Logger.Error(
                        $"[Terraprisma] mounted blade autoplay aborted: official play-pile add failed | " +
                        $"owner={owner.NetId} | card={blade.Id.Entry} | " +
                        $"pile={blade.Pile?.Type.ToString() ?? "None"}.");
                    continue;
                }

                await CardCmd.AutoPlay(
                    choiceContext,
                    blade,
                    target,
                    AutoPlayType.Default,
                    true,
                    true);

                await EnsureMountedPileAsync(blade, mountedPile);
                MainFile.Logger.Info(
                    $"[Terraprisma] mounted blade autoplay completed | owner={owner.NetId} | " +
                    $"card={blade.Id.Entry} | resultPile={blade.Pile?.Type.ToString() ?? "None"}.");
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error(
                    $"[Terraprisma] mounted blade autoplay failed | owner={owner.NetId} | card={blade.Id.Entry} | " +
                    $"pile={blade.Pile?.Type.ToString() ?? "None"} | error={ex}");
                try
                {
                    await EnsureMountedPileAsync(blade, mountedPile);
                }
                catch (Exception recoveryEx)
                {
                    MainFile.Logger.Error(
                        $"[Terraprisma] mounted blade recovery failed | owner={owner.NetId} | " +
                        $"card={blade.Id.Entry} | pile={blade.Pile?.Type.ToString() ?? "None"} | error={recoveryEx}");
                }
            }
            finally
            {
                ExitAutoPlay(blade);
            }
        }
    }

    private static async Task<TerraprismaCard?> CreateMountedBladeAsync(
        Player owner,
        CardModel source,
        decimal baseDamage)
    {
        var combatState = owner.Creature?.CombatState;
        if (combatState == null)
            return null;

        var blade = combatState.CreateCard<TerraprismaCard>(owner);
        blade.GetOrCreateCapability<TerraprismaLoadedBladeCapability>()
            .Initialize(blade, source, baseDamage);

        var result = await CardPileCmd.AddGeneratedCardToCombat(
            blade,
            MountedPileType,
            owner,
            CardPilePosition.Bottom);
        if (!result.success || blade.Pile?.Type != MountedPileType)
        {
            MainFile.Logger.Error(
                $"[Terraprisma] official mounted pile registration failed | owner={owner.NetId} | " +
                $"success={result.success} | pile={blade.Pile?.Type.ToString() ?? "None"}.");
            return null;
        }

        MainFile.Logger.Info(
            $"[Terraprisma] blade entered official mounted pile | owner={owner.NetId} | " +
            $"card={blade.Id.Entry} | pile={blade.Pile?.Type.ToString() ?? "None"}.");
        await CreateMountedBladeVfxAsync(owner, blade);
        MainFile.Logger.Info(
            $"[Terraprisma] official blade mounted | owner={owner.NetId} | card={blade.Id.Entry} | " +
            $"pile={blade.Pile?.Type.ToString() ?? "None"} | allCards={owner.PlayerCombatState?.AllCards.ToList().Count ?? 0}.");
        return blade;
    }

    internal static IReadOnlyList<TerraprismaCard> GetMountedBlades(Player owner)
    {
        var mountedPile = MountedPileType.GetPile(owner);
        return mountedPile?.Cards
            .OfType<TerraprismaCard>()
            .Where(blade => blade.Owner == owner && IsMountedBlade(blade))
            .ToArray() ?? [];
    }

    public static bool IsMountedBlade(CardModel? card)
    {
        return card is TerraprismaCard
               && card.TryGetCapability<TerraprismaLoadedBladeCapability>(out _);
    }

    public static bool IsTerraprismaBlade(CardModel? card)
    {
        return IsMountedBlade(card);
    }

    public static bool TryGetLoadedDamage(CardModel card, out decimal damage)
    {
        if (card.TryGetCapability<TerraprismaLoadedBladeCapability>(out var capability))
        {
            damage = capability.BaseDamage;
            return true;
        }

        damage = 0m;
        return false;
    }

    internal static NSovereignBladeVfx? GetVfxNode(Player owner, TerraprismaCard card)
    {
        return NCombatRoom.Instance?.GetCreatureNode(owner.Creature)?.GetChildren()
            .OfType<NSovereignBladeVfx>()
            .FirstOrDefault(vfx => vfx.Card == card);
    }

    private static async Task CreateMountedBladeVfxAsync(Player owner, TerraprismaCard card)
    {
        var ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner.Creature);
        var vfxNode = NSovereignBladeVfx.Create(card);
        if (ownerNode == null || vfxNode == null)
        {
            MainFile.Logger.Error(
                $"[Terraprisma] VFX creation failed | owner={owner.NetId} | card={card.Id.Entry} | " +
                $"ownerNode={ownerNode != null} | vfxNode={vfxNode != null}.");
            return;
        }

        ownerNode.AddChildSafely(vfxNode);
        for (var frame = 0; frame < 8 && GodotObject.IsInstanceValid(vfxNode) && !vfxNode.IsInsideTree(); frame++)
            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        if (!GodotObject.IsInstanceValid(vfxNode) || !vfxNode.IsInsideTree())
        {
            MainFile.Logger.Error(
                $"[Terraprisma] VFX did not enter scene tree | owner={owner.NetId} | card={card.Id.Entry}.");
            return;
        }

        try
        {
            // NSovereignBladeVfx starts hidden at scale zero. This fixed zero-damage
            // call only reveals the base model; no forge command or card state is used.
            vfxNode.Forge(0f, false);
            if (UpdateHoverTipMethod == null)
                MainFile.Logger.Warn("[Terraprisma] NSovereignBladeVfx.UpdateHoverTip was not found.");
            else
                UpdateHoverTipMethod.Invoke(vfxNode, null);
            MainFile.Logger.Info(
                $"[Terraprisma] VFX mounted with independent card identity | owner={owner.NetId} | " +
                $"card={card.Id.Entry} | title={card.Title} | sourceTips={card.HoverTips.Count()}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"[Terraprisma] VFX initialization failed | owner={owner.NetId} | card={card.Id.Entry} | error={ex}");
        }
    }

    private static async Task EnsureMountedPileAsync(TerraprismaCard blade, CardPile mountedPile)
    {
        if (blade.Pile == mountedPile)
            return;

        await CardPileCmd.Add(blade, mountedPile, CardPilePosition.Bottom, null, true);
        if (blade.Pile != mountedPile)
            throw new InvalidOperationException(
                $"Official mounted pile recovery did not complete for {blade.Id.Entry}; " +
                $"pile={blade.Pile?.Type.ToString() ?? "None"}.");
    }

    public static Creature? FindNearestLivingEnemy(Player owner)
    {
        var ownerCreature = owner.Creature;
        if (ownerCreature?.CombatState == null)
            return null;

        var candidates = ownerCreature.CombatState
            .GetOpponentsOf(ownerCreature)
            .Where(static creature => creature.IsAlive)
            .OrderBy(static creature => creature.CombatId ?? uint.MaxValue)
            .ThenBy(static creature => creature.ModelId.ToString(), StringComparer.Ordinal)
            .ThenBy(static creature => creature.SlotName ?? string.Empty, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
            return null;

        var room = NCombatRoom.Instance;
        var ownerNode = room?.GetCreatureNode(ownerCreature);
        if (ownerNode == null)
            return candidates[0];

        return candidates
            .Select(enemy => new { Enemy = enemy, Node = room?.GetCreatureNode(enemy) })
            .OrderBy(candidate => candidate.Node == null
                ? float.MaxValue
                : ownerNode.GlobalPosition.DistanceSquaredTo(candidate.Node.GlobalPosition))
            .ThenBy(candidate => candidate.Enemy.CombatId ?? uint.MaxValue)
            .ThenBy(candidate => candidate.Enemy.ModelId.ToString(), StringComparer.Ordinal)
            .Select(candidate => candidate.Enemy)
            .FirstOrDefault();
    }

    private static bool TryEnterAutoPlay(CardModel card)
    {
        lock (AutoPlayingBlades)
        {
            return AutoPlayingBlades.Add(card);
        }
    }

    private static void ExitAutoPlay(CardModel card)
    {
        lock (AutoPlayingBlades)
        {
            AutoPlayingBlades.Remove(card);
        }
    }

    private sealed class CardReferenceComparer : IEqualityComparer<CardModel>
    {
        public static CardReferenceComparer Instance { get; } = new();

        public bool Equals(CardModel? x, CardModel? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(CardModel obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}

[RegisterModelCapability]
public sealed class TerraprismaLoadedBladeCapability : StatefulModelCapability<CardModel, TerraprismaLoadedBladeState>,
    ICardHoverTipContributor,
    ICardPlayResultContributor
{
    public decimal BaseDamage => State.BaseDamage;

    public void Initialize(TerraprismaCard card, CardModel source, decimal baseDamage)
    {
        InitializeSnapshot(source.ToSerializable(), baseDamage);
        card.DynamicVars.Damage.BaseValue = baseDamage;
    }

    public void InitializeSnapshot(SerializableCard sourceSnapshot, decimal baseDamage)
    {
        MutateState(state =>
        {
            state.SourceSnapshot = sourceSnapshot;
            state.BaseDamage = baseDamage;
        });
    }

    public IEnumerable<IHoverTip> GetHoverTips(CardModel card)
    {
        return GetSourceHoverTips();
    }

    public IEnumerable<IHoverTip> GetSourceHoverTips()
    {
        if (State.SourceSnapshot == null)
            return [];

        var preview = CardModel.FromSerializable(State.SourceSnapshot);
        return [HoverTipFactory.FromCard(preview)];
    }

    public PileType? GetResultPileTypeForCardPlay(CardModel card)
    {
        return TerraprismaHelper.MountedPileType;
    }
}

public sealed class TerraprismaLoadedBladeState
{
    public SerializableCard? SourceSnapshot { get; set; }
    public decimal BaseDamage { get; set; }
}
