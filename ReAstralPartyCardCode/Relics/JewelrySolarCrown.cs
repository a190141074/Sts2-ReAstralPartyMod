using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class JewelrySolarCrown : AstralPartyRelicModel
{
    [SavedProperty] public string AstralParty_JewelrySolarCrownProcessedTargetsThisCard { get; set; } = string.Empty;
    [SavedProperty] public string AstralParty_JewelrySolarCrownCurrentCardKey { get; set; } = string.Empty;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        SinkouSetHelper.BuildSetDynamicVars(Owner);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WhereDivineLightShinesPower>(),
        .. SinkouSetHelper.BuildSetHoverTips(Owner)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_JewelrySolarCrownProcessedTargetsThisCard = string.Empty;
        AstralParty_JewelrySolarCrownCurrentCardKey = string.Empty;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (SinkouSetHelper.HasVariantSinkou(Owner))
        {
            foreach (var teammate in Owner.Creature.CombatState.Players)
            {
                if (teammate == Owner || teammate.Creature == null || teammate.Creature.HasPower<WhereDivineLightShinesPower>())
                    continue;

                await PowerCmd.Apply<WhereDivineLightShinesPower>(teammate.Creature, 1m, Owner.Creature, null, false);
            }
        }

        if (SinkouSetHelper.HasFullListeningToSolarRoarSet(Owner))
        {
            Flash();
            await SinkouSetHelper.TriggerExtraBurnAtTurnStart(Owner, this);
        }
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
            return Task.CompletedTask;

        AstralParty_JewelrySolarCrownCurrentCardKey = GetCardKey(cardPlay.Card);
        AstralParty_JewelrySolarCrownProcessedTargetsThisCard = string.Empty;
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
        if (!FlashlightRelicHelper.IsTrackedAttackCard(Owner, cardSource, 1))
            return;

        var currentCardKey = GetCardKey(cardSource);
        if (AstralParty_JewelrySolarCrownCurrentCardKey != currentCardKey)
        {
            AstralParty_JewelrySolarCrownCurrentCardKey = currentCardKey;
            AstralParty_JewelrySolarCrownProcessedTargetsThisCard = string.Empty;
        }

        var processedKey = $"{target.GetHashCode()}|";
        if (AstralParty_JewelrySolarCrownProcessedTargetsThisCard.Contains(processedKey, StringComparison.Ordinal))
            return;

        AstralParty_JewelrySolarCrownProcessedTargetsThisCard += processedKey;
        Flash();
        await PowerCmd.Apply<BlazingSolarBurnPower>(target, 2m, Owner.Creature, cardSource, false);
    }

    private static string GetCardKey(CardModel? card)
    {
        return card == null ? string.Empty : $"{card.Id.Entry}:{card.GetHashCode()}";
    }
}
