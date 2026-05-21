using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeBookOfHeaven : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeBookOfHeavenStacks { get; set; } = 1;

    public int Stacks => Math.Max(AstralParty_PersonalityDerivativeBookOfHeavenStacks, 1);

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Stacks;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DivineSonPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeBookOfHeavenStacks = Math.Max(AstralParty_PersonalityDerivativeBookOfHeavenStacks, 1);
        InvokeDisplayAmountChanged();
    }

    public void AddStacks(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_PersonalityDerivativeBookOfHeavenStacks = Math.Max(1,
            AstralParty_PersonalityDerivativeBookOfHeavenStacks + amount);
        InvokeDisplayAmountChanged();
    }
}
