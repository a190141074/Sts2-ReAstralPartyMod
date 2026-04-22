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

[Pool(typeof(EventCardPool))]
public class SkillFamousBlade : AstralPartyCardModel
{
    private const int BaseDamage = 2;
    private const int MaxAuraToConsume = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move)
    ];

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [AstralPartyMod.AstralPartyCardCode.Keywords.AstralKeywords.AstralCooldown];

    public override string PortraitPath => GetPortraitPath();

    public SkillFamousBlade() : base(
        0,
        CardType.Attack,
        CardRarity.Rare,
        TargetType.AnyEnemy,
        false)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Target == null) return;

        var attackCmd = CommonActions.CardAttack(this, cardPlay, 1);
        await attackCmd.Execute(choiceContext);

        var auraToConsume = Math.Min(MaxAuraToConsume, Owner.Creature.GetPowerAmount<SwordAuraPower>());
        if (auraToConsume > 0)
        {
            var auraPower = Owner.Creature.GetPower<SwordAuraPower>();
            if (auraPower != null) await PowerCmd.ModifyAmount(auraPower, -auraToConsume, Owner.Creature, this, false);
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
            await swordIntent.OnFamousBladePlayed(choiceContext, cardPlay.Target, auraToConsume, this);
    }

    public string GetDisplayTitle(string language)
    {
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

    private int GetSwordIntentCounter()
    {
        if (ReferenceEquals(CanonicalInstance, this))
            return 0;

        return Owner?.GetRelic<PersonalityDerivativeSwordIntent>()?.AstralParty_PersonalityDerivativeSwordIntentCounter ?? 0;
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

    private string GetPortraitPath()
    {
        return GetDisplayTier() switch
        {
            FamousBladeDisplayTier.Medium => "res://AstralPartyMod/images/card_portraits/famous_blade_medium.png",
            FamousBladeDisplayTier.Large => "res://AstralPartyMod/images/card_portraits/famous_blade_large.png",
            FamousBladeDisplayTier.ExtraLarge => "res://AstralPartyMod/images/card_portraits/famous_blade_extra_large.png",
            FamousBladeDisplayTier.GawuCutter => "res://AstralPartyMod/images/card_portraits/famous_blade_gawu_cutter.png",
            _ => "res://AstralPartyMod/images/card_portraits/skill_famous_blade.png"
        };
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
