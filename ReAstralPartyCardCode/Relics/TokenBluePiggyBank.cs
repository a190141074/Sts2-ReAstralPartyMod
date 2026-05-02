using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenBluePiggyBank : AstralPartyRelicModel
{
    private const int EnergyThreshold = 6;
    private const decimal StarLightPerTrigger = 3m;

    [SavedProperty] public int AstralParty_RelicBluePiggyBankEnergyProgress { get; set; }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_RelicBluePiggyBankEnergyProgress;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_RelicBluePiggyBankEnergyProgress = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (card.Owner != Owner)
            return;
        if (amount <= 0)
            return;

        AstralParty_RelicBluePiggyBankEnergyProgress += amount;

        var triggerCount = AstralParty_RelicBluePiggyBankEnergyProgress / EnergyThreshold;
        AstralParty_RelicBluePiggyBankEnergyProgress %= EnergyThreshold;
        InvokeDisplayAmountChanged();

        if (triggerCount <= 0)
            return;

        Flash();
        await PowerCmd.Apply(
            ModelDb.Power<StarLightPower>().ToMutable(),
            Owner.Creature,
            triggerCount * StarLightPerTrigger,
            Owner.Creature,
            card,
            false
        );
    }
}