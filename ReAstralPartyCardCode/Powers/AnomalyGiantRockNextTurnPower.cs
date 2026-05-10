using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class AnomalyGiantRockNextTurnPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ANOMALY_GIANT_ROCK_NEXT_TURN_POWER.title");

    public override LocString Description => new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ANOMALY_GIANT_ROCK_NEXT_TURN_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || Owner.Player != player || player.Creature?.CombatState == null)
            return;

        var card = player.Creature.CombatState.CreateCard(ModelDb.Card<GiantRock>(), player);
        card.AddKeyword(CardKeyword.Exhaust);
        card.AddKeyword(CardKeyword.Ethereal);
        await CardGainAttribution.RunWithSource(this, () => CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, this));
        await GeneratedCardObserver.NotifyCardAddedToHand(card, this);
        await PowerCmd.Remove(this);
    }
}
