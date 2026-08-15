using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonSlimeLuluDoppelganger : CooldownPersonRelicBase
{
    private const int SlimeLuluDoppelgangerBaseMaxCounter = 5;
    private const decimal TriggeredHealAmount = 1m;
    private const decimal TriggeredMucusAmount = 3m;

    [SavedProperty] public int AstralParty_PersonSlimeLuluDoppelgangerCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonSlimeLuluDoppelgangerPendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonSlimeLuluDoppelgangerCounter;
        set => AstralParty_PersonSlimeLuluDoppelgangerCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonSlimeLuluDoppelgangerPendingCombatStartCard;
        set => AstralParty_PersonSlimeLuluDoppelgangerPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillHealingSlimeDoppelganger>(),
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        HoverTipFactory.FromPower<AdherentMucusPower>()
    ];

    protected override int BaseMaxCounter => SlimeLuluDoppelgangerBaseMaxCounter;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        var owner = Owner;
        if (owner?.Creature is not { } ownerCreature)
            return;
        if (!IsOwnedByTarget(target, ownerCreature))
            return;
        if (result.UnblockedDamage <= 0)
            return;

        Flash();

        await PowerCmd.Apply<HalfLifeHealPower>(
            ownerCreature,
            TriggeredHealAmount,
            ownerCreature,
            null,
            false
        );

        AdvanceCounter();
        RefreshCooldownDisplay();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (cardPlay.Card is not SkillHealingSlimeDoppelganger)
            return;

        var target = PickRandomLivingEnemy();
        if (target == null)
            return;

        Flash();
        await PowerCmd.Apply<AdherentMucusPower>(
            target,
            TriggeredMucusAmount,
            Owner.Creature,
            cardPlay.Card,
            false
        );
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();

        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillHealingSlimeDoppelganger>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }

    private Creature? PickRandomLivingEnemy()
    {
        if (Owner?.Creature == null)
            return null;

        var livingEnemies = CombatTargetOrdering.GetLivingOpponentsStable(Owner.Creature);
        if (livingEnemies.Count == 0)
            return null;

        return Owner.RunState.Rng.CombatTargets.NextItem(livingEnemies);
    }

    private static bool IsOwnedByTarget(Creature target, Creature ownerCreature)
    {
        if (target == ownerCreature)
            return true;

        return target.PetOwner == ownerCreature.Player;
    }
}
