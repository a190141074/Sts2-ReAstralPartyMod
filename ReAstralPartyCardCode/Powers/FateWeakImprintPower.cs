using System;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class FateWeakImprintPower : AstralPartyPowerModel
{
    private const decimal BonusDamageTaken = 1m;
    private const decimal MaxDuration = 2m;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.None;

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not FateWeakImprintPower)
            return false;
        if (target != Owner)
            return false;
        if (amount <= 0m)
            return false;

        // Reapplying refreshes the debuff back to two turns instead of increasing its strength.
        modifiedAmount = Math.Max(MaxDuration - Amount, 0m);
        return true;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // Apply the bonus before block so it also affects hits that are fully blocked.
        if (target != Owner)
            return 0m;
        if (Amount <= 0)
            return 0m;
        if (amount <= 0m)
            return 0m;

        return BonusDamageTaken;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner != null && side == Owner.Side)
            await PowerCmd.TickDownDuration(this);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (target != Owner)
            return;
        if (!result.WasTargetKilled)
            return;
        if (dealer == null)
            return;

        var rewardPlayer = dealer.Player ?? dealer.PetOwner;
        var combatState = rewardPlayer?.Creature?.CombatState;
        if (rewardPlayer == null || combatState == null)
            return;

        Flash();

        // Reward the creature that dealt the killing blow, not necessarily the one that applied the imprint.
        var card = combatState.CreateCard(ModelDb.Card<SkillFateGuidance>(), rewardPlayer);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
        await XiaoLeiAwakeningHelper.TryGrantAwakeningForGrantedCard(Applier?.Player, rewardPlayer);
    }
}
