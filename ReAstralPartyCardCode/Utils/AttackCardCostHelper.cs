using MegaCrit.Sts2.Core.Entities.Cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class AttackCardCostHelper
{
    public static int GetPlayedCost(CardPlay cardPlay)
    {
        if (cardPlay.Card.EnergyCost.CostsX)
            return Math.Max(1, cardPlay.Resources.EnergyValue);

        return cardPlay.Card.EnergyCost.GetResolved();
    }
}
