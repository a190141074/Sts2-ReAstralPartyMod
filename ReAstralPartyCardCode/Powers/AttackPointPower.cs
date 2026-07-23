using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class AttackPointPower : CardPointPowerBase
{
    [SavedProperty] public decimal AstralParty_AttackPointPowerLastAcceptedAmount { get; set; }

    public override LocString Title => new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ATTACK_POINT_POWER.title");

    public override LocString Description =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ATTACK_POINT_POWER.description");

    protected override string SmartDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_ATTACK_POINT_POWER.smartDescription";

    protected override decimal LastAcceptedAmount
    {
        get => AstralParty_AttackPointPowerLastAcceptedAmount;
        set => AstralParty_AttackPointPowerLastAcceptedAmount = value;
    }

    protected override bool QualifiesForPointSpend(CardModel card)
    {
        return WarforgeEnchantmentHelper.CountsAsAttack(card);
    }

    protected override decimal GetMinimumCostAfterPriorityPointSpend(CardModel card)
    {
        var player = Owner?.Player;
        if (player == null || !WarforgeEnchantmentHelper.CountsAsAttack(card))
            return 0m;

        if (player.GetRelic<TokenGoldBattleBookMid>() != null)
            return TokenGoldBattleBookMid.AttackPointMinimumCost;

        if (player.GetRelic<TokenBlueBattleBookBasic>() != null)
            return TokenBlueBattleBookBasic.AttackPointMinimumCost;

        return 0m;
    }

    internal static Task GrantFromRelic(Creature target, decimal amount, RelicModel sourceRelic)
    {
        return GrantFromRelic<AttackPointPower>(target, amount, sourceRelic);
    }
}
