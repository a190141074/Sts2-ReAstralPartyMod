using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeZ3000WeaponFrame : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeZ3000WeaponFrameStacks { get; set; }

    protected override string IconBasePath => "res://ReAstralPartyMod/images/relic/personality_derivative_z3000_weapon_frame";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeZ3000WeaponFrameStacks;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillComeHereYou>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public PersonalityDerivativeZ3000WeaponFrame()
    {
        MainFile.Logger.Info(
            $"Z3000 weapon frame relic assets | packed={PackedIconPath} | big={PublicBigIconPath}");
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;
        if (AstralParty_PersonalityDerivativeZ3000WeaponFrameStacks <= 0)
            return;

        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature,
            AstralParty_PersonalityDerivativeZ3000WeaponFrameStacks,
            Owner.Creature,
            null,
            true);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;
        if (AstralParty_PersonalityDerivativeZ3000WeaponFrameStacks <= 6)
            return;
        if (dealer != Owner.Creature)
            return;
        if (target.Side == Owner.Creature.Side)
            return;
        if (result.UnblockedDamage <= 0m)
            return;
        if (cardSource?.Type != CardType.Attack)
            return;

        Flash();
        await PowerCmd.Apply<DexterityPower>(target, -1m, Owner.Creature, cardSource, false);
    }

    public void AddStacks(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_PersonalityDerivativeZ3000WeaponFrameStacks += amount;
        Flash();
        InvokeDisplayAmountChanged();
    }
}
