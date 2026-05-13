using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

[RegisterPotion(typeof(VialEpisodePotionPool))]
public class VialGoodEventPotion : AstralPartyPotionModel
{
    public VialGoodEventPotion() : base(true)
    {
    }

    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null)
            return;

        var options = VialEpisodeEventHelper.CreateGoodEventOptions(Owner);
        var selected = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayer(
            choiceContext,
            Owner,
            options,
            false,
            $"{Id.Entry}.use");
        if (selected == null)
            return;

        await VialEpisodeEventHelper.AutoPlayCanonicalCardForOwner(Owner, selected);
    }
}
