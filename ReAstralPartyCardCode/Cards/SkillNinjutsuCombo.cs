using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillNinjutsuCombo : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CopyQuotaPower>(),
        HoverTipFactory.FromPower<EsotericEmpowerPower>()
    ];

    public SkillNinjutsuCombo() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature?.CombatState == null)
            return;

        await PlayerCmd.GainEnergy(1m, Owner);

        var baseAbilityCard = BaseAbilityCardRegistry.GetDeterministicCardModel(
            MainFile.ModId,
            Id.Entry,
            Owner.RunState.Rng.StringSeed,
            Owner.NetId,
            Owner.Creature.CombatState.RoundNumber,
            PileType.Draw.GetPile(Owner).Cards.Count,
            PileType.Hand.GetPile(Owner).Cards.Count,
            PileType.Discard.GetPile(Owner).Cards.Count);
        if (baseAbilityCard == null)
            return;

        var createdCard = Owner.Creature.CombatState.CreateCard(
            baseAbilityCard.CanonicalInstance ?? baseAbilityCard,
            Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(
            createdCard,
            true,
            CardPilePosition.Top,
            this);
    }
}

