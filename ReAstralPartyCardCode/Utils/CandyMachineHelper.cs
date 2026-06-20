using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Potions;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class CandyMachineHelper
{
    public static bool HasSkillTokenCuriousCandyMachineInHand(Player owner)
    {
        return PileType.Hand.GetPile(owner).Cards.Any(card =>
            card.CanonicalInstance is SkillTokenCuriousCandyMachine || card is SkillTokenCuriousCandyMachine);
    }

    public static async Task EnsureSkillTokenCuriousCandyMachineInHand(Player owner)
    {
        if (owner.Creature?.CombatState == null)
            return;
        if (HasSkillTokenCuriousCandyMachineInHand(owner))
            return;

        await CreateSkillTokenCuriousCandyMachineCardInHand(owner);
    }

    public static async Task CreateSkillTokenCuriousCandyMachineCardInHand(Player owner)
    {
        if (owner.Creature?.CombatState == null)
            return;

        var card = owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillTokenCuriousCandyMachine>(), owner);
        await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, true);
    }

    public static void GrantRandomCandyPotion(Player owner)
    {
        var potionIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            3,
            MainFile.ModId,
            nameof(CandyMachineHelper),
            nameof(GrantRandomCandyPotion),
            owner.RunState.Rng.StringSeed,
            owner.NetId,
            owner.Gold,
            owner.Potions.Count(),
            owner.Relics.Count(),
            owner.Creature?.CombatState?.RoundNumber ?? 0);

        switch (potionIndex)
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
