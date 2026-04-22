using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
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

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenGoldFlashlightFlashburst : AstralPartyRelicModel
{
    private const int MinTrackedAttackCost = 1;
    private const int HitsPerTrigger = 3;
    private const int EternalStarlightPerDamageBonus = 10;

    [SavedProperty] public int AstralParty_TokenGoldFlashlightFlashburstHitProgress { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetAttackDamageBonus();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ExposurePower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralFlashlightSet),
        TokenEternalStarlight.BuildReferenceHoverTip(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenGoldFlashlightFlashburstHitProgress = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        InvokeDisplayAmountChanged();

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
        if (FlashlightRelicHelper.IsTrackedAttackHit(Owner, dealer, result, target, cardSource, MinTrackedAttackCost))
        {
            AstralParty_TokenGoldFlashlightFlashburstHitProgress++;
            var triggerCount = AstralParty_TokenGoldFlashlightFlashburstHitProgress / HitsPerTrigger;
            AstralParty_TokenGoldFlashlightFlashburstHitProgress %= HitsPerTrigger;
            InvokeDisplayAmountChanged();

            if (triggerCount > 0)
            {
                Flash();
                await PowerCmd.Apply(
                    ModelDb.Power<StarLightPower>().ToMutable(),
                    Owner.Creature,
                    triggerCount,
                    Owner.Creature,
                    cardSource,
                    false);
            }
        }

        if (dealer == Owner.Creature
            && target.Side != Owner.Creature.Side
            && result.WasTargetKilled
            && FlashlightRelicHelper.ShouldHandleSharedSet(this))
            await FlashlightRelicHelper.TryGrantEternalStarlightOnKill(Owner, target);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (cardSource?.Type != CardType.Attack)
            return 0m;

        return GetAttackDamageBonus();
    }

    private int GetAttackDamageBonus()
    {
        var eternalStarlight = Owner?.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0;
        return eternalStarlight / EternalStarlightPerDamageBonus;
    }
}
