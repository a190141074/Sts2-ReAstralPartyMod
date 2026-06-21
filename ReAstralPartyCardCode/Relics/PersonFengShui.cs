using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonFengShui : PersonRelicBase
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFortuneMischance>(),
        HoverTipFactory.FromPower<FengShuiNodePower>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeFortuneMischance>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeFortuneMischance>(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeFortuneMischance>(Owner);
        await PowerCmd.SetAmount<FengShuiNodePower>(Owner.Creature, 1m, Owner.Creature, null);

        var alreadyExists = Owner.PlayerCombatState?.AllCards.Any(card =>
            card.Owner == Owner && (card.CanonicalInstance?.Id ?? card.Id) == ModelDb.GetId<SkillFortuneMischance>()) == true;
        if (alreadyExists)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFortuneMischance>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;
        if (Owner?.Creature?.CombatState == null)
            return;
        if (HasFortuneMischanceInPrimaryPiles())
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFortuneMischance>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private bool HasFortuneMischanceInPrimaryPiles()
    {
        if (Owner == null)
            return false;

        var cardId = ModelDb.GetId<SkillFortuneMischance>();
        return PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Any(card => card.Owner == Owner && (card.CanonicalInstance?.Id ?? card.Id) == cardId);
    }
}
