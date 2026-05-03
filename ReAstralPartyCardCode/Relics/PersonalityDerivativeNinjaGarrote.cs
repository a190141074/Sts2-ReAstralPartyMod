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
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeNinjaGarrote : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeNinjaGarroteStacks { get; set; } = 1;
    [SavedProperty] public int AstralParty_PersonalityDerivativeNinjaGarroteSkillCardsThisCombat { get; set; }
    [SavedProperty] public int AstralParty_PersonalityDerivativeNinjaGarroteQualifiedSkillCardsThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeNinjaGarroteStacks;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<EsotericEmpowerPower>(),
        HoverTipFactory.FromPower<CopyQuotaPower>(),
        HoverTipFactory.FromCard<SkillNinjutsuCombo>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (AstralParty_PersonalityDerivativeNinjaGarroteStacks <= 0)
            AstralParty_PersonalityDerivativeNinjaGarroteStacks = 1;
        AstralParty_PersonalityDerivativeNinjaGarroteSkillCardsThisCombat = 0;
        AstralParty_PersonalityDerivativeNinjaGarroteQualifiedSkillCardsThisCombat = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        AstralParty_PersonalityDerivativeNinjaGarroteSkillCardsThisCombat = 0;
        AstralParty_PersonalityDerivativeNinjaGarroteQualifiedSkillCardsThisCombat = 0;
        if (AstralParty_PersonalityDerivativeNinjaGarroteStacks <= 0)
            AstralParty_PersonalityDerivativeNinjaGarroteStacks = 1;

        Flash();
        await PowerCmd.Apply<EsotericEmpowerPower>(
            Owner.Creature,
            AstralParty_PersonalityDerivativeNinjaGarroteStacks,
            Owner.Creature,
            null,
            false);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner != Owner)
            return Task.CompletedTask;
        if (cardPlay.Card.Type != CardType.Skill)
            return Task.CompletedTask;

        AstralParty_PersonalityDerivativeNinjaGarroteSkillCardsThisCombat++;

        if (GetPlayedCost(cardPlay) >= 1)
        {
            AstralParty_PersonalityDerivativeNinjaGarroteQualifiedSkillCardsThisCombat++;

            if (AstralParty_PersonalityDerivativeNinjaGarroteQualifiedSkillCardsThisCombat % 6 == 0
                && Owner.GetRelic<PersonNinja>() is { } personNinja)
            {
                Flash();
                personNinja.ReduceCooldown(1);
            }
        }

        if (GetPlayedCost(cardPlay) >= 1
            && AstralParty_PersonalityDerivativeNinjaGarroteQualifiedSkillCardsThisCombat % 9 == 0)
        {
            Flash();
            AstralParty_PersonalityDerivativeNinjaGarroteStacks++;
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonalityDerivativeNinjaGarroteSkillCardsThisCombat = 0;
        AstralParty_PersonalityDerivativeNinjaGarroteQualifiedSkillCardsThisCombat = 0;
        return Task.CompletedTask;
    }

    private static int GetPlayedCost(CardPlay cardPlay)
    {
        if (cardPlay.Card.EnergyCost.CostsX)
            return Math.Max(1, cardPlay.Resources.EnergyValue);

        return cardPlay.Card.EnergyCost.GetResolved();
    }
}
