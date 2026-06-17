using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class JewelryWorldTears : AstralPartyRelicModel
{
    private const int SkillTriggerThreshold = 5;

    [SavedProperty] public int AstralParty_JewelryWorldTearsSkillCountThisCombat { get; set; }
    [SavedProperty] public bool AstralParty_JewelryWorldTearsGrantedExtraSunsetGlowThisRound { get; set; }
    [SavedProperty] public int AstralParty_JewelryWorldTearsLastProcessedRound { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillSunsetGlow>()
    ];

    public override Task AfterObtained()
    {
        ResetCombatState();
        return base.AfterObtained();
    }

    public override Task BeforeCombatStart()
    {
        ResetCombatState();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        ResetCombatState();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null || player != Owner)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_JewelryWorldTearsLastProcessedRound == roundNumber)
            return;

        AstralParty_JewelryWorldTearsLastProcessedRound = roundNumber;
        AstralParty_JewelryWorldTearsGrantedExtraSunsetGlowThisRound = false;

        if (!HasElena() || !IsEvenFloorCombat() || !IsEvenRoundNumber(roundNumber))
            return;

        AstralParty_JewelryWorldTearsGrantedExtraSunsetGlowThisRound = true;
        await Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (HasElena())
            return;
        if (cardPlay.Card.Type != CardType.Skill)
            return;
        if (!IsCardCostEligible(cardPlay.Card))
            return;

        AstralParty_JewelryWorldTearsSkillCountThisCombat++;
        if (AstralParty_JewelryWorldTearsSkillCountThisCombat < SkillTriggerThreshold)
            return;

        AstralParty_JewelryWorldTearsSkillCountThisCombat -= SkillTriggerThreshold;
        Flash();
        await PlayerCmd.GainEnergy(1m, Owner);
    }

    public bool ShouldGrantExtraSunsetGlowPlayThisTurn()
    {
        if (!HasElena() || !IsEvenFloorCombat())
            return false;

        return AstralParty_JewelryWorldTearsGrantedExtraSunsetGlowThisRound;
    }

    private bool HasElena()
    {
        return Owner?.GetRelic<VariantPersonElena>() != null;
    }

    private bool IsEvenFloorCombat()
    {
        var totalFloor = Owner?.RunState?.TotalFloor ?? 0;
        return totalFloor > 0 && totalFloor % 2 == 0;
    }

    private static bool IsEvenRoundNumber(int roundNumber)
    {
        return roundNumber > 0 && roundNumber % 2 == 0;
    }

    private static bool IsCardCostEligible(CardModel card)
    {
        return card.EnergyCost.CostsX || card.EnergyCost.GetResolved() >= 1m;
    }

    private void ResetCombatState()
    {
        AstralParty_JewelryWorldTearsSkillCountThisCombat = 0;
        AstralParty_JewelryWorldTearsGrantedExtraSunsetGlowThisRound = false;
        AstralParty_JewelryWorldTearsLastProcessedRound = 0;
    }
}
