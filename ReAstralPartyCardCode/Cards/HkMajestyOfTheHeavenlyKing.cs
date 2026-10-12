using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class HkMajestyOfTheHeavenlyKing : AstralPartyCardModel
{
    protected override string PortraitBasePath =>
        "res://ReAstralPartyMod/images/card_portraits/hk_majesty_of_the_heavenly_king，rare";

    public HkMajestyOfTheHeavenlyKing() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Target == null || !cardPlay.Target.IsAlive)
            return;

        var damage = Owner.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0;
        var results =
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, damage, ValueProp.Move, Owner.Creature, this);
        var unblockedDamage = results.Sum(static result => result.UnblockedDamage);
        if (unblockedDamage <= 0m || CombatState == null)
            return;

        foreach (var teammate in EventCombatTargetHelper.GetAlivePlayers(CombatState).Where(player => player != Owner))
            await TokenEternalStarlight.GrantStacks(teammate, (int)unblockedDamage);
    }
}
