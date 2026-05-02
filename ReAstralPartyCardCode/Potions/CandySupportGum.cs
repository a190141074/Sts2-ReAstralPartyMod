using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

[RegisterPotion(typeof(CandyPotionPool))]
public class CandySupportGum : AstralPartyPotionModel
{
    private const decimal HealAmount = 6m;
    private const decimal EnergyAmount = 1m;

    public CandySupportGum() : base(true)
    {
    }

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DoomPower>()
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var targetPlayer = target?.Player;
        if (target == null || targetPlayer == null)
            return;

        await CreatureCmd.Heal(target, HealAmount, true);
        await PlayerCmd.GainEnergy(EnergyAmount, targetPlayer);
        await CandyMachineHelper.RemovePowerIfPresent<DoomPower>(target);
    }
}