using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenBlueFlashlightGeneral : AstralPartyRelicModel
{
    private const decimal HealOnKill = 4m;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ExposurePower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFlashlightSetId),
        TokenEternalStarlight.BuildReferenceHoverTip()
    ];

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
        if (dealer != Owner.Creature)
            return;
        if (target.Side == Owner.Creature.Side)
            return;
        if (!result.WasTargetKilled)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, HealOnKill, true);

        if (!FlashlightRelicHelper.ShouldHandleSharedSet(this))
            return;

        await FlashlightRelicHelper.TryGrantEternalStarlightOnKill(Owner, target);
    }
}
