using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class SkillSaveMeMousy : AstralPartyCardModel
{
    private const decimal CardsToDraw = 1m;
    private const decimal CounterAmount = 1m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralPartyMod.AstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MouseShieldPower>(),
        HoverTipFactory.FromPower<CounterPower>()
    ];

    public SkillSaveMeMousy() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AnyAlly)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null)
            return;

        var target = cardPlay.Target;
        var targetPlayer = target.Player;
        if (targetPlayer == null)
            return;

        await GrantTargetSupport(choiceContext, target, targetPlayer, ownerCreature);

        if (!ShouldGrantEmergencyBackup(target))
            return;

        await GrantEmergencyBackup(choiceContext, ownerCreature);
    }

    private async Task GrantTargetSupport(
        PlayerChoiceContext choiceContext,
        Creature target,
        MegaCrit.Sts2.Core.Entities.Players.Player targetPlayer,
        Creature ownerCreature)
    {
        await CardPileCmd.Draw(choiceContext, CardsToDraw, targetPlayer);
        await PowerCmd.Apply<MouseShieldPower>(target, 1m, ownerCreature, this, false);
        await PowerCmd.Apply<CounterPower>(target, CounterAmount, ownerCreature, this, false);
    }

    private bool ShouldGrantEmergencyBackup(Creature target)
    {
        return target.MaxHp > 0m && target.CurrentHp / target.MaxHp <= 0.5m;
    }

    private async Task GrantEmergencyBackup(PlayerChoiceContext choiceContext, Creature ownerCreature)
    {
        await PowerCmd.Apply<MouseShieldPower>(ownerCreature, 1m, ownerCreature, this, false);

        if (Owner != null)
            await CardPileCmd.Draw(choiceContext, CardsToDraw, Owner);
    }
}
