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
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenPurpleFlyingStar : AstralPartyRelicModel
{
    private const decimal EndTurnDamage = 3m;
    private const int StarLightAmount = 1;
    private const int EnemiesPerEternalStarlight = 3;

    [SavedProperty] public bool AstralParty_TokenPurpleFlyingStarPlayedAttackCardThisTurn { get; set; }
    [SavedProperty] public int AstralParty_TokenPurpleFlyingStarEnemyHitProgress { get; set; }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_TokenPurpleFlyingStarEnemyHitProgress;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFlyingStarId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenPurpleFlyingStarEnemyHitProgress = 0;
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenPurpleFlyingStarPlayedAttackCardThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;

        AstralParty_TokenPurpleFlyingStarPlayedAttackCardThisTurn = true;
        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        var shouldResolve = !AstralParty_TokenPurpleFlyingStarPlayedAttackCardThisTurn;
        AstralParty_TokenPurpleFlyingStarPlayedAttackCardThisTurn = false;
        if (!shouldResolve)
            return;

        Flash();
        var enemies = EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature).ToList();
        foreach (var enemy in enemies)
        {
            await CreatureCmdCompat.Damage(
                choiceContext,
                enemy,
                EndTurnDamage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                null);

            await RecordEnemyHit();
        }

        await PowerCmd.Apply(ModelDb.Power<StarLightPower>().ToMutable(), Owner.Creature, StarLightAmount, Owner.Creature, null, false);
    }

    private async Task RecordEnemyHit()
    {
        AstralParty_TokenPurpleFlyingStarEnemyHitProgress++;
        var triggerCount = AstralParty_TokenPurpleFlyingStarEnemyHitProgress / EnemiesPerEternalStarlight;
        AstralParty_TokenPurpleFlyingStarEnemyHitProgress %= EnemiesPerEternalStarlight;
        InvokeDisplayAmountChanged();

        if (triggerCount <= 0 || Owner == null)
            return;

        await TokenEternalStarlight.GrantStacks(Owner, triggerCount);
    }
}
