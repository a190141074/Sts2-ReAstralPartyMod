using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

public abstract class UltimateSkillCardModel : AstralPartyCardModel
{
    private const string ChargeVarName = "UltimateCharge";
    private static readonly System.Reflection.MethodInfo? InvokeDisplayAmountChangedMethod = typeof(CardModel)
        .GetMethod("InvokeDisplayAmountChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

    [SavedProperty] public int AstralParty_UltimateSkillCharge { get; set; }

    public override bool ShouldReceiveCombatHooks => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUltimateSkill];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar(ChargeVarName, 0)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralUltimateSkillId)
    ];

    protected override bool IsPlayable => Owner != null && GetCurrentCharge() >= UltimateSkillChargeHelper.MaxCharge;

    protected UltimateSkillCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target)
        : base(baseCost, type, rarity, target)
    {
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        RefreshUltimateChargeDisplay();
        return base.AfterCardChangedPiles(card, oldPileType, source);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        RefreshUltimateChargeDisplay();
    }

    public void RefreshUltimateChargeDisplay()
    {
        if (!DynamicVars.ContainsKey(ChargeVarName))
            return;

        DynamicVars[ChargeVarName].BaseValue = GetCurrentCharge();
        InvokeDisplayAmountChangedMethod?.Invoke(this, null);
    }

    protected int GetCurrentCharge()
    {
        return Math.Clamp(AstralParty_UltimateSkillCharge, 0, UltimateSkillChargeHelper.MaxCharge);
    }

    public int SetCurrentCharge(int charge)
    {
        var normalized = Math.Clamp(charge, 0, UltimateSkillChargeHelper.MaxCharge);
        AstralParty_UltimateSkillCharge = normalized;
        RefreshUltimateChargeDisplay();
        return normalized;
    }

    public int AddCharge(int delta)
    {
        return SetCurrentCharge(GetCurrentCharge() + delta);
    }

    protected bool TryConsumeFullCharge()
    {
        if (Owner == null || GetCurrentCharge() < UltimateSkillChargeHelper.MaxCharge)
            return false;

        SetCurrentCharge(0);
        return true;
    }
}
