using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

[RegisterPotion(typeof(CandyPotionPool))]
public class CandyEnergySupplementBar : AstralPartyPotionModel
{
    private const decimal ModificationAmount = 2m;
    private const decimal DoomAmount = 2m;

    public CandyEnergySupplementBar() : base(true)
    {
    }

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override bool PassesCustomUsabilityCheck =>
        Owner?.Creature?.CombatState?.Players.Any(player =>
            player.Creature?.GetPowerAmount<ModificationPower>() > 0m) == true;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ModificationPower>(),
        HoverTipFactory.FromPower<DoomPower>(),
        HoverTipFactory.FromPower<CandyEnergySupplementBarPower>()
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target == null || target.GetPowerAmount<ModificationPower>() <= 0m)
            return;

        await PowerCmd.Apply<ModificationPower>(target, ModificationAmount, Owner?.Creature, null, false);
        await PowerCmd.Apply<DoomPower>(target, DoomAmount, Owner?.Creature, null, false);
        await CandyEnergySupplementBarPower.Apply(target, 1m, Owner?.Creature, null);
    }
}