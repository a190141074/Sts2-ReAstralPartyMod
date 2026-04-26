using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenPurpleBigBackpack : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? amount + 1m : amount;
    }
}