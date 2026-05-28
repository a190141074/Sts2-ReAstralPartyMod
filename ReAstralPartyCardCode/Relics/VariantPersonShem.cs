using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonShem : CooldownPersonaRelicBase
{
    private const decimal AttackDamageBonusPerEnemy = 0.15m;

    [SavedProperty] public int AstralParty_VariantPersonShemCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonShemPendingCombatStartCard { get; set; }
    [SavedProperty] public bool AstralParty_VariantPersonShemPendingSplashThisTurn { get; set; }

    private CardModel? _pendingAttackCard;
    private Creature? _pendingAttackTarget;
    private bool _pendingAttackShouldSplash;
    private bool _pendingAttackSplashResolved;
    private bool _isResolvingSplashDamage;

    protected override string RelicId => "variant_person_shem";

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonShemCounter;
        set => AstralParty_VariantPersonShemCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonShemPendingCombatStartCard;
        set => AstralParty_VariantPersonShemPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 5;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillInnocentWish>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeAbyssWhisper>(),
        HoverTipFactory.FromPower<WhisperPower>(),
        HoverTipFactory.FromPower<InnocentPureLightPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await EnsureAbyssWhisper();
        ResetCombatState();
    }

    public override async Task BeforeCombatStart()
    {
        await EnsureAbyssWhisper();
        ResetCombatState();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        ResetCombatState();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;
        if (_isResolvingSplashDamage || !AstralParty_VariantPersonShemPendingSplashThisTurn)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;
        if (cardPlay.Target == null || cardPlay.Target.Side == Owner.Creature.Side)
            return Task.CompletedTask;

        _pendingAttackCard = cardPlay.Card;
        _pendingAttackTarget = cardPlay.Target;
        _pendingAttackShouldSplash = true;
        _pendingAttackSplashResolved = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null || player != Owner)
            return;

        await ClearCoinResultPowers();
        AstralParty_VariantPersonShemPendingSplashThisTurn = RollHeadsForTurn();
        if (AstralParty_VariantPersonShemPendingSplashThisTurn)
            await PowerCmd.Apply<CoinFrontPower>(Owner.Creature, 1m, Owner.Creature, null, false);
        else
            await PowerCmd.Apply<CoinBackPower>(Owner.Creature, 1m, Owner.Creature, null, false);
        ClearPendingAttackState();
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (_isResolvingSplashDamage || !_pendingAttackShouldSplash || _pendingAttackSplashResolved)
            return;
        if (dealer != Owner.Creature || target.Side == Owner.Creature.Side)
            return;
        if (cardSource != _pendingAttackCard || target != _pendingAttackTarget)
            return;

        _pendingAttackSplashResolved = true;
        AstralParty_VariantPersonShemPendingSplashThisTurn = false;
        await RemovePowerIfPresent<CoinFrontPower>();

        var splashDamage = result.TotalDamage + result.OverkillDamage;
        if (splashDamage <= 0m)
            return;

        var splashTargets = SelectSplashTargets(target, cardSource);
        if (splashTargets.Count == 0)
            return;

        Flash();
        _isResolvingSplashDamage = true;
        try
        {
            await CreatureCmd.Damage(
                choiceContext,
                splashTargets,
                splashDamage,
                ValueProp.Unpowered | ValueProp.Move,
                Owner.Creature,
                cardSource);
        }
        finally
        {
            _isResolvingSplashDamage = false;
        }
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != _pendingAttackCard)
            return Task.CompletedTask;

        try
        {
            if (_pendingAttackShouldSplash && !_pendingAttackSplashResolved)
            {
                AstralParty_VariantPersonShemPendingSplashThisTurn = false;
                _ = RemovePowerIfPresent<CoinFrontPower>();
            }
        }
        finally
        {
            ClearPendingAttackState();
        }

        return Task.CompletedTask;
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
        if (cardSource?.Owner != Owner || cardSource.Type != CardType.Attack)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side || amount <= 0m)
            return 0m;

        var enemyCount = CombatTargetOrdering.GetLivingOpponentsStable(Owner.Creature).Count;
        if (enemyCount <= 1)
            return 0m;

        return amount * AttackDamageBonusPerEnemy * enemyCount;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillInnocentWish>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private async Task EnsureAbyssWhisper()
    {
        if (Owner == null)
            return;

        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeAbyssWhisper>(Owner);
    }

    private bool RollHeadsForTurn()
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null)
            return false;

        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            2,
            MainFile.ModId,
            Id.Entry,
            nameof(VariantPersonShem),
            "coin",
            Owner?.RunState?.Rng.StringSeed ?? string.Empty,
            Owner?.NetId ?? 0UL,
            combatState.RoundNumber);
        return roll == 0;
    }

    private List<Creature> SelectSplashTargets(Creature primaryTarget, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return [];

        var candidates = CombatTargetOrdering.GetLivingOpponentsStable(Owner.Creature)
            .Where(enemy => enemy != primaryTarget)
            .ToList();
        if (candidates.Count == 0)
            return [];

        return DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                candidates,
                GetStableEnemyKey,
                MainFile.ModId,
                Id.Entry,
                nameof(VariantPersonShem),
                "splash",
                Owner.RunState?.Rng.StringSeed ?? string.Empty,
                Owner.NetId,
                Owner.Creature.CombatState?.RoundNumber ?? 0,
                primaryTarget.CombatId ?? uint.MaxValue,
                cardSource?.Id.Entry ?? string.Empty)
            .Take(2)
            .ToList();
    }

    private static string GetStableEnemyKey(Creature enemy)
    {
        return $"{enemy.CombatId ?? uint.MaxValue}|{enemy.ModelId}|{enemy.SlotName ?? string.Empty}";
    }

    private void ClearPendingAttackState()
    {
        _pendingAttackCard = null;
        _pendingAttackTarget = null;
        _pendingAttackShouldSplash = false;
        _pendingAttackSplashResolved = false;
    }

    private void ResetCombatState()
    {
        AstralParty_VariantPersonShemPendingSplashThisTurn = false;
        ClearPendingAttackState();
        _isResolvingSplashDamage = false;
    }

    private async Task ClearCoinResultPowers()
    {
        await RemovePowerIfPresent<CoinFrontPower>();
        await RemovePowerIfPresent<CoinBackPower>();
    }

    private async Task RemovePowerIfPresent<TPower>()
        where TPower : PowerModel
    {
        var power = Owner?.Creature?.GetPower<TPower>();
        if (power != null)
            await PowerCmd.Remove(power);
    }
}
