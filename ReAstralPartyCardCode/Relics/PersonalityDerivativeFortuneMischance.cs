using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeFortuneMischance : AstralPartyRelicModel
{
    private const int MaxStacks = 9;

    [SavedProperty] public int AstralParty_PersonalityDerivativeFortuneMischanceStacks { get; set; }
    [SavedProperty] public int AstralParty_PersonalityDerivativeFortuneMischanceLastGrantedTotalFloor { get; set; } = -1;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeFortuneMischanceStacks;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFortuneMischance>(),
        HoverTipFactory.FromPower<FengShuiNodePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeFortuneMischanceStacks = Math.Clamp(
            AstralParty_PersonalityDerivativeFortuneMischanceStacks,
            0,
            MaxStacks);
        InvokeDisplayAmountChanged();
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        var totalFloor = Owner?.RunState?.TotalFloor ?? -1;
        if (totalFloor < 0 || totalFloor == AstralParty_PersonalityDerivativeFortuneMischanceLastGrantedTotalFloor)
            return Task.CompletedTask;

        var ones = Math.Abs(totalFloor % 10);
        if (ones is 1 or 6)
        {
            AstralParty_PersonalityDerivativeFortuneMischanceLastGrantedTotalFloor = totalFloor;
            AddStacksCapped(1);
        }

        return Task.CompletedTask;
    }

    public void AddStacksCapped(int amount)
    {
        if (amount <= 0)
            return;

        var newAmount = Math.Clamp(AstralParty_PersonalityDerivativeFortuneMischanceStacks + amount, 0, MaxStacks);
        if (newAmount == AstralParty_PersonalityDerivativeFortuneMischanceStacks)
            return;

        AstralParty_PersonalityDerivativeFortuneMischanceStacks = newAmount;
        Flash();
        InvokeDisplayAmountChanged();
    }

    public bool TryConsume(int amount)
    {
        if (amount <= 0)
            return true;
        if (AstralParty_PersonalityDerivativeFortuneMischanceStacks < amount)
            return false;

        AstralParty_PersonalityDerivativeFortuneMischanceStacks -= amount;
        InvokeDisplayAmountChanged();
        return true;
    }
}
