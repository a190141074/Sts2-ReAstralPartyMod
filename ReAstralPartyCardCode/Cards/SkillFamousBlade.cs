using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillFamousBlade : AstralPartyCardModel
{
    private const int BaseDamage = 2;
    private const int MaxAuraToConsume = 2;
    private const int GawuCutterBonusDamageMin = 1;
    private const int GawuCutterBonusDamageMaxExclusive = 21;
    private const string BaseDescriptionKey = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FAMOUS_BLADE.description";

    private const string GawuCutterDescriptionKey =
        "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FAMOUS_BLADE.description_gawu_cutter";

    private const string GawuCutterBonusDamageSalt = "gawu_cutter_bonus_damage";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move)
    ];

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override string DefaultPortraitPath => "res://ReAstralPartyMod/images/card_portraits/skill_famous_blade.png";

    protected override string ResolveActivePortraitPath()
    {
        return DefaultPortraitPath;
    }

    protected override string? ResolveBetaPortraitPath()
    {
        return null;
    }

    public SkillFamousBlade() : base(
        0,
        CardType.Attack,
        CardRarity.Ancient,
        TargetType.AnyEnemy,
        false)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null || cardPlay.Target == null)
            return;

        var originalTarget = cardPlay.Target;
        var originalTargetKey = GetStableTargetKey(originalTarget);

        var attackCmd = CommonActions.CardAttack(this, cardPlay, 1);
        await attackCmd.Execute(choiceContext);

        var auraToConsume = Math.Min(MaxAuraToConsume, Owner.Creature.GetPowerAmount<SwordAuraPower>());
        if (auraToConsume > 0)
        {
            var auraPower = Owner.Creature.GetPower<SwordAuraPower>();
            if (auraPower != null)
                await PowerCmd.ModifyAmount(auraPower, -auraToConsume, Owner.Creature, this, false);
        }

        if (Owner.Creature.GetPowerAmount<SwordAuraPower>() < 3)
            await PowerCmd.Apply(
                ModelDb.Power<SwordAuraPower>().ToMutable(),
                Owner.Creature,
                1,
                Owner.Creature,
                this,
                false);

        var swordIntent = Owner.GetRelic<PersonalityDerivativeSwordIntent>();
        if (swordIntent != null)
            await swordIntent.OnFamousBladePlayed(choiceContext, originalTarget, auraToConsume, this);

        if (!IsGawuCutterTier())
            return;

        if (!CanApplyGawuCutterBonusDamage(originalTarget))
            return;

        var bonusDamage = RollGawuCutterBonusDamage(originalTargetKey);
        await CreatureCmd.Damage(
            choiceContext,
            originalTarget,
            bonusDamage,
            ValueProp.Unpowered,
            Owner.Creature,
            this);
    }

    public string GetDisplayTitle(string language)
    {
        if (!CanUseDynamicDisplay())
            return language switch
            {
                "zhs" => "名刀",
                _ => "Famous Blade"
            };

        return GetDisplayTier() switch
        {
            FamousBladeDisplayTier.Medium => language switch
            {
                "zhs" => "名刀·中",
                _ => "Famous Blade (Medium)"
            },
            FamousBladeDisplayTier.Large => language switch
            {
                "zhs" => "名刀·大",
                _ => "Famous Blade (Large)"
            },
            FamousBladeDisplayTier.ExtraLarge => language switch
            {
                "zhs" => "名刀·特大",
                _ => "Famous Blade (Extra Large)"
            },
            FamousBladeDisplayTier.GawuCutter => language switch
            {
                "zhs" => "名刀·嘎呜切",
                _ => "Famous Blade (Gawu-Cutter)"
            },
            _ => language switch
            {
                "zhs" => "名刀",
                _ => "Famous Blade"
            }
        };
    }

    public string GetDisplayDescriptionKey()
    {
        return CanUseDynamicDisplay() && IsGawuCutterTier() ? GawuCutterDescriptionKey : BaseDescriptionKey;
    }

    private int GetSwordIntentCounter()
    {
        if (ReferenceEquals(CanonicalInstance, this))
            return 0;

        return Owner?.GetRelic<PersonalityDerivativeSwordIntent>()
            ?.AstralParty_PersonalityDerivativeSwordIntentCounter ?? 0;
    }

    private FamousBladeDisplayTier GetDisplayTier()
    {
        var counter = GetSwordIntentCounter();

        return counter switch
        {
            <= 1 => FamousBladeDisplayTier.Basic,
            <= 3 => FamousBladeDisplayTier.Medium,
            <= 6 => FamousBladeDisplayTier.Large,
            <= 10 => FamousBladeDisplayTier.ExtraLarge,
            _ => FamousBladeDisplayTier.GawuCutter
        };
    }

    private bool IsGawuCutterTier()
    {
        return GetDisplayTier() == FamousBladeDisplayTier.GawuCutter;
    }

    private bool CanApplyGawuCutterBonusDamage(Creature target)
    {
        return Owner?.Creature != null
               && target.IsAlive
               && target.Side != Owner.Creature.Side;
    }

    private int RollGawuCutterBonusDamage(string targetKey)
    {
        return DeterministicMultiplayerChoiceHelper.RollDeterministically(
            GawuCutterBonusDamageMin,
            GawuCutterBonusDamageMaxExclusive,
            MainFile.ModId,
            Id.Entry,
            GawuCutterBonusDamageSalt,
            Owner?.RunState.Rng.StringSeed ?? string.Empty,
            Owner?.NetId ?? 0UL,
            Owner?.Creature?.CombatState?.RoundNumber ?? 0,
            CanonicalInstance?.Id.Entry ?? Id.Entry,
            targetKey);
    }

    private string GetStableTargetKey(Creature target)
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState?.Enemies != null)
        {
            var enemyIndex = combatState.Enemies.ToList().FindIndex(enemy => ReferenceEquals(enemy, target));
            if (enemyIndex >= 0)
                return $"enemy:{enemyIndex}";
        }

        return $"{target.Side}:{target.LogName}";
    }

    private bool CanUseDynamicDisplay()
    {
        return !ReferenceEquals(CanonicalInstance, this) && Owner != null;
    }

    private enum FamousBladeDisplayTier
    {
        Basic,
        Medium,
        Large,
        ExtraLarge,
        GawuCutter
    }
}

