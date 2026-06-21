using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Tags;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(ColorlessCardPool), StableEntryStem = "collectors_card_realm_of_death")]
public sealed class CollectorsCardRealmOfDeath : AstralPartyCardModel
{
    private const decimal BlockPercent = 0.1m;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override HashSet<CardTag> CanonicalTags => [AstralCardTags.Collectors];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<RealmOfDeathPower>()
    ];

    public CollectorsCardRealmOfDeath() : base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target;
        if (target == null || target.Side == Owner.Creature.Side || !target.IsAlive)
            return;

        var blockAmount = Math.Max(1m, Math.Ceiling(target.MaxHp * BlockPercent));
        await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, null);

        var ownerPower = await RealmOfDeathPower.ApplyToTarget(Owner.Creature, Owner.Creature, this);
        await RealmOfDeathPower.ApplyToTarget(target, Owner.Creature, this);

        if (ownerPower != null)
            ownerPower.AstralParty_RealmOfDeathMarkedTargetCombatId = target.CombatId ?? 0u;
    }
}
