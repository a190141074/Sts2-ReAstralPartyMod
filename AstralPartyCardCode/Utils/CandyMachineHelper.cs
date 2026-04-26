using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Potions;
using AstralPartyMod.AstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class CandyMachineHelper
{
    public static bool HasCuriousCandyMachineInHand(Player owner)
    {
        return PileType.Hand.GetPile(owner).Cards.Any(card =>
            card.CanonicalInstance is CuriousCandyMachine || card is CuriousCandyMachine);
    }

    public static async Task EnsureCuriousCandyMachineInHand(Player owner)
    {
        if (owner.Creature?.CombatState == null)
            return;
        if (HasCuriousCandyMachineInHand(owner))
            return;

        var card = owner.Creature.CombatState.CreateCard(ModelDb.Card<CuriousCandyMachine>(), owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }

    public static void GrantRandomCandyPotion(Player owner)
    {
        switch (owner.RunState.Rng.Niche.NextInt(3))
        {
            case 0:
                owner.AddPotionInternal(ModelDb.Potion<CandySupportGum>().ToMutable());
                break;
            case 1:
                owner.AddPotionInternal(ModelDb.Potion<CandyEnergySupplementBar>().ToMutable());
                break;
            default:
                owner.AddPotionInternal(ModelDb.Potion<CandyBigBrainGummy>().ToMutable());
                break;
        }
    }

    public static async Task RemovePowerIfPresent<T>(Creature target)
        where T : PowerModel
    {
        var power = target.GetPower<T>();
        if (power != null)
            await PowerCmd.Remove(power);
    }
}