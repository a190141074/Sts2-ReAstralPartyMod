using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusiveBoutiqueSwordShield : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromPower<ProblemStudentPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralMagicAcademySeriesId)
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, 2m, Owner.Creature, null, true);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply<VigorPower>(Owner.Creature, 2m, Owner.Creature, null, false);

        if (Owner.Creature.CombatState?.Encounter?.RoomType != RoomType.Boss)
            return;
        if (Owner.Creature.HasPower<ProblemStudentPower>())
            return;

        await PowerCmd.Apply<ProblemStudentPower>(Owner.Creature, 1m, Owner.Creature, null, false);
    }
}