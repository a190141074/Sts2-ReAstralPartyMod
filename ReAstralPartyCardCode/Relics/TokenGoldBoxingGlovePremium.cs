using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldBoxingGlovePremium : BoxingGloveRelicBase
{
    protected override decimal CombatStartStrengthBonus => 2m;

    protected override decimal TurnStartVigorBonus => 3m;

    public override RelicRarity Rarity => RelicRarity.Rare;
}
