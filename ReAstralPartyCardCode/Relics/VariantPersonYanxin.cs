using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonYanxin : CooldownPersonRelicBase
{
    [SavedProperty] public int AstralParty_VariantPersonYanxinCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonYanxinPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonYanxinLastProcessedRound { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonYanxinCounter;
        set => AstralParty_VariantPersonYanxinCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonYanxinPendingCombatStartCard;
        set => AstralParty_VariantPersonYanxinPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 5;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillDivinePunishment>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeBookOfHeaven>(),
        HoverTipFactory.FromPower<DivineSonPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_VariantPersonYanxinLastProcessedRound = 0;
        await AstralDivinePersonaHelper.EnsureBookOfHeaven(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        AstralParty_VariantPersonYanxinLastProcessedRound = 0;
        await AstralDivinePersonaHelper.EnsureBookOfHeaven(Owner);
        await PowerCmd.Apply<DivineSonPower>(Owner.Creature, 1m, Owner.Creature, null, false);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_VariantPersonYanxinLastProcessedRound == roundNumber)
            return;

        AstralParty_VariantPersonYanxinLastProcessedRound = roundNumber;
        var interval = Math.Max(1, 10 - (int)Owner.Creature.GetPowerAmount<DivineSonPower>());
        if (roundNumber % interval != 0)
            return;

        Flash();
        foreach (var teammate in AstralDivinePersonaHelper.GetStablePlayers(Owner))
        {
            await PlayerCmd.GainEnergy(1m, teammate);
        }

        await AstralDivinePersonaHelper.GrantDivineSonToAllPlayers(Owner, this);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_VariantPersonYanxinLastProcessedRound = 0;
        return Task.CompletedTask;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillDivinePunishment>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
