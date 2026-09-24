using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleSandwichBiscuitIntermediate : AstralPartyRelicModel
{
    private const decimal MaxHpBonus = 15m;
    private const decimal MaxHpPerStrength = 10m;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner?.Creature == null || !LocalContext.IsMe(Owner))
            return;

        Flash();
        if (TokenRelicBridgeInitializationContext.ShouldSkipOneTimeObtainRewards)
            return;

        await PandaMaxHpHelper.GainMaxHpFromRelic(Owner.Creature, MaxHpBonus, false);
    }

    public override Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;

        var extraMaxHp = Math.Max(0m, Owner.Creature.MaxHp - BaseMaxHpHelper.GetBaseMaxHp(Owner));
        var strength = Math.Floor(extraMaxHp / MaxHpPerStrength);
        if (strength <= 0m)
            return Task.CompletedTask;

        Flash();
        return PowerCmd.Apply<StrengthPower>(Owner.Creature, strength, Owner.Creature, null, true);
    }
}
