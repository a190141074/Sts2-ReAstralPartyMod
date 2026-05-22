using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonNoa : CooldownPersonaRelicBase
{
    [SavedProperty] public int AstralParty_VariantPersonNoaCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonNoaPendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonNoaCounter;
        set => AstralParty_VariantPersonNoaCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonNoaPendingCombatStartCard;
        set => AstralParty_VariantPersonNoaPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 5;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillClearObstacle>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeGlitchRobot>(),
        HoverTipFactory.FromPower<DivineSonPower>(),
        HoverTipFactory.FromPower<OverloadModePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await AstralNoaHelper.EnsureGlitchRobot(Owner);
        await AstralNoaHelper.SyncGlitchRobot(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        await AstralNoaHelper.EnsureGlitchRobot(Owner);
        await AstralNoaHelper.InitializeOpeningState(Owner);
        await AstralNoaHelper.GrantOpeningBlockToAllPlayers(Owner, this);
        await AstralNoaHelper.SyncGlitchRobot(Owner);
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (Owner?.Creature == null)
            return false;
        if (target != Owner.Creature)
            return false;
        if (!AstralNoaHelper.IsPoisonOrDoom(canonicalPower))
            return false;
        if (amount <= 0m)
            return false;

        var bonus = AstralNoaHelper.GetNoaBonusAmount(amount);
        if (bonus <= 0)
            return false;

        modifiedAmount = amount + bonus;
        Flash();
        return true;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillClearObstacle>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
