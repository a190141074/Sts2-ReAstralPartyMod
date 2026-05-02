using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldBankCardPremium : AstralPartyRelicModel
{
    private const int EternalStarlightToGrant = 14;
    private const decimal StarLightPerPlayer = 3m;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        TokenEternalStarlight.BuildReferenceHoverTip(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralEternalStarlightSetId),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null)
            return;

        await TokenEternalStarlight.GrantStacks(Owner, EternalStarlightToGrant);
        Owner.GetRelic<TokenGoldStarCoinHammer>()?.RefreshDisplayedBonusDamage();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        foreach (var player in Owner.Creature.CombatState.Players)
            await PowerCmd.Apply(
                ModelDb.Power<StarLightPower>().ToMutable(),
                player.Creature,
                StarLightPerPlayer,
                Owner.Creature,
                null,
                false
            );
    }
}