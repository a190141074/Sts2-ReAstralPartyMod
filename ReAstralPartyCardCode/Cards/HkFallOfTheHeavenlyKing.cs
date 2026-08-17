using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class HkFallOfTheHeavenlyKing : AstralPartyCardModel
{
    private const decimal EnemyBlock = 50m;

    protected override bool IsPlayable => Owner != null && !IrrefusableCardHelper.HasIrrefusableCard(Owner);

    public HkFallOfTheHeavenlyKing() : base(100, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null)
            return;

        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature!).Where(enemy => enemy.IsAlive))
        {
            await MegaCrit.Sts2.Core.Commands.CreatureCmd.SetMaxHp(enemy, 1m);
            await CreatureCmd.GainBlock(enemy, EnemyBlock, ValueProp.Move, cardPlay);
        }

        var deckCard = DeckVersion ?? EventDeckCardHelper.GetRunDeckCards(Owner)
            .OfType<HkFallOfTheHeavenlyKing>()
            .FirstOrDefault();
        if (deckCard != null)
            await EventDeckCardHelper.RemoveCardFromRunDeck(Owner, deckCard, false);
    }
}
