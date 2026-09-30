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

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetExtraMaxHpLayers();

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
        {
            InvokeDisplayAmountChanged();
            return;
        }

        await PandaMaxHpHelper.GainMaxHpFromRelic(Owner.Creature, MaxHpBonus, false);
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;

        var strength = GetExtraMaxHpLayers();
        InvokeDisplayAmountChanged();
        if (strength <= 0m)
            return Task.CompletedTask;

        Flash();
        return PowerCmd.Apply<StrengthPower>(Owner.Creature, strength, Owner.Creature, null, true);
    }

    private int GetExtraMaxHpLayers()
    {
        if (Owner?.Creature == null)
            return 0;

        var extraMaxHp = Math.Max(0m, Owner.Creature.MaxHp - BaseMaxHpHelper.GetBaseMaxHp(Owner));
        return StableNumericStateHelper.FloorDivisionToNonNegativeInt(extraMaxHp, MaxHpPerStrength);
    }
}
