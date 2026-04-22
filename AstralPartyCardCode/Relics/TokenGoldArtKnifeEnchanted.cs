using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenGoldArtKnifeEnchanted : ArtKnifeRelicBase
{
    protected override decimal StrengthBonus => 4m;
    protected override CardType DamageCardType => CardType.Skill;
    protected override decimal HealDamageDivisor => 2m;

    public override RelicRarity Rarity => RelicRarity.Rare;
}
