using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldMembersReferenceUltimate : TokenMembersReferenceRelicBase
{
    protected override int CardsToDraw => 2;

    protected override int EnergyToGain => 2;

    public override RelicRarity Rarity => RelicRarity.Rare;
}