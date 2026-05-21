using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeDivineThrone : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeDivineThroneDisplayedCharge { get; set; }

    public int Stacks => AstralParty_PersonalityDerivativeDivineThroneDisplayedCharge;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(AstralParty_PersonalityDerivativeDivineThroneDisplayedCharge, 0);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DivineThronePower>()
    ];

    public void SetDisplayedCharge(int amount)
    {
        AstralParty_PersonalityDerivativeDivineThroneDisplayedCharge = Math.Max(amount, 0);
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null || !Owner.Creature.IsAlive)
            return;

        var node = (int)Owner.Creature.GetPowerAmount<DorothyNodePower>();
        if (node <= AstralDivinePersonaHelper.GetDivineNodeThresholdForAct(Owner))
            return;

        SetDisplayedCharge(DisplayAmount + 1);
        await Task.CompletedTask;
    }
}
