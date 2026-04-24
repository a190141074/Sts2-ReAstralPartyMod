using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenPurpleArtKnifeBeginner : ArtKnifeRelicBase
{
    protected override decimal StrengthBonus => 2m;
    protected override CardType DamageCardType => CardType.Attack;

    public override RelicRarity Rarity => RelicRarity.Uncommon;
}