using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

// This is an independent combat container. It deliberately never touches OrbQueue.
public sealed class TerraprismaMountedPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override int DisplayAmount => 0;
    public override bool ShouldReceiveCombatHooks => true;

    protected override bool IsVisibleInternal => false;

    internal static TerraprismaMountedPower? GetExisting(Player owner)
    {
        return owner.Creature?.GetPower<TerraprismaMountedPower>();
    }

    internal static async Task<TerraprismaMountedPower?> GetOrCreate(Player owner)
    {
        var existing = GetExisting(owner);
        if (existing != null)
            return existing;

        if (owner.Creature == null)
            return null;

        await PowerCmd.Apply<TerraprismaMountedPower>(
            owner.Creature,
            1m,
            owner.Creature,
            null,
            false);
        return GetExisting(owner);
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Player is not { } owner || side != Owner.Side)
            return Task.CompletedTask;

        return TerraprismaHelper.AutoPlayMountedBladesAsync(choiceContext, owner);
    }
}
