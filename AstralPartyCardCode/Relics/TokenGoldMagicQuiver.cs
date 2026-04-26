using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenGoldMagicQuiver : AstralPartyRelicModel
{
    [SavedProperty] public bool AstralParty_TokenGoldMagicQuiverTriggeredThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = false;
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = false;

        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        var owner = Owner;
        if (owner == null)
            return;
        if (AstralParty_TokenGoldMagicQuiverTriggeredThisTurn)
            return;
        if (!IsTrackedSkillDamage(target, result.TotalDamage, dealer, cardSource))
            return;
        if (target.GetPowerAmount<MarkLockPower>() <= 0m)
            return;

        AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = true;
        Flash();

        await PowerCmd.Apply<MarkLockPower>(target, 1m, Owner?.Creature, cardSource, false);

        var copiedCard = cardSource!.ToMutable();
        copiedCard.Owner = owner;
        if (!copiedCard.Keywords.Contains(CardKeyword.Exhaust))
            copiedCard.AddKeyword(CardKeyword.Exhaust);
        await CardPileCmd.AddGeneratedCardToCombat(copiedCard, PileType.Hand, true);
    }

    private bool IsTrackedSkillDamage(Creature? target, decimal amount, Creature? dealer, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (dealer != Owner.Creature)
            return false;
        if (target == null || target.Side == Owner.Creature.Side)
            return false;
        if (amount <= 0m)
            return false;
        if (cardSource?.Owner != Owner)
            return false;

        return cardSource.Type == CardType.Skill;
    }
}