using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
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
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "person_sherry_tachibana")]
public sealed class PersonSherryTachibana : PersonRelicBase
{
    [SavedProperty] public int AstralParty_PersonSherryTachibanaTurnCount { get; set; }
    [SavedProperty] public bool AstralParty_PersonSherryTachibanaFirstAttackTriggered { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillMightyThrow>(),
        HoverTipFactory.FromPower<DetectiveOnTheCasePower>()
    ];

    public override async Task BeforeCombatStart()
    {
        AstralParty_PersonSherryTachibanaTurnCount = 0;
        AstralParty_PersonSherryTachibanaFirstAttackTriggered = false;
        await EnsureDetectivePower();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature == null || player != Owner)
            return;

        AstralParty_PersonSherryTachibanaTurnCount++;
        AstralParty_PersonSherryTachibanaFirstAttackTriggered = false;
        await EnsureDetectivePower();

        if ((AstralParty_PersonSherryTachibanaTurnCount & 1) == 1)
            await AddMightyThrowToHand();
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (Owner?.Creature is not { } ownerCreature || AstralParty_PersonSherryTachibanaFirstAttackTriggered)
            return;
        if (dealer != ownerCreature || target.Side == ownerCreature.Side || result.TotalDamage <= 0m)
            return;
        if (cardSource == null || cardSource.Owner != Owner || !WarforgeEnchantmentHelper.CountsAsAttack(cardSource))
            return;

        AstralParty_PersonSherryTachibanaFirstAttackTriggered = true;
        var current = ownerCreature.GetPowerAmount<DetectiveOnTheCasePower>();
        if (current >= DetectiveOnTheCasePower.MaxStacks)
            return;

        Flash();
        await PowerCmd.Apply<DetectiveOnTheCasePower>(ownerCreature,
            Math.Min(1m, DetectiveOnTheCasePower.MaxStacks - current), ownerCreature, cardSource, false);
    }

    private async Task EnsureDetectivePower()
    {
        if (Owner?.Creature == null || Owner.Creature.HasPower<DetectiveOnTheCasePower>())
            return;

        await PowerCmd.Apply<DetectiveOnTheCasePower>(Owner.Creature, 0m, Owner.Creature, null, true);
    }

    private async Task AddMightyThrowToHand()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillMightyThrow>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
