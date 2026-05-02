using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleFlashlightStronglight : AstralPartyRelicModel
{
    private const int MinTrackedAttackCost = 1;
    private const int CardsPerTrigger = 3;

    [SavedProperty] public int AstralParty_TokenPurpleFlashlightStronglightHitProgress { get; set; }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_TokenPurpleFlashlightStronglightHitProgress;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ExposurePower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFlashlightSetId),
        TokenEternalStarlight.BuildReferenceHoverTip(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenPurpleFlashlightStronglightHitProgress = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner == null)
            return;
        if (!FlashlightRelicHelper.ShouldHandleSharedSet(this))
            return;

        await FlashlightRelicHelper.ApplyExposureToEnemies(Owner);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;

        if (dealer == Owner.Creature
            && target.Side != Owner.Creature.Side
            && result.WasTargetKilled
            && FlashlightRelicHelper.ShouldHandleSharedSet(this))
            await FlashlightRelicHelper.TryGrantEternalStarlightOnKill(Owner, target);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        if (!FlashlightRelicHelper.IsTrackedAttackCard(Owner, cardPlay.Card, MinTrackedAttackCost))
            return;

        AstralParty_TokenPurpleFlashlightStronglightHitProgress++;
        var triggerCount = AstralParty_TokenPurpleFlashlightStronglightHitProgress / CardsPerTrigger;
        AstralParty_TokenPurpleFlashlightStronglightHitProgress %= CardsPerTrigger;
        InvokeDisplayAmountChanged();

        if (triggerCount <= 0)
            return;

        Flash();
        await PowerCmd.Apply(
            ModelDb.Power<StarLightPower>().ToMutable(),
            Owner.Creature,
            triggerCount,
            Owner.Creature,
            cardPlay.Card,
            false);
    }
}