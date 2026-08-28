using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "personality_derivative_lake_marsh_king")]
public sealed class PersonalityDerivativeLakeMarshKing : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeLakeMarshKingAwakening { get; set; }
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShowCounter => Owner?.Creature?.CombatState != null;
    public override int DisplayAmount => AstralParty_PersonalityDerivativeLakeMarshKingAwakening;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [ HoverTipFactory.FromCard<SkillChainReaction>(), HoverTipFactory.FromCard<SkillBite>(), HoverTipFactory.FromCard<SkillDragonsRoar>(), HoverTipFactory.FromPower<DragonAwakeningPower>(), HoverTipFactory.FromPower<TrueDragonFormRealPower>() ];

    public override Task BeforeCombatStart()
    {
        AstralParty_PersonalityDerivativeLakeMarshKingAwakening = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    internal async Task HandleObservedCardGain(Player recipient, AbstractModel? source, int amount = 1)
    {
        if (Owner?.Creature?.CombatState == null || recipient == Owner || !CardGainAttribution.IsCausedBy(Owner)) return;
        var current = AstralParty_PersonalityDerivativeLakeMarshKingAwakening;
        if (current < 8)
        {
            AstralParty_PersonalityDerivativeLakeMarshKingAwakening = Math.Min(8, current + Math.Max(1, amount));
            InvokeDisplayAmountChanged();
            if (AstralParty_PersonalityDerivativeLakeMarshKingAwakening >= 8)
                await EnterTrueDragonForm();
        }
    }

    internal bool ShouldGenerateDragonsRoar => Owner?.Creature?.HasPower<TrueDragonFormRealPower>() == true;

    internal CardModel CreateBiteOrRoar()
    {
        CardModel model = ShouldGenerateDragonsRoar ? ModelDb.Card<SkillDragonsRoar>() : ModelDb.Card<SkillBite>();
        return Owner!.Creature!.CombatState!.CreateCard(model, Owner);
    }

    internal async Task ResolveChainReactionDragonDamage(PlayerChoiceContext choiceContext)
    {
        if (!ShouldGenerateDragonsRoar || Owner?.Creature?.CombatState == null)
            return;

        var enemies = CombatTargetOrdering.GetLivingOpponentsStable(Owner.Creature);
        if (enemies.Count == 0)
            return;

        foreach (var player in PersonMultiplayerEffectHelper.GetStableCombatPlayers(Owner))
        {
            if (PileType.Hand.GetPile(player).Cards.Count >= 4)
                continue;

            var target = Owner.RunState.Rng.CombatTargets.NextItem(enemies);
            if (target == null)
                continue;
            await DamageCmd.Attack(6m).Unpowered().Targeting(target).Execute(choiceContext);
        }
    }

    private async Task EnterTrueDragonForm()
    {
        if (Owner?.Creature == null || Owner.Creature.GetPower<TrueDragonFormRealPower>() != null) return;
        await PowerCmd.Apply<TrueDragonFormRealPower>(Owner.Creature, 1m, Owner.Creature, null, false);
        var hand = MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (var card in hand)
        {
            if (card is not SkillBite) continue;
            var replacement = Owner.Creature.CombatState?.CreateCard(ModelDb.Card<SkillDragonsRoar>(), Owner);
            if (replacement != null) await CardCmd.Transform(card, replacement);
        }
    }
}
