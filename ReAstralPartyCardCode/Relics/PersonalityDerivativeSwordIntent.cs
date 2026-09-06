using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "personality_derivative_sword_intent_doppelganger")]
public class PersonalityDerivativeSwordIntentDoppelganger : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeSwordIntentCounter { get; set; }

    [SavedProperty] public int AstralParty_PersonalityDerivativeSwordIntentSwordGustProgress { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeSwordIntentCounter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFamousBladeDoppelganger>(),
        HoverTipFactory.FromPower<SwordGustPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeSwordIntentCounter = 0;
        AstralParty_PersonalityDerivativeSwordIntentSwordGustProgress = 0;
        InvokeDisplayAmountChanged();
    }

    public async Task OnFamousBladePlayed(
        PlayerChoiceContext choiceContext,
        Creature? target,
        int consumedSwordGust,
        CardModel sourceCard)
    {
        if (Owner?.Creature == null)
            return;

        if (consumedSwordGust > 0)
        {
            AstralParty_PersonalityDerivativeSwordIntentSwordGustProgress += consumedSwordGust;
            var convertedDamage = AstralParty_PersonalityDerivativeSwordIntentSwordGustProgress / 2;
            AstralParty_PersonalityDerivativeSwordIntentSwordGustProgress %= 2;

            if (convertedDamage > 0)
            {
                AstralParty_PersonalityDerivativeSwordIntentCounter += convertedDamage;
                InvokeDisplayAmountChanged();
            }
        }

        if (target == null || !target.IsAlive || target.Side == Owner.Creature.Side)
            return;

        var pursuitDamage = AstralParty_PersonalityDerivativeSwordIntentCounter;
        if (pursuitDamage <= 0)
            return;

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            target,
            pursuitDamage,
            ValueProp.Unpowered,
            Owner.Creature,
            sourceCard
        );
    }
}
