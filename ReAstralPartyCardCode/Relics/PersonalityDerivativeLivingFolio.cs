using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeLivingFolio : AstralPartyRelicModel
{
    private const int MaxStacks = 9;

    [SavedProperty] public int AstralParty_PersonalityDerivativeLivingFolioStacks { get; set; } = 1;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeLivingFolioStacks;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillLivingFolio>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeLivingFolioStacks =
            ClampStacks(AstralParty_PersonalityDerivativeLivingFolioStacks);
        InvokeDisplayAmountChanged();
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room.RoomType == RoomType.Event)
            AddStacksCapped(1);

        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner != Owner)
            return Task.CompletedTask;
        if (cardPlay.Card is SkillTroubleMaker)
            return Task.CompletedTask;
        if (!AstralEventCardPool.IsEventCard(cardPlay.Card))
            return Task.CompletedTask;

        AddStacksCapped(1);
        return Task.CompletedTask;
    }

    public bool TryConsume(int amount)
    {
        if (amount <= 0)
            return true;
        if (AstralParty_PersonalityDerivativeLivingFolioStacks < amount)
            return false;

        AstralParty_PersonalityDerivativeLivingFolioStacks -= amount;
        InvokeDisplayAmountChanged();
        return true;
    }

    public void AddStacksCapped(int amount)
    {
        if (amount <= 0)
            return;

        var newAmount = ClampStacks(AstralParty_PersonalityDerivativeLivingFolioStacks + amount);
        if (newAmount == AstralParty_PersonalityDerivativeLivingFolioStacks)
            return;

        AstralParty_PersonalityDerivativeLivingFolioStacks = newAmount;
        Flash();
        InvokeDisplayAmountChanged();
    }

    private static int ClampStacks(int amount)
    {
        return Math.Clamp(amount, 0, MaxStacks);
    }
}
