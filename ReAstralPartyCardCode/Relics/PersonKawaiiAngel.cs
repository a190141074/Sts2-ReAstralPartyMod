using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
public class PersonKawaiiAngel : CooldownPersonaRelicBase
{
    [SavedProperty] public int AstralParty_PersonKawaiiAngelCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonKawaiiAngelPendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonKawaiiAngelCounter;
        set => AstralParty_PersonKawaiiAngelCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonKawaiiAngelPendingCombatStartCard;
        set => AstralParty_PersonKawaiiAngelPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillCyberAngel>(),
        HoverTipFactory.FromPower<FanPower>()
    ];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return;
        if (dealer == null || dealer.Side == Owner.Creature.Side || dealer.IsDead)
            return;
        if (result.UnblockedDamage <= 0)
            return;

        await ApplyFanToEnemy(dealer, cardSource);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;
        if (target.Side == Owner.Creature.Side || target.IsDead)
            return;
        if (result.UnblockedDamage <= 0)
            return;
        if (!IsTrackedDealer(dealer))
            return;

        await ApplyFanToEnemy(target, cardSource);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillCyberAngel>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private bool IsTrackedDealer(Creature? dealer)
    {
        if (Owner?.Creature == null || dealer == null)
            return false;
        if (dealer == Owner.Creature)
            return true;

        return KawaiiPersonaHelper.GetSameSidePlayersWithRelic<PersonNeedyGirl>(Owner)
            .Any(player => player.Creature == dealer);
    }

    private async Task ApplyFanToEnemy(Creature enemy, CardModel? source)
    {
        if (Owner?.Creature == null || enemy.Side == Owner.Creature.Side)
            return;

        Flash();
        await PowerCmd.Apply<FanPower>(enemy, 1m, Owner.Creature, source, false);
    }
}
