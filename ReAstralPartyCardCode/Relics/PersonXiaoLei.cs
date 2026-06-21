using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonXiaoLei : LegacyCooldownPersonRelicBase
{
    [SavedProperty]
    public int AstralParty_PersonXiaoLeiCounter
    {
        get => GetCanonicalCounter();
        set => SetCanonicalCounter(value);
    }

    [SavedProperty]
    public bool AstralParty_PersonXiaoLeiPendingCombatStartCard
    {
        get => GetCanonicalPendingCombatStartCard();
        set => SetCanonicalPendingCombatStartCard(value);
    }

    // Preserve legacy wire/save names so older XiaoLei runs still hydrate correctly.
    public int TimesLifted
    {
        get => default;
        set => SetLegacyCounterAliasIfMissing(value);
    }

    public bool GoldenPathAct
    {
        get => default;
        set => SetLegacyPendingAliasIfMissing(value);
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillChainReaction>(),
        HoverTipFactory.FromPower<DragonAwakeningPower>(),
        HoverTipFactory.FromPower<TrueDragonFormPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeXiaoLeiDragonGate>(
            Owner);
    }

    public async Task GrantDragonAwakening(int amount)
    {
        if (amount <= 0)
            return;
        if (Owner?.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply<DragonAwakeningPower>(Owner.Creature, amount, Owner.Creature, null, false);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillChainReaction>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }
}
