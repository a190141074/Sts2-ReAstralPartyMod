using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class VariantPersonElena : PersonaRelicBase
{
    private const int EnergyGainInterval = 5;
    private const int CombatCycleResetThreshold = 3;
    private const decimal SharedBlockActOnePercent = 0.07m;
    private const decimal SharedBlockActTwoPercent = 0.13m;
    private const decimal SharedBlockActThreePlusPercent = 0.19m;
    private const decimal BaseTsunamiDamageReductionPercent = 0.16m;
    private const decimal BaseSettingSunBonusPercent = 0.5m;
    private const decimal BaseThunderChance = 0.55m;
    private const decimal BaseThunderPercent = 0.26m;
    private const decimal ConductingBonusDamagePerStack = 3m;
    private const decimal ThunderFieldExtraBaseDamagePercent = 0.60m;
    private const decimal ArcaneRiverWeakAmount = 1m;
    private const decimal ArcaneRiverHitDamage = 11m;
    private const decimal ArcaneRiverCurrentHpBlockPercent = 0.30m;
    private const decimal ExplosiveFlameHitDamage = 11m;
    private const decimal ExplosiveFlameBurnAmount = 1m;
    private const decimal ExplosiveFlameBonusBurnPercent = 0.70m;
    private const decimal ThunderFieldHitDamage = 11m;
    private const decimal ThunderFieldConductingAmount = 1m;
    private const decimal ArcaneGalaxyHitDamage = 11m;
    private const decimal ArcaneGalaxyGrowthPercentPerRelease = 0.05m;
    private const decimal ArcaneGalaxyStrengthLossAmount = 2m;
    private const decimal ArcaneGalaxyDamageTakenDuration = 3m;

    private static readonly LocString ElementSelectionTitle =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.selection_title");
    private static readonly LocString ElementSelectionSubtitle =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.selection_subtitle");
    private static readonly LocString BaseTsunamiName =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_tsunami_base_name");
    private static readonly LocString BaseTsunamiDescription =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_tsunami_base_description");
    private static readonly LocString BaseSettingSunName =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_setting_sun_base_name");
    private static readonly LocString BaseSettingSunDescription =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_setting_sun_base_description");
    private static readonly LocString BaseThundersBreathName =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_thunders_breath_base_name");
    private static readonly LocString BaseThundersBreathDescription =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_thunders_breath_base_description");
    private static readonly LocString StageTwoTsunamiName =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_tsunami_stage2_name");
    private static readonly LocString StageTwoTsunamiDescription =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_tsunami_stage2_description");
    private static readonly LocString StageTwoSettingSunName =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_setting_sun_stage2_name");
    private static readonly LocString StageTwoSettingSunDescription =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_setting_sun_stage2_description");
    private static readonly LocString StageTwoThundersBreathName =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_thunders_breath_stage2_name");
    private static readonly LocString StageTwoThundersBreathDescription =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_thunders_breath_stage2_description");
    private static readonly LocString StageThreeName =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_stage3_name");
    private static readonly LocString StageThreeTsunamiDescription =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_tsunami_stage3_description");
    private static readonly LocString StageThreeSettingSunDescription =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_setting_sun_stage3_description");
    private static readonly LocString StageThreeThundersBreathDescription =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_ELENA.element_thunders_breath_stage3_description");

    [SavedProperty] public int AstralParty_VariantPersonElenaSunsetGlowUsesRun { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonElenaCombatCycleUses { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonElenaCycleTsunamiSelections { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonElenaCycleSettingSunSelections { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonElenaCycleThundersBreathSelections { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonElenaRunTsunamiReleases { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonElenaRunSettingSunReleases { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonElenaRunThundersBreathReleases { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonElenaSunsetGlowPlaysThisTurn { get; set; }
    [SavedProperty] public bool AstralParty_VariantPersonElenaPendingResetAtNextOwnerTurnStart { get; set; }
    [SavedProperty] public bool AstralParty_VariantPersonElenaPendingThunderFieldGuarantee { get; set; }

    private CardModel? _pendingThunderCard;
    private uint? _pendingThunderTargetCombatId;
    private bool _pendingThunderGuaranteedThisCard;
    private bool _resolvingThunderDamage;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillSunsetGlow>(),
        HoverTipFactory.FromPower<TsunamiPower>(),
        HoverTipFactory.FromPower<SettingSunPower>(),
        HoverTipFactory.FromPower<ConductingPower>(),
        HoverTipFactory.FromPower<ThundersBreathPower>(),
        HoverTipFactory.FromPower<BlazingSolarBurnPower>(),
        HoverTipFactory.FromPower<SunsetGlowArcaneRiverDebuffPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ResetCombatState();
    }

    public override async Task BeforeCombatStart()
    {
        ResetCombatState();
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillSunsetGlow>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        ResetCombatState();
        await ClearElenaElementFieldStates();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        AstralParty_VariantPersonElenaSunsetGlowPlaysThisTurn = 0;
        ClearPendingThunderCard();

        if (!HasSunsetGlowInPrimaryPiles())
        {
            Flash();
            var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillSunsetGlow>(), Owner);
            await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
        }

        if (!AstralParty_VariantPersonElenaPendingResetAtNextOwnerTurnStart)
            return;

        AstralParty_VariantPersonElenaPendingResetAtNextOwnerTurnStart = false;
        await ClearElenaElementFieldStates();
        AstralParty_VariantPersonElenaCombatCycleUses = 0;
        AstralParty_VariantPersonElenaCycleTsunamiSelections = 0;
        AstralParty_VariantPersonElenaCycleSettingSunSelections = 0;
        AstralParty_VariantPersonElenaCycleThundersBreathSelections = 0;
        AstralParty_VariantPersonElenaPendingThunderFieldGuarantee = false;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        ClearPendingThunderCard();

        if (_resolvingThunderDamage || Owner?.Creature == null)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;
        if (!IsThunderBreathCostEligible(cardPlay.Card))
            return Task.CompletedTask;

        var target = cardPlay.Target;
        if (target == null || target.Side == Owner.Creature.Side || !target.IsAlive)
            return Task.CompletedTask;
        if (target.GetPowerAmount<ConductingPower>() <= 0m)
            return Task.CompletedTask;
        if (Owner.Creature.GetPowerAmount<ThundersBreathPower>() <= 0m && !AstralParty_VariantPersonElenaPendingThunderFieldGuarantee)
            return Task.CompletedTask;

        _pendingThunderCard = cardPlay.Card;
        _pendingThunderTargetCombatId = target.CombatId;
        _pendingThunderGuaranteedThisCard = AstralParty_VariantPersonElenaPendingThunderFieldGuarantee;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != _pendingThunderCard || Owner?.Creature == null)
            return;

        var target = ResolveThunderBreathTarget(cardPlay.Target);
        var shouldGuarantee = _pendingThunderGuaranteedThisCard;
        if (shouldGuarantee)
            AstralParty_VariantPersonElenaPendingThunderFieldGuarantee = false;

        if (target == null || target.Side == Owner.Creature.Side)
        {
            ClearPendingThunderCard();
            return;
        }

        var shouldTrigger = shouldGuarantee || RollThunderBreathTrigger(cardPlay.Card, target);
        if (!shouldTrigger)
        {
            ClearPendingThunderCard();
            return;
        }

        var bonusDamage = CalculateThunderBreathBonusDamage(cardPlay.Card, target, shouldGuarantee);
        if (bonusDamage <= 0 || !target.IsAlive)
        {
            ClearPendingThunderCard();
            return;
        }

        Flash();
        _resolvingThunderDamage = true;
        try
        {
            await CreatureCmd.Damage(
                choiceContext,
                target,
                bonusDamage,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim,
                Owner!.Creature,
                null);
        }
        finally
        {
            _resolvingThunderDamage = false;
            ClearPendingThunderCard();
        }
    }

    public bool CanPlaySunsetGlowThisTurn(CardModel card)
    {
        return Owner?.Creature?.CombatState != null
               && AstralParty_VariantPersonElenaSunsetGlowPlaysThisTurn < GetSunsetGlowPlayLimitThisTurn()
               && card.Owner == Owner;
    }

    public async Task ResolveSunsetGlowPlayed(PlayerChoiceContext choiceContext, CardModel sourceCard, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null || AstralParty_VariantPersonElenaSunsetGlowPlaysThisTurn >= GetSunsetGlowPlayLimitThisTurn())
            return;

        AstralParty_VariantPersonElenaSunsetGlowPlaysThisTurn++;
        AstralParty_VariantPersonElenaSunsetGlowUsesRun++;

        if (AstralParty_VariantPersonElenaSunsetGlowUsesRun % EnergyGainInterval == 0)
            await GrantEnergyToAllPlayers();

        var sharedBlockBase = Math.Max(0m, Owner.Creature.Block);
        var sharedBlock = StableNumericStateHelper.FloorToNonNegativeInt(sharedBlockBase * GetSharedBlockPercentForCurrentAct());

        var branchId = await SelectElementBranch();
        if (string.IsNullOrWhiteSpace(branchId))
            branchId = SunsetGlowElementSelectionHelper.TsunamiBranch;

        if (sharedBlock > 0)
            await GrantSharedBlock(sharedBlock);

        await ResolveSelectedBranch(choiceContext, sourceCard, branchId);
        AstralParty_VariantPersonElenaCombatCycleUses++;
        if (AstralParty_VariantPersonElenaCombatCycleUses >= CombatCycleResetThreshold)
            AstralParty_VariantPersonElenaPendingResetAtNextOwnerTurnStart = true;
    }

    private async Task<string?> SelectElementBranch()
    {
        if (Owner == null)
            return null;

        var cycleStage = Math.Clamp(AstralParty_VariantPersonElenaCombatCycleUses + 1, 1, 3);
        var options = SunsetGlowElementSelectionHelper.BuildOptions(
            GetDisplayedElementName(SunsetGlowElementSelectionHelper.TsunamiBranch),
            GetDisplayedElementDescription(SunsetGlowElementSelectionHelper.TsunamiBranch),
            GetDisplayedElementName(SunsetGlowElementSelectionHelper.SettingSunBranch),
            GetDisplayedElementDescription(SunsetGlowElementSelectionHelper.SettingSunBranch),
            GetDisplayedElementName(SunsetGlowElementSelectionHelper.ThundersBreathBranch),
            GetDisplayedElementDescription(SunsetGlowElementSelectionHelper.ThundersBreathBranch),
            cycleStage);

        return await SunsetGlowElementSelectionHelper.SelectBranchAsync(
            Owner,
            ElementSelectionTitle.GetRawText(),
            ElementSelectionSubtitle.GetRawText(),
            options,
            $"{Id.Entry}.round.{Owner.Creature?.CombatState?.RoundNumber ?? 0}.cycle.{AstralParty_VariantPersonElenaCombatCycleUses}");
    }

    private async Task ResolveSelectedBranch(PlayerChoiceContext choiceContext, CardModel sourceCard, string branchId)
    {
        IncrementRunReleaseCounter(branchId);
        await ApplyBaseElementEffect(sourceCard, branchId);

        var cycleCount = GetCycleSelectionCount(branchId);
        IncrementCycleSelectionCounter(branchId);
        var nextCycleCount = cycleCount + 1;
        if (nextCycleCount == 2)
        {
            await ResolveStageTwoEffect(choiceContext, sourceCard, branchId);
            return;
        }

        if (nextCycleCount >= 3)
            await ResolveStageThreeEffect(choiceContext, sourceCard, branchId);
    }

    private async Task ApplyBaseElementEffect(CardModel sourceCard, string branchId)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        switch (branchId)
        {
            case SunsetGlowElementSelectionHelper.TsunamiBranch:
                foreach (var player in EventCombatTargetHelper.GetAlivePlayers(Owner.Creature.CombatState))
                    await PowerCmd.Apply<TsunamiPower>(player.Creature, 1m, Owner.Creature, sourceCard, false);
                break;
            case SunsetGlowElementSelectionHelper.SettingSunBranch:
                foreach (var enemy in EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature))
                {
                    await PowerCmd.Apply<SettingSunPower>(enemy, 1m, Owner.Creature, sourceCard, false);
                    await PowerCmd.Apply<BlazingSolarBurnPower>(enemy, 1m, Owner.Creature, sourceCard, false);
                }
                break;
            case SunsetGlowElementSelectionHelper.ThundersBreathBranch:
                foreach (var enemy in EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature))
                    await PowerCmd.Apply<ConductingPower>(enemy, 1m, Owner.Creature, sourceCard, false);
                foreach (var player in EventCombatTargetHelper.GetAlivePlayers(Owner.Creature.CombatState))
                    await PowerCmd.Apply<ThundersBreathPower>(player.Creature, 1m, Owner.Creature, sourceCard, false);
                break;
        }
    }

    private async Task ResolveStageTwoEffect(PlayerChoiceContext choiceContext, CardModel sourceCard, string branchId)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        switch (branchId)
        {
            case SunsetGlowElementSelectionHelper.TsunamiBranch:
                await DamageAllEnemies(choiceContext, ArcaneRiverHitDamage, sourceCard);
                foreach (var enemy in EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature))
                    await PowerCmd.Apply<WeakPower>(enemy, ArcaneRiverWeakAmount, Owner.Creature, sourceCard, false);
                var blockAmount = StableNumericStateHelper.FloorToNonNegativeInt(Math.Max(0m, Owner.Creature.CurrentHp) * ArcaneRiverCurrentHpBlockPercent);
                if (blockAmount > 0)
                    await GrantBlockToAllPlayers(blockAmount);
                break;
            case SunsetGlowElementSelectionHelper.SettingSunBranch:
                await DamageAllEnemies(choiceContext, ExplosiveFlameHitDamage, sourceCard);
                foreach (var enemy in EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature).ToList())
                {
                    await PowerCmd.Apply<BlazingSolarBurnPower>(enemy, ExplosiveFlameBurnAmount, Owner.Creature, sourceCard, false);
                    var extraDamage = StableNumericStateHelper.FloorToNonNegativeInt(enemy.GetPowerAmount<BlazingSolarBurnPower>() * ExplosiveFlameBonusBurnPercent);
                    if (extraDamage > 0 && enemy.IsAlive)
                        await CreatureCmd.Damage(choiceContext, enemy, extraDamage, ValueProp.Unpowered, Owner.Creature, sourceCard);
                }
                break;
            case SunsetGlowElementSelectionHelper.ThundersBreathBranch:
                await DamageAllEnemies(choiceContext, ThunderFieldHitDamage, sourceCard);
                foreach (var enemy in EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature))
                    await PowerCmd.Apply<ConductingPower>(enemy, ThunderFieldConductingAmount, Owner.Creature, sourceCard, false);
                AstralParty_VariantPersonElenaPendingThunderFieldGuarantee = true;
                break;
        }
    }

    private async Task ResolveStageThreeEffect(PlayerChoiceContext choiceContext, CardModel sourceCard, string branchId)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var growthPercent = 1m + GetRunReleaseCounter(branchId) * ArcaneGalaxyGrowthPercentPerRelease;
        var damage = StableNumericStateHelper.FloorToNonNegativeInt(ArcaneGalaxyHitDamage * growthPercent);
        if (damage > 0)
            await DamageAllEnemies(choiceContext, damage, sourceCard);

        foreach (var enemy in EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature))
        {
            await AstralTemporaryStrengthLossPower.Apply(enemy, ArcaneGalaxyStrengthLossAmount, this, Owner.Creature, sourceCard, false);
            await PowerCmd.Apply<SunsetGlowArcaneRiverDebuffPower>(
                enemy,
                ArcaneGalaxyDamageTakenDuration,
                Owner.Creature,
                sourceCard,
                false);
        }
    }

    private bool RollThunderBreathTrigger(CardModel cardSource, Creature target)
    {
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            1000,
            MainFile.ModId,
            Id.Entry,
            nameof(VariantPersonElena),
            "thunder",
            Owner.RunState?.Rng.StringSeed ?? string.Empty,
            Owner.NetId,
            Owner.Creature.CombatState?.RoundNumber ?? 0,
            cardSource.Id.Entry,
            target.CombatId ?? uint.MaxValue);
        return roll < StableNumericStateHelper.FloorToNonNegativeInt(BaseThunderChance * 1000m);
    }

    private int CalculateThunderBreathBonusDamage(CardModel cardSource, Creature target, bool guaranteed)
    {
        var baseDamage = GetCardPanelBaseDamage(cardSource);
        var bonusDamage = baseDamage * BaseThunderPercent;
        if (guaranteed)
            bonusDamage += baseDamage * ThunderFieldExtraBaseDamagePercent;

        bonusDamage += target.GetPowerAmount<ConductingPower>() * ConductingBonusDamagePerStack;
        return StableNumericStateHelper.FloorToNonNegativeInt(bonusDamage);
    }

    private decimal GetCardPanelBaseDamage(CardModel cardSource)
    {
        return cardSource.DynamicVars.TryGetValue("Damage", out var damageVar)
            ? damageVar.BaseValue
            : 0m;
    }

    private bool IsThunderBreathCostEligible(CardModel cardSource)
    {
        return cardSource.EnergyCost.CostsX || cardSource.EnergyCost.GetResolved() >= 1m;
    }

    private Creature? ResolveThunderBreathTarget(Creature? fallbackTarget)
    {
        if (Owner?.Creature?.CombatState == null)
            return fallbackTarget;
        if (_pendingThunderTargetCombatId == null)
            return fallbackTarget;

        return Owner.Creature.CombatState.Creatures.FirstOrDefault(creature =>
            creature.CombatId == _pendingThunderTargetCombatId);
    }

    private async Task DamageAllEnemies(PlayerChoiceContext choiceContext, decimal damage, CardModel sourceCard)
    {
        if (Owner?.Creature?.CombatState == null || damage <= 0m)
            return;

        var totalDamage = StableNumericStateHelper.FloorToNonNegativeInt(damage);
        if (totalDamage <= 0)
            return;

        var enemies = EventCombatTargetHelper
            .GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature)
            .ToList();
        if (enemies.Count == 0)
            return;

        if (enemies.Count == 1)
        {
            await CreatureCmd.Damage(choiceContext, enemies[0], totalDamage, ValueProp.Move, Owner.Creature, sourceCard);
            return;
        }

        var assignedDamage = new int[enemies.Count];
        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        var rngSeed = Owner.RunState?.Rng.StringSeed ?? string.Empty;
        for (var pointIndex = 0; pointIndex < totalDamage; pointIndex++)
        {
            var targetIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                enemies.Count,
                MainFile.ModId,
                Id.Entry,
                nameof(VariantPersonElena),
                "sunset_glow_random_split",
                rngSeed,
                Owner.NetId,
                roundNumber,
                AstralParty_VariantPersonElenaSunsetGlowUsesRun,
                AstralParty_VariantPersonElenaCombatCycleUses,
                sourceCard.Id.Entry,
                totalDamage,
                pointIndex);
            assignedDamage[targetIndex]++;
        }

        for (var index = 0; index < enemies.Count; index++)
        {
            var assigned = assignedDamage[index];
            if (assigned <= 0 || !enemies[index].IsAlive)
                continue;

            await CreatureCmd.Damage(choiceContext, enemies[index], assigned, ValueProp.Move, Owner.Creature, sourceCard);
        }
    }

    private async Task GrantEnergyToAllPlayers()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        foreach (var player in EventCombatTargetHelper.GetAlivePlayers(Owner.Creature.CombatState))
            await PlayerCmd.GainEnergy(1m, player);
    }

    private async Task GrantSharedBlock(int amount)
    {
        await GrantBlockToAllPlayers(amount);
    }

    private async Task GrantBlockToAllPlayers(int amount)
    {
        if (Owner?.Creature?.CombatState == null || amount <= 0)
            return;

        foreach (var player in EventCombatTargetHelper.GetAlivePlayers(Owner.Creature.CombatState))
            await CreatureCmd.GainBlock(player.Creature, amount, ValueProp.Move, null);
    }

    private async Task ClearElenaElementFieldStates()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var creatures = EventCombatTargetHelper.GetAliveCreaturesExcludingPlayerSummons(Owner.Creature.CombatState);
        foreach (var creature in creatures)
        {
            await CandyMachineHelper.RemovePowerIfPresent<TsunamiPower>(creature);
            await CandyMachineHelper.RemovePowerIfPresent<SettingSunPower>(creature);
            await CandyMachineHelper.RemovePowerIfPresent<ThundersBreathPower>(creature);
            await CandyMachineHelper.RemovePowerIfPresent<ConductingPower>(creature);
            await CandyMachineHelper.RemovePowerIfPresent<BlazingSolarBurnPower>(creature);
        }
    }

    private string GetDisplayedElementName(string branchId)
    {
        var count = GetCycleSelectionCount(branchId);
        return count switch
        {
            <= 0 => branchId switch
            {
                SunsetGlowElementSelectionHelper.TsunamiBranch => BaseTsunamiName.GetRawText(),
                SunsetGlowElementSelectionHelper.SettingSunBranch => BaseSettingSunName.GetRawText(),
                _ => BaseThundersBreathName.GetRawText()
            },
            1 => branchId switch
            {
                SunsetGlowElementSelectionHelper.TsunamiBranch => StageTwoTsunamiName.GetRawText(),
                SunsetGlowElementSelectionHelper.SettingSunBranch => StageTwoSettingSunName.GetRawText(),
                _ => StageTwoThundersBreathName.GetRawText()
            },
            _ => StageThreeName.GetRawText()
        };
    }

    private string GetDisplayedElementDescription(string branchId)
    {
        var count = GetCycleSelectionCount(branchId);
        return count switch
        {
            <= 0 => branchId switch
            {
                SunsetGlowElementSelectionHelper.TsunamiBranch => BaseTsunamiDescription.GetRawText(),
                SunsetGlowElementSelectionHelper.SettingSunBranch => BaseSettingSunDescription.GetRawText(),
                _ => BaseThundersBreathDescription.GetRawText()
            },
            1 => branchId switch
            {
                SunsetGlowElementSelectionHelper.TsunamiBranch => StageTwoTsunamiDescription.GetRawText(),
                SunsetGlowElementSelectionHelper.SettingSunBranch => StageTwoSettingSunDescription.GetRawText(),
                _ => StageTwoThundersBreathDescription.GetRawText()
            },
            _ => branchId switch
            {
                SunsetGlowElementSelectionHelper.TsunamiBranch => StageThreeTsunamiDescription.GetRawText(),
                SunsetGlowElementSelectionHelper.SettingSunBranch => StageThreeSettingSunDescription.GetRawText(),
                _ => StageThreeThundersBreathDescription.GetRawText()
            }
        };
    }

    private bool HasSunsetGlowInPrimaryPiles()
    {
        if (Owner == null)
            return false;

        var cardId = ModelDb.GetId<SkillSunsetGlow>();
        return PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Any(card => card.Owner == Owner && (card.CanonicalInstance?.Id ?? card.Id) == cardId);
    }

    private void IncrementCycleSelectionCounter(string branchId)
    {
        switch (branchId)
        {
            case SunsetGlowElementSelectionHelper.TsunamiBranch:
                AstralParty_VariantPersonElenaCycleTsunamiSelections++;
                break;
            case SunsetGlowElementSelectionHelper.SettingSunBranch:
                AstralParty_VariantPersonElenaCycleSettingSunSelections++;
                break;
            case SunsetGlowElementSelectionHelper.ThundersBreathBranch:
                AstralParty_VariantPersonElenaCycleThundersBreathSelections++;
                break;
        }
    }

    private int GetCycleSelectionCount(string branchId)
    {
        return branchId switch
        {
            SunsetGlowElementSelectionHelper.TsunamiBranch => AstralParty_VariantPersonElenaCycleTsunamiSelections,
            SunsetGlowElementSelectionHelper.SettingSunBranch => AstralParty_VariantPersonElenaCycleSettingSunSelections,
            _ => AstralParty_VariantPersonElenaCycleThundersBreathSelections
        };
    }

    private void IncrementRunReleaseCounter(string branchId)
    {
        switch (branchId)
        {
            case SunsetGlowElementSelectionHelper.TsunamiBranch:
                AstralParty_VariantPersonElenaRunTsunamiReleases++;
                break;
            case SunsetGlowElementSelectionHelper.SettingSunBranch:
                AstralParty_VariantPersonElenaRunSettingSunReleases++;
                break;
            case SunsetGlowElementSelectionHelper.ThundersBreathBranch:
                AstralParty_VariantPersonElenaRunThundersBreathReleases++;
                break;
        }
    }

    private int GetRunReleaseCounter(string branchId)
    {
        return branchId switch
        {
            SunsetGlowElementSelectionHelper.TsunamiBranch => AstralParty_VariantPersonElenaRunTsunamiReleases,
            SunsetGlowElementSelectionHelper.SettingSunBranch => AstralParty_VariantPersonElenaRunSettingSunReleases,
            _ => AstralParty_VariantPersonElenaRunThundersBreathReleases
        };
    }

    private decimal GetSharedBlockPercentForCurrentAct()
    {
        var actNumber = Math.Max((Owner?.RunState?.CurrentActIndex ?? 0) + 1, 1);
        return actNumber switch
        {
            <= 1 => SharedBlockActOnePercent,
            2 => SharedBlockActTwoPercent,
            _ => SharedBlockActThreePlusPercent
        };
    }

    private void ResetCombatState()
    {
        AstralParty_VariantPersonElenaCombatCycleUses = 0;
        AstralParty_VariantPersonElenaCycleTsunamiSelections = 0;
        AstralParty_VariantPersonElenaCycleSettingSunSelections = 0;
        AstralParty_VariantPersonElenaCycleThundersBreathSelections = 0;
        AstralParty_VariantPersonElenaSunsetGlowPlaysThisTurn = 0;
        AstralParty_VariantPersonElenaPendingResetAtNextOwnerTurnStart = false;
        AstralParty_VariantPersonElenaPendingThunderFieldGuarantee = false;
        ClearPendingThunderCard();
    }

    public int GetSunsetGlowPlayLimitThisTurn()
    {
        if (Owner?.Creature?.CombatState == null)
            return 0;

        var limit = 1;
        var worldTears = Owner.GetRelic<JewelryWorldTears>();
        if (worldTears?.ShouldGrantExtraSunsetGlowPlayThisTurn() == true)
            limit++;

        return limit;
    }

    private void ClearPendingThunderCard()
    {
        _pendingThunderCard = null;
        _pendingThunderTargetCombatId = null;
        _pendingThunderGuaranteedThisCard = false;
        _resolvingThunderDamage = false;
    }
}
