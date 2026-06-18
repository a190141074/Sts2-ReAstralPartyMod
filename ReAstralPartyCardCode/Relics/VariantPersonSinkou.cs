using System.Collections.Generic;
using System.Threading.Tasks;
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
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonSinkou : CooldownPersonaRelicBase
{
    [SavedProperty] public int AstralParty_VariantPersonSinkouCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonSinkouPendingCombatStartCard { get; set; }
    protected override string RelicId => "variant_person_sinkou";

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonSinkouCounter;
        set => AstralParty_VariantPersonSinkouCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonSinkouPendingCombatStartCard;
        set => AstralParty_VariantPersonSinkouPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 5;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillPunitiveJudgment>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeDawnAndDuskAfterglow>(),
        HoverTipFactory.FromPower<BlazingSolarBurnPower>(),
        HoverTipFactory.FromPower<RageOfFirePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await AstralSinkouHelper.EnsureAfterglow(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        await AstralSinkouHelper.EnsureAfterglow(Owner);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (cardSource?.Owner != Owner || !WarforgeEnchantmentHelper.CountsAsAttack(cardSource))
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;

        return AstralSinkouHelper.GetAttackBonusAmount(Owner, amount);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillPunitiveJudgment>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
