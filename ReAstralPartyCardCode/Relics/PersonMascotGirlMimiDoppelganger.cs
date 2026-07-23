using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonMascotGirlMimiDoppelganger : PersonRelicBase
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromRelic<PersonalityDerivativeSolidStateTime>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeSolidStateTime>(Owner);
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

        var candidates = SolidStateTimeHelper.GetRewardablePowerCardsIgnoringCharacterPool(player);
        if (candidates.Count == 0)
            return false;

        var rerollCount = StokovCardRewardRefreshHelper.GetRerollCountFor(player, options);
        var selected = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                candidates,
                static card => (card.CanonicalInstance?.Id ?? card.Id).Entry,
                MainFile.ModId,
                Id.Entry,
                nameof(TryModifyCardRewardOptions),
                player.RunState?.Rng?.StringSeed,
                player.RunState?.CurrentActIndex ?? -1,
                player.RunState?.TotalFloor ?? -1,
                player.NetId,
                rewardCards.Count,
                rerollCount)
            .FirstOrDefault();
        if (selected == null)
            return false;

        var createdCard = player.RunState?.CreateCard(selected.CanonicalInstance ?? selected, player)
                          ?? (selected.CanonicalInstance ?? selected).ToMutable();
        createdCard.Owner ??= player;
        rewardCards.Add(new CardCreationResult(createdCard));
        return true;
    }
}
