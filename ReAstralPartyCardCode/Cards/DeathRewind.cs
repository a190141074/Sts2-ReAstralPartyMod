using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool), StableEntryStem = "death_rewind")]
public sealed class DeathRewind : PersonSkillCardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeathRewindPower>(),
        HoverTipFactory.FromPower<WithPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>(50m)
    ];

    public DeathRewind() : base(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        await PowerCmd.Apply<DeathRewindPower>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<WithPower>(Owner.Creature, DynamicVars["WithPower"].BaseValue, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
