using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class EtheriumSwordRecallOmenPower : EnigmaticOmenPowerBase
{
    protected override string EffectDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_ETHERIUM_SWORD_RECALL_OMEN_POWER.effect";

    protected override IEnumerable<IHoverTip> EffectHoverTips =>
    [
        HoverTipFactory.FromCard<EnigmaticStrikeEtheriumSword>()
    ];

    protected override async Task OnTriggered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature?.CombatState == null)
            return;

        var card = player.Creature.CombatState.CreateCard(ModelDb.Card<EnigmaticStrikeEtheriumSword>(), player);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
