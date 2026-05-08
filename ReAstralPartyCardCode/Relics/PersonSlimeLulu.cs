using System;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonSlimeLulu : CooldownPersonaRelicBase
{
    private const int SlimeLuluBaseMaxCounter = 4;

    [SavedProperty] public int AstralParty_PersonSlimeLuluCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonSlimeLuluPendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonSlimeLuluHealingSlimeUses { get; set; }

    // Preserve legacy wire/save names so older SlimeLulu runs still hydrate correctly.
    public int CombatsLeft
    {
        get => AstralParty_PersonSlimeLuluCounter;
        set => AstralParty_PersonSlimeLuluCounter = value;
    }

    public int CardsAdded
    {
        get => AstralParty_PersonSlimeLuluHealingSlimeUses;
        set => AstralParty_PersonSlimeLuluHealingSlimeUses = value;
    }

    public bool Skin
    {
        get => AstralParty_PersonSlimeLuluPendingCombatStartCard;
        set => AstralParty_PersonSlimeLuluPendingCombatStartCard = value;
    }

    protected override int CounterValue
    {
        get => AstralParty_PersonSlimeLuluCounter;
        set => AstralParty_PersonSlimeLuluCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonSlimeLuluPendingCombatStartCard;
        set => AstralParty_PersonSlimeLuluPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillHealingSlime>(),
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        HoverTipFactory.FromPower<AdherentMucusPower>()
    ];

    protected override int BaseMaxCounter => SlimeLuluBaseMaxCounter;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner?.Creature == null)
            return;

        AstralParty_PersonSlimeLuluHealingSlimeUses = 0;

        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            10m,
            false
        );
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return;

        if (result.UnblockedDamage <= 0)
            return;

        Flash();

        await PowerCmd.Apply<HalfLifeHealPower>(
            Owner.Creature,
            1m,
            Owner.Creature,
            null,
            false
        );

        if (dealer != null && dealer.Side != Owner.Creature.Side && dealer.IsAlive)
        {
            await AdherentMucusPower.Apply(
                dealer,
                Owner,
                Owner.Creature,
                cardSource
            );
        }

        AdvanceCounter();
        RefreshCooldownDisplay();
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        await base.BeforeSideTurnStart(choiceContext, side, combatState);

        if (Owner?.Creature?.CombatState == null || side == Owner.Creature.Side)
            return;

        await AdherentMucusPower.ResetRoundHitFlagForBoundSlime(Owner.Creature.CombatState, Owner.NetId);
    }

    protected override async Task AfterAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await base.AfterAdvanceCounterOnTurnEnd(choiceContext, side);

        if (Owner?.Creature?.CombatState == null || side == Owner.Creature.Side)
            return;

        await AdherentMucusPower.DecayAllForBoundSlimeIfMissed(Owner.Creature.CombatState, Owner.NetId);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();

        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillHealingSlime>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }
}
