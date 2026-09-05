using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool), StableEntryStem = "skill_token_legendary_sword_gawu_cut")]
public sealed class SkillTokenLegendarySwordGawuCut : AstralPartyCardModel
{
    public SkillTokenLegendarySwordGawuCut() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override string DefaultPortraitPath =>
        "res://ReAstralPartyMod/images/card_portraits/skill_token_legendary_sword_gawu_cut.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Target is not { IsAlive: true } target)
            return;

        var combatRound = Owner.Creature.CombatState?.RoundNumber ?? 0;
        var damage = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            8, 34, MainFile.ModId, Id.Entry, "legendary_sword_gawu_cut", Owner.RunState.Rng.StringSeed,
            Owner.NetId, combatRound, CanonicalInstance?.Id.Entry ?? Id.Entry, Id.Entry);

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Unpowered()
            .Targeting(target)
            .Execute(choiceContext);
    }
}
