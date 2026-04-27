using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonJillSteinle : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;

    [SavedProperty] public int AstralParty_PersonJillSteinleCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonJillSteinlePendingCombatStartCard { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillMixedCocktails>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralMixed),
        HoverTipFactory.FromPower<MixedCocktailsPower>(),
        HoverTipFactory.FromPower<StarLightPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonJillSteinleCounter = 1;
        AstralParty_PersonJillSteinlePendingCombatStartCard = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonJillSteinlePendingCombatStartCard)
        {
            await GrantMixedCocktails();
            AstralParty_PersonJillSteinlePendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantMixedCocktails();
        AstralParty_PersonJillSteinleCounter = 1;
        AstralParty_PersonJillSteinlePendingCombatStartCard = false;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        AdvanceCounter();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AdvanceCounterAfterCombatEnd();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonJillSteinleCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonJillSteinleCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonJillSteinlePendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonJillSteinleCounter = 1;
            AstralParty_PersonJillSteinlePendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantMixedCocktails()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillMixedCocktails>(), Owner);
        await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, true);
    }
}
