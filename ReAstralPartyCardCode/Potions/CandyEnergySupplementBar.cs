using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
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

    public override TargetType TargetType => CandyPotionTargetingHelper.AnyModifiedPlayer;

    public override bool PassesCustomUsabilityCheck => CandyPotionTargetingHelper.AnyModifiedPlayersInCombat(Owner);

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ModificationPower>(),
        HoverTipFactory.FromPower<DoomPower>(),
        HoverTipFactory.FromPower<CandyEnergySupplementBarPower>()
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (!CandyPotionTargetingHelper.IsModifiedPlayer(target))
            return;

        var actualTarget = target!;
        await PowerCmd.Apply<ModificationPower>(actualTarget, ModificationAmount, Owner?.Creature, null, false);
        await PowerCmd.Apply<DoomPower>(actualTarget, DoomAmount, Owner?.Creature, null, false);
        await CandyEnergySupplementBarPower.Apply(actualTarget, 1m, Owner?.Creature, null);
    }
}
