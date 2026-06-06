using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool), StableEntryStem = "enigmatic_strikes_etherium_sword")]
public class EnigmaticStrikeEtheriumSword : AstralPartyCardModel
{
    private const decimal BaseDamage = 12m;
    private const decimal BaseSplashDamageRatio = 0.5m;
    private const decimal UpgradedSplashDamageRatio = 0.75m;

    protected override string CardId => "enigmatic_strikes_etherium_sword";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<EtheriumSwordRecallOmenPower>()
    ];

    public EnigmaticStrikeEtheriumSword()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, showInCardLibrary: false)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(3m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        var target = cardPlay.Target;
        if (target == null || target.Side == Owner.Creature.Side || !target.IsAlive)
            return;

        var damage = DynamicVars["Damage"].BaseValue;
        await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move, Owner.Creature, this);

        if (Owner.Creature.CombatState != null)
        {
            var splashDamageRatio = CurrentUpgradeLevel > 0 ? UpgradedSplashDamageRatio : BaseSplashDamageRatio;
            var splashDamage = damage * splashDamageRatio;
            var otherEnemies = Owner.Creature.CombatState
                .GetOpponentsOf(Owner.Creature)
                .Where(enemy => enemy.IsAlive && enemy != target)
                .ToList();
            foreach (var enemy in otherEnemies)
                await CreatureCmd.Damage(choiceContext, enemy, splashDamage, ValueProp.Move, Owner.Creature, this);
        }

        await PowerCmd.Apply(
            ModelDb.Power<EtheriumSwordRecallOmenPower>().ToMutable(),
            Owner.Creature,
            2m,
            Owner.Creature,
            this,
            false);
    }
}
