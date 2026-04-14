using System.Linq;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.cards;

/*
 * 玩家代表
 * 触发者立刻进行一次骰点
 * 4~6你获得6星光，其他玩家获得3星光
 * 1~3你受到4伤害，其他玩家受到2伤害
 */
[Pool(typeof(EventCardPool))]
public class EventPlayerRepresentative : AstralPartyCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("SelfDamage", 4),
        new IntVar("TeamDamage", 2),
        new IntVar("SelfStarLight", 6),
        new IntVar("TeamStarLight", 3)
    ];

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public EventPlayerRepresentative() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        var roll = Owner.RunState.Rng.Niche.NextInt(1, 7);
        var teammates = CombatState.Players.Where(player => player != Owner).ToList();

        if (roll <= 3)
        {
            await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars["SelfDamage"].BaseValue,
                ValueProp.Unpowered, Owner.Creature);
            foreach (var teammate in teammates)
                await CreatureCmd.Damage(choiceContext, teammate.Creature, DynamicVars["TeamDamage"].BaseValue,
                    ValueProp.Unpowered, Owner.Creature);
            return;
        }

        await PowerCmd.Apply(ModelDb.Power<StarLightPower>().ToMutable(), Owner.Creature,
            DynamicVars["SelfStarLight"].BaseValue, Owner.Creature, this, false);
        foreach (var teammate in teammates)
            await PowerCmd.Apply(ModelDb.Power<StarLightPower>().ToMutable(), teammate.Creature,
                DynamicVars["TeamStarLight"].BaseValue, Owner.Creature, this, false);
    }
}