using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

[RegisterPotion(typeof(VialEpisodePotionPool))]
public class VialNeutralEventPotion : AstralPartyPotionModel
{
    public VialNeutralEventPotion() : base(true)
    {
    }

    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null)
            return;

        var options = VialEpisodeEventHelper.CreateNeutralEventOptions(Owner, $"{Id.Entry}.use");
        await VialEpisodeEventHelper.PlaySelectedCanonicalCardForOwner(choiceContext, Owner, options,
            $"{Id.Entry}.use");
    }
}
