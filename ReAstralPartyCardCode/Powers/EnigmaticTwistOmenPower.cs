using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class EnigmaticTwistOmenPower : EnigmaticOmenPowerBase
{
    protected override int DefaultTurns => 1;

    protected override string EffectDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_ENIGMATIC_TWIST_OMEN_POWER.effect";

    protected override int ResolveTriggerCount()
    {
        return Math.Max(1, StableNumericStateHelper.FloorToNonNegativeInt(Amount));
    }

    protected override async Task OnTriggered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature?.CombatState == null)
            return;

        var card = player.Creature.CombatState.CreateCard(ModelDb.Card<EnigmaticTheTwist>(), player);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
