using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
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

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonalityDerivativeSwordIntent : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeSwordIntentCounter { get; set; }

    [SavedProperty] public int AstralParty_PersonalityDerivativeSwordIntentAuraProgress { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeSwordIntentCounter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFamousBlade>(),
        HoverTipFactory.FromPower<SwordAuraPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeSwordIntentCounter = 0;
        AstralParty_PersonalityDerivativeSwordIntentAuraProgress = 0;
        InvokeDisplayAmountChanged();
    }

    public async Task OnFamousBladePlayed(
        PlayerChoiceContext choiceContext,
        Creature? target,
        int consumedAura,
        CardModel sourceCard)
    {
        if (Owner?.Creature == null || target == null || !target.IsAlive || target.Side == Owner.Creature.Side)
            return;

        if (consumedAura > 0)
        {
            AstralParty_PersonalityDerivativeSwordIntentAuraProgress += consumedAura;
            var convertedDamage = AstralParty_PersonalityDerivativeSwordIntentAuraProgress / 2;
            AstralParty_PersonalityDerivativeSwordIntentAuraProgress %= 2;

            if (convertedDamage > 0)
            {
                AstralParty_PersonalityDerivativeSwordIntentCounter += convertedDamage;
                InvokeDisplayAmountChanged();
            }
        }

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