using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldArtKnifeEnchanted : ArtKnifeRelicBase
{
    protected override decimal StrengthBonus => 4m;
    protected override CardType DamageCardType => CardType.Skill;
    protected override decimal HealDamageDivisor => 2m;

    public override RelicRarity Rarity => RelicRarity.Rare;
}