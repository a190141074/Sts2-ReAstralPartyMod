using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonSnakebiter : PersonaRelicBase
{
    protected override string RelicId => "variant_person_snakebiter";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<PoisonedStab>(),
        HoverTipFactory.FromCard<Snakebite>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        SnakebiterDeckReplacementHelper.ReplaceCurrentDeck(Owner);
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> rewardCards,
        CardCreationOptions options)
    {
        if (player != Owner)
            return false;
        if (options.Source != CardCreationSource.Encounter)
            return false;

        var createdCard = player.RunState?.CreateCard(ModelDb.Card<Snakebite>(), player) ?? ModelDb.Card<Snakebite>().ToMutable();
        createdCard.Owner ??= player;
        rewardCards.Add(new CardCreationResult(createdCard));
        return true;
    }
}
