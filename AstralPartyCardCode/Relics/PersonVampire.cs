using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonVampire : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;

    [SavedProperty] public int AstralParty_PersonVampireCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonVampirePendingCombatStartCard { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override int DisplayAmount => GetClampedCounter();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillVampireBite>(),
        HoverTipFactory.FromPower<BloodthirstPower>(),
        HoverTipFactory.FromPower<CuteIsJusticePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonVampireCounter = 1;
        AstralParty_PersonVampirePendingCombatStartCard = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        await VampirePersonaHelper.SyncCuteIsJustice(Owner);
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner?.Creature)
            return;

        await VampirePersonaHelper.SyncCuteIsJustice(Owner);
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || power.Owner != Owner.Creature)
            return;
        if (power is not BloodthirstPower)
            return;

        await VampirePersonaHelper.SyncCuteIsJustice(Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        await VampirePersonaHelper.SyncCuteIsJustice(Owner);

        if (AstralParty_PersonVampirePendingCombatStartCard)
        {
            await GrantVampireBite();
            AstralParty_PersonVampirePendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantVampireBite();
        AstralParty_PersonVampireCounter = 1;
        AstralParty_PersonVampirePendingCombatStartCard = false;
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

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        AdvanceCounterAfterCombatEnd();

        if (Owner?.Creature?.HasPower<CuteIsJusticePower>() == true)
            await PowerCmd.Remove(Owner.Creature.GetPower<CuteIsJusticePower>()!);

        InvokeDisplayAmountChanged();
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonVampireCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonVampireCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonVampirePendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonVampireCounter = 1;
            AstralParty_PersonVampirePendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantVampireBite()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillVampireBite>(), Owner);
        await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, true);
    }
}
