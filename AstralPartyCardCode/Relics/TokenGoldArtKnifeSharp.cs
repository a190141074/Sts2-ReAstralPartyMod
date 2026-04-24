using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenGoldArtKnifeSharp : ArtKnifeRelicBase
{
    protected override decimal StrengthBonus => 4m;
    protected override CardType DamageCardType => CardType.Attack;

    public override RelicRarity Rarity => RelicRarity.Rare;
}