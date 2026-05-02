using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonVampire : CooldownPersonaRelicBase
{
    [SavedProperty] public int AstralParty_PersonVampireCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonVampirePendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonVampireCounter;
        set => AstralParty_PersonVampireCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonVampirePendingCombatStartCard;
        set => AstralParty_PersonVampirePendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillVampireBite>(),
        HoverTipFactory.FromPower<BloodthirstPower>(),
        HoverTipFactory.FromPower<CuteIsJusticePower>()
    ];

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

    protected override async Task BeforeCooldownCardCheck(PlayerChoiceContext choiceContext, Player player)
    {
        await VampirePersonaHelper.SyncCuteIsJustice(Owner);
    }

    protected override async Task AfterAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        if (Owner?.Creature?.HasPower<CuteIsJusticePower>() == true)
            await PowerCmd.Remove(Owner.Creature.GetPower<CuteIsJusticePower>()!);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillVampireBite>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }
}
