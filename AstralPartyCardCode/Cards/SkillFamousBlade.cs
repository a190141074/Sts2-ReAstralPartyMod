using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Relics;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(ColorlessCardPool))]
public class SkillFamousBlade : AstralPartyCardModel
{
    private const int MaxAuraToConsume = 2;
    private int _currentDamage = 1;
    private int _increasedDamage;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new ExtraDamageVar(0m),
        new CalculationBaseVar(1m),
        new CalculationExtraVar(0m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((_, _) => 1m)
    ];

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    public int CurrentDamage
    {
        get
        {
            UpdateDamage();
            return _currentDamage;
        }
    }

    public int IncreasedDamage
    {
        get
        {
            UpdateDamage();
            return _increasedDamage;
        }
    }

    public override string PortraitPath => GetPortraitPath();

    public SkillFamousBlade() : base(
        0,
        CardType.Attack,
        CardRarity.Rare,
        TargetType.AnyEnemy,
        false)
    {
        UpdateDamage();
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Target == null) return;

        UpdateDamage();

        var attackCmd = CommonActions.CardAttack(this, cardPlay, 1);
        await attackCmd.Execute(choiceContext);

        var auraToConsume = Math.Min(MaxAuraToConsume, Owner.Creature.GetPowerAmount<SwordAuraPower>());
        if (auraToConsume > 0)
        {
            var auraPower = Owner.Creature.GetPower<SwordAuraPower>();
            if (auraPower != null) await PowerCmd.ModifyAmount(auraPower, -auraToConsume, Owner.Creature, this, false);

            BuffFromPlay(auraToConsume);
        }

        if (Owner.Creature.GetPowerAmount<SwordAuraPower>() < 3)
            await PowerCmd.Apply(
                ModelDb.Power<SwordAuraPower>().ToMutable(),
                Owner.Creature,
                1,
                Owner.Creature,
                this,
                false);

        UpdateDamage();
    }

    public void BuffFromPlay(int consumedAura)
    {
        if (consumedAura <= 0) return;

        var relic = Owner?.GetRelic<PersonSamuraiPrawn>();
        relic?.IncreaseFamousBladeConsumedAura(consumedAura);
        UpdateDamage();
    }

    public void UpdateDamage()
    {
        var increasedDamage = GetDisplayIncrease();
        _increasedDamage = increasedDamage;
        _currentDamage = 1 + increasedDamage;

        DynamicVars["Damage"].BaseValue = 1m;
        DynamicVars["ExtraDamage"].BaseValue = increasedDamage;
        DynamicVars["CalculationBase"].BaseValue = 1m;
        DynamicVars["CalculationExtra"].BaseValue = increasedDamage;
    }

    public string GetDisplayTitle(string language)
    {
        UpdateDamage();
        var damage = CurrentDamage;

        return language switch
        {
            "zhs" => damage switch
            {
                <= 2 => "\u540d\u5200",
                <= 4 => "\u540d\u5200\u00b7\u4e2d",
                <= 7 => "\u540d\u5200\u00b7\u5927",
                <= 11 => "\u540d\u5200\u00b7\u7279\u5927",
                _ => "\u540d\u5200\u00b7\u560e\u545c\u5207"
            },
            _ => damage switch
            {
                <= 2 => "Famous Blade",
                <= 4 => "Famous Blade (Medium)",
                <= 7 => "Famous Blade (Large)",
                <= 11 => "Famous Blade (Extra Large)",
                _ => "Famous Blade (Gawu-Cutter)"
            }
        };
    }

    private int GetDisplayIncrease()
    {
        if (ReferenceEquals(CanonicalInstance, this)) return 0;

        var damage = Owner?.GetRelic<PersonSamuraiPrawn>()?.GetFamousBladeDamage() ?? 1;
        return Math.Max(0, damage - 1);
    }

    private string GetPortraitPath()
    {
        UpdateDamage();
        var damage = CurrentDamage;
        var suffix = damage switch
        {
            <= 2 => "skill_famous_blade",
            <= 4 => "famous_blade_medium",
            <= 7 => "famous_blade_large",
            <= 11 => "famous_blade_extra_large",
            _ => "famous_blade_gawu_cutter"
        };

        return $"res://AstralPartyMod/images/card_portraits/{suffix}.png";
    }
}