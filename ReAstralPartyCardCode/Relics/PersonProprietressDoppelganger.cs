using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonProprietressDoppelganger : CooldownPersonRelicBase
{
    private const decimal InterestPerFloorRatio = 0.035m;
    private const decimal OtherPlayerPriceIncreaseRatio = 0.10m;

    [SavedProperty] public int AstralParty_PersonProprietressDoppelgangerCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonProprietressDoppelgangerPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_PersonProprietressDoppelgangerLastProcessedTotalFloor { get; set; } = -1;

    protected override int CounterValue
    {
        get => AstralParty_PersonProprietressDoppelgangerCounter;
        set => AstralParty_PersonProprietressDoppelgangerCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonProprietressDoppelgangerPendingCombatStartCard;
        set => AstralParty_PersonProprietressDoppelgangerPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 3;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillRemoteStore>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonProprietressDoppelgangerLastProcessedTotalFloor = Owner?.RunState?.TotalFloor ?? -1;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        var owner = Owner;
        var runState = owner?.RunState;
        if (owner == null || runState == null)
            return Task.CompletedTask;

        var currentFloor = runState.TotalFloor;
        if (AstralParty_PersonProprietressDoppelgangerLastProcessedTotalFloor < 0)
        {
            AstralParty_PersonProprietressDoppelgangerLastProcessedTotalFloor = currentFloor;
            return Task.CompletedTask;
        }

        var climbedFloors = Math.Max(0, currentFloor - AstralParty_PersonProprietressDoppelgangerLastProcessedTotalFloor);
        AstralParty_PersonProprietressDoppelgangerLastProcessedTotalFloor = currentFloor;
        if (climbedFloors <= 0)
            return Task.CompletedTask;

        var totalPartyGold = runState.Players.Sum(player => player.Gold);
        var interest = StableNumericStateHelper.FloorToNonNegativeInt(totalPartyGold * InterestPerFloorRatio * climbedFloors);
        if (interest <= 0)
            return Task.CompletedTask;

        Flash();
        return PersonMultiplayerEffectHelper.GainGoldDeterministic(interest, owner);
    }

    public override decimal ModifyMerchantPrice(MegaCrit.Sts2.Core.Entities.Players.Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (Owner == null || player == Owner)
            return originalPrice;
        if (player.RunState?.CurrentRoom is not MerchantRoom)
            return originalPrice;

        return originalPrice * (1m + OtherPlayerPriceIncreaseRatio);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillRemoteStore>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
