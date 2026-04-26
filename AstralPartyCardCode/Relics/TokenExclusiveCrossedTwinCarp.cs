using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenExclusiveCrossedTwinCarp : AstralPartyRelicModel
{
    private bool _grantScheduled;
    private bool _isGrantingPower;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CrossedTwinCarpPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralDragonPalaceSeries)
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return;
        if (Owner.Creature.HasPower<CrossedTwinCarpPower>())
            return;

        await GrantCrossedTwinCarpPower();
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (_isGrantingPower)
            return false;
        if (Owner?.Creature == null || target != Owner.Creature)
            return false;
        if (canonicalPower is not VigorPower || amount <= 0m)
            return false;
        if (Owner.Creature.HasPower<CrossedTwinCarpPower>())
            return false;

        ScheduleGrant();
        return false;
    }

    private void ScheduleGrant()
    {
        if (_grantScheduled)
            return;

        _grantScheduled = true;
        _ = FlushGrantAsync();
    }

    private async Task FlushGrantAsync()
    {
        await Task.Yield();
        _grantScheduled = false;

        if (Owner?.Creature == null || Owner.Creature.HasPower<CrossedTwinCarpPower>())
            return;

        await GrantCrossedTwinCarpPower();
    }

    private async Task GrantCrossedTwinCarpPower()
    {
        if (Owner?.Creature == null)
            return;

        _isGrantingPower = true;
        try
        {
            Flash();
            await PowerCmd.Apply<CrossedTwinCarpPower>(Owner.Creature, 1m, Owner.Creature, null, false);
        }
        finally
        {
            _isGrantingPower = false;
        }
    }
}