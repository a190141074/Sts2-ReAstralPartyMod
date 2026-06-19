using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract partial class AstralPartyRelicModel : ModRelicTemplate
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string RelicId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    protected virtual string IconBasePath => $"res://ReAstralPartyMod/images/relic/{RelicId}";
    protected new virtual IEnumerable<IHoverTip> ExtraHoverTips => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => ExtraHoverTips;

    public override RelicAssetProfile AssetProfile => new()
    {
        IconPath = PackedIconPath,
        IconOutlinePath = PackedIconOutlinePath,
        BigIconPath = BigIconPath
    };

    public override string PackedIconPath => $"{IconBasePath}.png";
    public virtual string PublicBigIconPath => BigIconPath;
    protected override string BigIconPath => $"{IconBasePath}.png";
    protected override string PackedIconOutlinePath => $"{IconBasePath}.png";

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
            ?? Owner?.Creature?.CombatState as CombatState
            ?? throw new InvalidOperationException("Expected CombatState for legacy relic hook bridge."));
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
            ?? Owner?.Creature?.CombatState as CombatState
            ?? throw new InvalidOperationException("Expected CombatState for legacy relic hook bridge."));
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
