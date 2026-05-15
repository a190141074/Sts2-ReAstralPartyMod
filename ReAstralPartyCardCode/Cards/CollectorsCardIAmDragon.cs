using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class CollectorsCardIAmDragon : AstralPartyCardModel
{
    private const decimal BaseDamage = 7m;
    private const decimal DamageUpgrade = 2m;
    private const decimal BaseStrengthLoss = 1m;
    private const decimal StrengthLossUpgrade = 1m;
    private const decimal BaseBonusDamage = 3m;
    private const decimal BonusDamageUpgrade = 1m;

    private static readonly ConcurrentDictionary<string, bool> LostHpThisTurnByPlayer = new();

    private bool _isAutoPlaying;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override bool ShouldReceiveCombatHooks => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new IntVar("StrengthLoss", BaseStrengthLoss),
        new IntVar("BonusDamage", BaseBonusDamage)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public CollectorsCardIAmDragon() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(DamageUpgrade);
        DynamicVars["StrengthLoss"].UpgradeValueBy(StrengthLossUpgrade);
        DynamicVars["BonusDamage"].UpgradeValueBy(BonusDamageUpgrade);
    }

    public override Task BeforeCombatStart()
    {
        ResetLostHpThisTurnFlag();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetLostHpThisTurnFlag();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            ResetLostHpThisTurnFlag();

        return Task.CompletedTask;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (Owner?.Creature == null || creature != Owner.Creature)
            return;
        if (delta >= 0m)
            return;

        SetLostHpThisTurnFlag(true);
        if (_isAutoPlaying)
            return;
        if (Pile?.Type != PileType.Exhaust)
            return;

        _isAutoPlaying = true;
        try
        {
            await CardCmd.AutoPlay(
                new ThrowingPlayerChoiceContext(),
                this,
                Owner.Creature,
                AutoPlayType.Default,
                false,
                false
            );
        }
        finally
        {
            _isAutoPlaying = false;
        }
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (cardSource != this)
            return 0m;

        return HasLostHpThisTurn() ? DynamicVars["BonusDamage"].BaseValue : 0m;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (card != this)
            return (pileType, position);

        return (PileType.Exhaust, CardPilePosition.Top);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var enemies = Owner.Creature.CombatState
            .GetOpponentsOf(Owner.Creature)
            .Where(creature => creature.IsAlive)
            .ToList();

        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply(
                ModelDb.Power<IAmDragonTemporaryStrengthLossPower>().ToMutable(),
                enemy,
                DynamicVars["StrengthLoss"].BaseValue,
                Owner.Creature,
                this,
                false
            );

            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                DynamicVars["Damage"].BaseValue,
                ValueProp.Move,
                Owner.Creature,
                this
            );
        }
    }

    private bool HasLostHpThisTurn()
    {
        var ownerKey = GetOwnerKey();
        return ownerKey != null
               && LostHpThisTurnByPlayer.TryGetValue(ownerKey, out var lostHpThisTurn)
               && lostHpThisTurn;
    }

    private void ResetLostHpThisTurnFlag()
    {
        var ownerKey = GetOwnerKey();
        if (ownerKey == null)
            return;

        LostHpThisTurnByPlayer[ownerKey] = false;
    }

    private void SetLostHpThisTurnFlag(bool value)
    {
        var ownerKey = GetOwnerKey();
        if (ownerKey == null)
            return;

        LostHpThisTurnByPlayer[ownerKey] = value;
    }

    private string? GetOwnerKey()
    {
        return Owner == null ? null : Owner.NetId.ToString();
    }
}
