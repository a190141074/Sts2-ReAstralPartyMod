using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenExclusiveCursedSword : AstralPartyRelicModel
{
    private const decimal StartingFrail = 99m;

    [SavedProperty] public int AstralParty_TokenExclusiveCursedSwordCounter { get; set; } = 1;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_TokenExclusiveCursedSwordCounter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FrailPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralSpiritFestivalSeries)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenExclusiveCursedSwordCounter = 1;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply<FrailPower>(Owner.Creature, StartingFrail, Owner.Creature, null, false);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return;

        var roundNumber = Owner.Creature.CombatState?.RoundNumber ?? 0;
        if (roundNumber != 1 && roundNumber % 3 != 0)
            return;

        Flash();
        await PowerCmd.Apply<VigorPower>(
            Owner.Creature,
            AstralParty_TokenExclusiveCursedSwordCounter,
            Owner.Creature,
            null,
            false);
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature || target.Side == Owner.Creature.Side ||
            !result.WasTargetKilled)
            return Task.CompletedTask;

        AstralParty_TokenExclusiveCursedSwordCounter++;
        Flash();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}