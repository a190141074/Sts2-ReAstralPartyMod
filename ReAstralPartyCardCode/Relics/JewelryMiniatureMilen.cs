using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class JewelryMiniatureMilen : AstralPartyRelicModel
{
    [SavedProperty] public bool AstralParty_JewelryMiniatureMilenTriggeredThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        SinkouSetHelper.BuildSetDynamicVars(IsMutable ? Owner : null);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        SinkouSetHelper.BuildSetHoverTips(IsMutable ? Owner : null);

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_JewelryMiniatureMilenTriggeredThisTurn = false;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner)
            AstralParty_JewelryMiniatureMilenTriggeredThisTurn = false;

        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return;
        if (target.Side == Owner.Creature.Side)
            return;
        if (result.TotalDamage <= 0m)
            return;
        if (SinkouSetHelper.HasVariantSinkou(Owner))
            return;
        if (AstralParty_JewelryMiniatureMilenTriggeredThisTurn)
            return;

        AstralParty_JewelryMiniatureMilenTriggeredThisTurn = true;
        var bonusDamage = Math.Ceiling(Math.Max(0m, target.Block) * 0.25m);
        if (bonusDamage <= 0m)
            return;

        Flash();
        await CreatureCmd.Damage(choiceContext, target, bonusDamage, ValueProp.Unpowered, Owner.Creature, null);
    }
}
