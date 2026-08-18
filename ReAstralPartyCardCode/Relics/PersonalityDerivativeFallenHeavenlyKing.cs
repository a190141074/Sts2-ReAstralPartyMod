using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonalityDerivativeFallenHeavenlyKing : AstralPartyRelicModel
{
    [SavedProperty] public bool AstralParty_FallenHeavenlyKingInitialDeckCopied { get; set; }
    [SavedProperty] public bool AstralParty_FallenHeavenlyKingDeckReplaced { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null || AstralParty_FallenHeavenlyKingInitialDeckCopied)
            return;

        await FallenHeavenlyKingDeckHelper.DuplicateDeckTwice(Owner);
        AstralParty_FallenHeavenlyKingInitialDeckCopied = true;
    }

    public override Task AfterEnergySpent(CardModel card, int amount)
    {
        if (Owner != null && card.Owner == Owner)
            FallenHeavenlyKingDeckHelper.ReduceFallCost(Owner, amount);
        return Task.CompletedTask;
    }
}
