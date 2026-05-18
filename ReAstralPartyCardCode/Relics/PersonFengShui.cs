using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonFengShui : PersonaRelicBase
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
        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeFortuneMischance>(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeFortuneMischance>(Owner);
        await PowerCmd.SetAmount<FengShuiNodePower>(Owner.Creature, 1m, Owner.Creature, null);

        var alreadyExists = Owner.PlayerCombatState?.AllCards.Any(card =>
            card.Owner == Owner && (card.CanonicalInstance?.Id ?? card.Id) == ModelDb.GetId<SkillFortuneMischance>()) == true;
        if (alreadyExists)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFortuneMischance>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
