using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonShadowScion : CooldownPersonaRelicBase
{
    private const int RoyalPrerogativeCombatCountPerAct = 1;
    private const int EternalStarlightPerCombatBonus = 13;
    private const decimal CombatBonusPerThreshold = 1m;

    [SavedProperty] public int AstralParty_PersonShadowScionCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonShadowScionPendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonShadowScionPendingRoyalPrerogativeCombats { get; set; }

    [SavedProperty] public int AstralParty_PersonShadowScionPendingRoyalPrerogativeActIndex { get; set; } = -1;

    protected override int CounterValue
    {
        get => AstralParty_PersonShadowScionCounter;
        set => AstralParty_PersonShadowScionCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonShadowScionPendingCombatStartCard;
        set => AstralParty_PersonShadowScionPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillPowerfulPity>(),
        HoverTipFactory.FromCard<Royalties>(),
        HoverTipFactory.FromPower<StarLightPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        TokenEternalStarlight.BuildReferenceHoverTip()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        RefreshPendingRoyalPrerogativeForCurrentAct();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        RefreshPendingRoyalPrerogativeForCurrentAct();

        if (AstralParty_PersonShadowScionPendingRoyalPrerogativeCombats > 0)
        {
            Flash();
            foreach (var player in PersonaMultiplayerEffectHelper.GetStableCombatPlayers(Owner))
            {
                var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<Royalties>(), player);
                await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(
                    card,
                    true,
                    CardPilePosition.Top,
                    this);
            }

            AstralParty_PersonShadowScionPendingRoyalPrerogativeCombats--;
        }

        var eternalStarlightStacks = Owner.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0;
        var combatBonus = eternalStarlightStacks / EternalStarlightPerCombatBonus;
        if (combatBonus <= 0)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, combatBonus * CombatBonusPerThreshold, Owner.Creature,
            null);
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, combatBonus * CombatBonusPerThreshold, Owner.Creature,
            null);
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
        if (recipient == Owner)
            return;
        if (!CardGainAttribution.IsCausedBy(Owner))
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

        await PowerCmd.Apply(
            ModelDb.Power<StarLightPower>().ToMutable(),
            recipient.Creature,
            1m,
            Owner.Creature,
            source,
            false
        );
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || power.Owner != Owner.Creature)
            return;
        if (power is not StarLightPower)
            return;
        if (amount <= 0m)
            return;

        if (Owner.Creature.HasPower<MoneyPower>())
            return;

        Flash();
        await PowerCmd.Apply<MoneyPower>(Owner.Creature, 1m, applier, cardSource, false);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillPowerfulPity>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private void RefreshPendingRoyalPrerogativeForCurrentAct()
    {
        if (Owner?.RunState == null)
            return;

        var currentActIndex = Owner.RunState.CurrentActIndex;
        if (AstralParty_PersonShadowScionPendingRoyalPrerogativeActIndex == currentActIndex)
            return;

        AstralParty_PersonShadowScionPendingRoyalPrerogativeActIndex = currentActIndex;
        AstralParty_PersonShadowScionPendingRoyalPrerogativeCombats = RoyalPrerogativeCombatCountPerAct;
    }
}
