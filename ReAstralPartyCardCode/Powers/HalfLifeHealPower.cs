using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Exceptions;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

/// <summary>
/// Heals at the start of the owner's next turn, then halves its amount.
/// </summary>
public class HalfLifeHealPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldReceiveCombatHooks => true;

    public override LocString Description =>
        new("powers", GetDescriptionLocKey());

    protected override string SmartDescriptionLocKey => GetSmartDescriptionLocKey();

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || Owner.Player != player)
            return;

        var healAmount = AmountOnTurnStart;
        if (healAmount <= 0)
            return;

        Flash();
        await CreatureCmd.Heal(Owner, healAmount, true);

        var newAmount = ShouldUseLingYulinDecay()
            ? Math.Ceiling(healAmount * 2m / 3m)
            : healAmount / 2m;
        if (newAmount <= 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        var delta = newAmount - Amount;
        if (delta != 0)
            await PowerCmd.ModifyAmount(this, delta, Owner, null, true);
    }

    private bool ShouldUseLingYulinDecay()
    {
        try
        {
            return Owner?.Player?.GetRelic<VariantPersonLingYulin>() != null;
        }
        catch (CanonicalModelException)
        {
            return false;
        }
    }

    private string GetDescriptionLocKey()
    {
        return ShouldUseLingYulinDecay()
            ? "RE_ASTRAL_PARTY_MOD_POWER_HALF_LIFE_HEAL_POWER.description_ling_yu_lin"
            : "RE_ASTRAL_PARTY_MOD_POWER_HALF_LIFE_HEAL_POWER.description";
    }

    private string GetSmartDescriptionLocKey()
    {
        return ShouldUseLingYulinDecay()
            ? "RE_ASTRAL_PARTY_MOD_POWER_HALF_LIFE_HEAL_POWER.smartDescription_ling_yu_lin"
            : "RE_ASTRAL_PARTY_MOD_POWER_HALF_LIFE_HEAL_POWER.smartDescription";
    }
}
