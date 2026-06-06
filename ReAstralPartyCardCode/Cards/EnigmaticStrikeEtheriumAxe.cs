using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool), StableEntryStem = "enigmatic_strikes_etherium_axe")]
public class EnigmaticStrikeEtheriumAxe : AstralPartyCardModel
{
    private const decimal BaseDamage = 16m;
    private const decimal DrawDiscount = 2m;

    protected override string CardId => "enigmatic_strikes_etherium_axe";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move)
    ];

    public EnigmaticStrikeEtheriumAxe()
        : base(4, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, showInCardLibrary: false)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(3m);
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
        if (!ReferenceEquals(card, this))
            return;
        if (Owner?.Creature == null)
            return;

        var power = Owner.Creature.GetPower<EtheriumAxeDiscountPower>();
        if (power == null)
        {
            await PowerCmd.Apply(
                ModelDb.Power<EtheriumAxeDiscountPower>().ToMutable(),
                Owner.Creature,
                1m,
                Owner.Creature,
                this,
                true);
            power = Owner.Creature.GetPower<EtheriumAxeDiscountPower>();
        }

        power?.IncreaseDiscount(this, DrawDiscount);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target;
        if (target == null || target.Side == Owner.Creature.Side || !target.IsAlive)
            return;

        var damage = DynamicVars["Damage"].BaseValue;
        if (!EnergyCost.CostsX && EnergyCost.GetResolved() <= 0m)
            damage *= 2m;

        await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move, Owner.Creature, this);
    }
}
