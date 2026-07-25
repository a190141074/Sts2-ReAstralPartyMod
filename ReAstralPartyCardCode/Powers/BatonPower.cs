using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class BatonPower : AstralPartyPowerModel
{
    private const decimal TemporaryStrengthAmount = 2m;
    private const decimal TemporaryDexterityAmount = 2m;
    private const decimal BlockAmount = 2m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        await AstralTemporaryStrengthPower.Apply(Owner, TemporaryStrengthAmount, this, applier, cardSource, true);
        await AstralTemporaryDexterityPower.Apply(Owner, TemporaryDexterityAmount, this, applier, cardSource, true);
        await CreatureCmd.GainBlock(Owner, BlockAmount, ValueProp.Move, null);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Player == null || Owner.Player == player)
            return;

        var stablePlayers = PersonMultiplayerEffectHelper.GetStableCombatPlayers(Owner.Player);
        if (stablePlayers.Count <= 1)
            return;

        var currentIndex = stablePlayers
            .Select((stablePlayer, index) => new { stablePlayer, index })
            .FirstOrDefault(entry => entry.stablePlayer == Owner.Player)?.index ?? -1;
        if (currentIndex < 0)
            return;

        var nextPlayer = stablePlayers[(currentIndex + 1) % stablePlayers.Count];
        if (nextPlayer != player || nextPlayer.Creature == null)
            return;

        await PowerCmd.Remove(this);
        await PowerCmd.Apply<BatonPower>(nextPlayer.Creature, 1m, Owner, null, false);
    }
}
