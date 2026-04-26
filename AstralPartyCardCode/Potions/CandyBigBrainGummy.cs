using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AstralPartyMod.AstralPartyCardCode.Potions;

[Pool(typeof(CandyPotionPool))]
public class CandyBigBrainGummy : AstralPartyPotionModel
{
    private const decimal DrawAmount = 3m;

    public CandyBigBrainGummy() : base(true)
    {
    }

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ModificationPower>(),
        HoverTipFactory.FromPower<DoomPower>()
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var targetPlayer = target?.Player;
        if (target == null || targetPlayer == null)
            return;

        await CardPileCmd.Draw(choiceContext, DrawAmount, targetPlayer);
        await CandyMachineHelper.RemovePowerIfPresent<ModificationPower>(target);
        await CandyMachineHelper.RemovePowerIfPresent<DoomPower>(target);
    }
}