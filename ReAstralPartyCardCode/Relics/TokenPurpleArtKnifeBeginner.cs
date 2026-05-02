using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleArtKnifeBeginner : ArtKnifeRelicBase
{
    protected override decimal StrengthBonus => 2m;
    protected override CardType DamageCardType => CardType.Attack;

    public override RelicRarity Rarity => RelicRarity.Uncommon;
}