using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Potions;

public class CandyPotionPool : CustomPotionPoolModel
{
    public override bool IsShared => false;

    protected override IEnumerable<PotionModel> GenerateAllPotions()
    {
        return
        [
            ModelDb.Potion<CandySupportGum>(),
            ModelDb.Potion<CandyEnergySupplementBar>(),
            ModelDb.Potion<CandyBigBrainGummy>()
        ];
    }
}