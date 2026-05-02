using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldSandwichBiscuitPremium : AstralPartyRelicModel
{
    private const decimal MaxHpBonus = 11m;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner?.Creature == null || !LocalContext.IsMe(Owner))
            return;

        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpBonus);
    }
}