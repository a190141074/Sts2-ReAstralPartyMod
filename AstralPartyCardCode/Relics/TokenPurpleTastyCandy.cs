using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenPurpleTastyCandy : AstralPartyRelicModel
{
    private const decimal HealToGainPerCard = 1m;
    private const decimal HealCostForEnergy = 4m;
    private const decimal EnergyToGain = 1m;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<HalfLifeHealPower>()
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (GetPlayedCost(cardPlay) < 1)
            return;

        Flash();
        await PowerCmd.Apply<HalfLifeHealPower>(Owner.Creature, HealToGainPerCard, Owner.Creature, cardPlay.Card, false);

        if (Owner.Creature.CurrentHp < Owner.Creature.MaxHp)
            return;

        var healPower = Owner.Creature.GetPower<HalfLifeHealPower>();
        if (healPower == null || healPower.Amount < HealCostForEnergy)
            return;

        if (healPower.Amount == HealCostForEnergy)
            await PowerCmd.Remove(healPower);
        else
            await PowerCmd.ModifyAmount(healPower, -HealCostForEnergy, Owner.Creature, cardPlay.Card, true);

        await PlayerCmd.GainEnergy(EnergyToGain, Owner);
    }

    private static int GetPlayedCost(CardPlay cardPlay)
    {
        if (cardPlay.Card.EnergyCost.CostsX)
            return Math.Max(1, cardPlay.Resources.EnergyValue);

        return cardPlay.Card.EnergyCost.GetResolved();
    }
}
