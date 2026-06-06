using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class EnigmaticSynthesisEtheriumWeaponRelicBase : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected abstract IEnumerable<IHoverTip> CreateCardHoverTips();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => CreateCardHoverTips();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        EtheriumWeaponStrikeReplacementHelper.ReplaceDeckStrikesForCurrentWeapon(Owner);
    }
}
