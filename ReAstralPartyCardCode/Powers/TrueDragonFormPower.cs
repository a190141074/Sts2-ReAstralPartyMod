using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class TrueDragonFormPower : AstralPartyPowerModel
{
    private const decimal DamageBonus = 4m;
    private const decimal StrengthBonus = 2m;
    private const decimal DexterityBonus = 1m;
    private const decimal MaxHpBonus = 2m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        ApplyGlamToUpgradedAttackCards();
        UpgradeAllOwnedCards();
        await PowerCmd.Apply<StrengthPower>(Owner, StrengthBonus, applier, cardSource, true);
        await PowerCmd.Apply<DexterityPower>(Owner, DexterityBonus, applier, cardSource, true);
        await CreatureCmd.GainMaxHp(Owner, MaxHpBonus);
        await CreatureCmd.Heal(Owner, MaxHpBonus, false);
    }

    public override async Task AfterRemoved(Creature owner)
    {
        await PowerCmd.Apply<StrengthPower>(owner, -StrengthBonus, owner, null, true);
        await PowerCmd.Apply<DexterityPower>(owner, -DexterityBonus, owner, null, true);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (cardSource?.Type != CardType.Attack)
            return 0m;

        return DamageBonus;
    }

    private void ApplyGlamToUpgradedAttackCards()
    {
        var playerCombatState = GetOwnerPlayerCombatState();
        if (playerCombatState == null)
            return;

        var glam = ModelDb.Enchantment<Glam>();
        foreach (var card in playerCombatState.AllCards)
        {
            if (card.Type != CardType.Attack)
                continue;
            if (card.CurrentUpgradeLevel <= 0)
                continue;
            if (card.Enchantment != null)
                continue;
            if (!glam.CanEnchant(card))
                continue;

            CardCmd.Enchant<Glam>(card, 1m);
        }
    }

    private void UpgradeAllOwnedCards()
    {
        var playerCombatState = GetOwnerPlayerCombatState();
        if (playerCombatState == null)
            return;

        foreach (var card in playerCombatState.AllCards)
        {
            if (!card.IsUpgradable)
                continue;

            CardCmd.Upgrade(card);
        }
    }

    private PlayerCombatState? GetOwnerPlayerCombatState()
    {
        return Owner?.Player?.PlayerCombatState;
    }
}
