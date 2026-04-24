using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
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
public class PersonShadowScion : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;
    private const decimal FirstCombatStarLightAmount = 50m;
    private const int EternalStarlightPerCombatBonus = 12;
    private const decimal CombatBonusPerThreshold = 1m;

    [SavedProperty] public int AstralParty_PersonShadowScionCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonShadowScionPendingCombatStartCard { get; set; }

    [SavedProperty] public bool AstralParty_PersonShadowScionFirstCombatBonusPending { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override bool ShouldReceiveCombatHooks => true;

    public override int DisplayAmount => GetClampedCounter();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillPowerfulPity>(),
        HoverTipFactory.FromPower<StarLightPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        TokenEternalStarlight.BuildReferenceHoverTip()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonShadowScionCounter = 1;
        AstralParty_PersonShadowScionPendingCombatStartCard = true;
        AstralParty_PersonShadowScionFirstCombatBonusPending = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonShadowScionFirstCombatBonusPending)
        {
            Flash();
            foreach (var player in Owner.Creature.CombatState.Players)
            {
                await PowerCmd.Apply(
                    ModelDb.Power<StarLightPower>().ToMutable(),
                    player.Creature,
                    FirstCombatStarLightAmount,
                    Owner.Creature,
                    null,
                    false
                );
            }

            AstralParty_PersonShadowScionFirstCombatBonusPending = false;
        }

        var eternalStarlightStacks = Owner.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0;
        var combatBonus = eternalStarlightStacks / EternalStarlightPerCombatBonus;
        if (combatBonus <= 0)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, combatBonus * CombatBonusPerThreshold, Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, combatBonus * CombatBonusPerThreshold, Owner.Creature, null);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonShadowScionPendingCombatStartCard)
        {
            await GrantPowerfulPity();
            AstralParty_PersonShadowScionPendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantPowerfulPity();
        AstralParty_PersonShadowScionCounter = 1;
        AstralParty_PersonShadowScionPendingCombatStartCard = false;
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

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (Owner?.Creature?.CombatState == null || card.Owner == null)
            return;

        await HandleObservedCardGain(card.Owner, card);
    }

    internal async Task HandleObservedCardGain(Player recipient, CardModel? source)
    {
        if (Owner?.Creature == null || recipient.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply(
            ModelDb.Power<StarLightPower>().ToMutable(),
            Owner.Creature,
            1m,
            Owner.Creature,
            source,
            false
        );

        if (recipient != Owner)
        {
            await PowerCmd.Apply(
                ModelDb.Power<StarLightPower>().ToMutable(),
                recipient.Creature,
                1m,
                Owner.Creature,
                source,
                false
            );
        }
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonShadowScionCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonShadowScionCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonShadowScionPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonShadowScionCounter = 1;
            AstralParty_PersonShadowScionPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantPowerfulPity()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillPowerfulPity>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }
}
