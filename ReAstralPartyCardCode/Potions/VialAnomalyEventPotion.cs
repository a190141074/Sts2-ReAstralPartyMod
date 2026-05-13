using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

[RegisterPotion(typeof(VialEpisodePotionPool))]
public class VialAnomalyEventPotion : AstralPartyPotionModel
{
    public VialAnomalyEventPotion() : base(true)
    {
    }

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null)
            return;

        await VialEpisodeEventHelper.AutoPlayCanonicalCardForOwner(Owner, ModelDb.Card<EventAnomalyTransferRequest>());
    }
}
