using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenPurpleFlyingStar : AstralPartyRelicModel
{
    private const decimal EndTurnDamage = 3m;
    private const int StarLightAmount = 1;

    [SavedProperty] public bool AstralParty_TokenPurpleFlyingStarDealtEnemyDamageByCardThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenPurpleFlyingStarDealtEnemyDamageByCardThisTurn = false;
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
        if (!IsTrackedCardDamage(target, result.TotalDamage, cardSource))
            return;

        AstralParty_TokenPurpleFlyingStarDealtEnemyDamageByCardThisTurn = true;
        await Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        var shouldResolve = !AstralParty_TokenPurpleFlyingStarDealtEnemyDamageByCardThisTurn;
        AstralParty_TokenPurpleFlyingStarDealtEnemyDamageByCardThisTurn = false;
        if (!shouldResolve)
            return;

        Flash();
        var enemies = EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature).ToList();
        foreach (var enemy in enemies)
            await CreatureCmdCompat.Damage(
                choiceContext,
                enemy,
                EndTurnDamage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                null);

        await PowerCmd.Apply(ModelDb.Power<StarLightPower>().ToMutable(), Owner.Creature, StarLightAmount, Owner.Creature, null, false);
    }

    private bool IsTrackedCardDamage(Creature target, decimal totalDamage, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (target.Side == Owner.Creature.Side)
            return false;
        if (totalDamage <= 0m)
            return false;
        if (cardSource == null || cardSource.Owner != Owner)
            return false;

        return true;
    }
}
