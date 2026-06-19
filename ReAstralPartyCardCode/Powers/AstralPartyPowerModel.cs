using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower(Inherit = true)]
public abstract partial class AstralPartyPowerModel : ModPowerTemplate
{
    private const string MissingPowerIconPath = "res://images/powers/missing_power.png";
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    protected new virtual IEnumerable<IHoverTip> ExtraHoverTips => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => ExtraHoverTips;

    public override PowerAssetProfile AssetProfile => new()
    {
        IconPath = ResolveIconPath(),
        BigIconPath = ResolveIconPath()
    };

    public static string GeneratePowerId<T>() where T : class
    {
        var typeName = typeof(T).Name;
        return CamelCaseRegex.Replace(typeName, "$1_$2").ToLowerInvariant();
    }

    public static string GenerateIconPath<T>() where T : class
    {
        return AstralPartyAssetPaths.PowerIcon(GeneratePowerId<T>());
    }

    protected virtual string ResolveIconPath()
    {
        foreach (var path in GetCandidateIconPaths())
            if (ResourceLoader.Exists(path))
                return path;

        return MissingPowerIconPath;
    }

    protected virtual IEnumerable<string> GetCandidateIconPaths()
    {
        var idEntry = NormalizePowerImageId(Id.Entry);
        yield return $"res://ReAstralPartyMod/images/powers/{idEntry}.png";
        yield return $"res://ReAstralPartyMod/images/power/{idEntry}.png";
        yield return $"res://ReAstralPartyMod/images/powers/{PowerId}.png";
        yield return $"res://ReAstralPartyMod/images/power/{PowerId}.png";
        yield return MissingPowerIconPath;
    }

    protected virtual string NormalizePowerImageId(string idEntry)
    {
        var prefixSeparator = idEntry.IndexOf('-');
        if (prefixSeparator >= 0 && prefixSeparator < idEntry.Length - 1)
            idEntry = idEntry[(prefixSeparator + 1)..];

        return idEntry.ToLowerInvariant();
    }

    protected static PowerAssetProfile Icons(string iconPath, string? bigIconPath = null)
    {
        return new PowerAssetProfile(iconPath, bigIconPath ?? iconPath);
    }

    public virtual bool IsInstanced => false;

    public override PowerInstanceType InstanceType => IsInstanced ? PowerInstanceType.Instanced : PowerInstanceType.None;

    public virtual Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        return Task.CompletedTask;
    }

    public sealed override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        return BeforeSideTurnStart(choiceContext, side, combatState as CombatState
            ?? Owner?.CombatState as CombatState
            ?? Owner?.Player?.Creature?.CombatState as CombatState
            ?? Owner?.PetOwner?.Creature?.CombatState as CombatState
            ?? throw new InvalidOperationException("Expected CombatState for legacy power hook bridge."));
    }

    public virtual Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        return Task.CompletedTask;
    }

    public sealed override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        return AfterSideTurnStart(side, combatState as CombatState
            ?? Owner?.CombatState as CombatState
            ?? Owner?.Player?.Creature?.CombatState as CombatState
            ?? Owner?.PetOwner?.Creature?.CombatState as CombatState
            ?? throw new InvalidOperationException("Expected CombatState for legacy power hook bridge."));
    }

    public virtual Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return Task.CompletedTask;
    }

    public sealed override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        return AfterTurnEnd(choiceContext, side);
    }

    public virtual Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        return Task.CompletedTask;
    }

    public sealed override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        return AfterPowerAmountChanged(power, amount, applier, cardSource);
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
