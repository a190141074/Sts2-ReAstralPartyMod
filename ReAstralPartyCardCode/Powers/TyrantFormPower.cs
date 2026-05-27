using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class TyrantFormPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Player == null || player != Owner.Player)
            return;

        await ReturnSovereignBladesToHand();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Player == null || side != Owner.Side)
            return;

        var sovereignBlades = GetOwnedSovereignBlades();
        if (sovereignBlades == null || sovereignBlades.Count == 0)
            return;

        foreach (var sovereignBlade in sovereignBlades)
        {
            var target = GetRandomLivingEnemy();
            if (target == null)
                break;

            if (sovereignBlade.Pile?.Type != PileType.Hand)
                continue;

            if (!TyrantFormAutoPlayHelper.TryEnterAutoPlay(sovereignBlade))
                continue;

            try
            {
                await CardCmd.AutoPlay(
                    choiceContext,
                    sovereignBlade,
                    target,
                    AutoPlayType.Default,
                    true,
                    false);
            }
            finally
            {
                TyrantFormAutoPlayHelper.ExitAutoPlay(sovereignBlade);
            }
        }
    }

    private async Task ReturnSovereignBladesToHand()
    {
        var sovereignBlades = GetOwnedSovereignBlades();
        if (sovereignBlades == null || sovereignBlades.Count == 0)
            return;

        foreach (var sovereignBlade in sovereignBlades)
        {
            if (sovereignBlade.Pile?.Type == PileType.Hand)
                continue;

            await CardPileCmd.Add(sovereignBlade, PileType.Hand, CardPilePosition.Top, this);
        }
    }

    private List<SovereignBlade>? GetOwnedSovereignBlades()
    {
        return Owner?.Player?.PlayerCombatState?.AllCards
            .OfType<SovereignBlade>()
            .Where(card => card.Owner == Owner.Player)
            .ToList();
    }

    private Creature? GetRandomLivingEnemy()
    {
        var ownerCreature = Owner;
        var ownerPlayer = ownerCreature?.Player;
        if (ownerCreature == null)
            return null;

        var enemies = ownerCreature.CombatState?
            .GetOpponentsOf(ownerCreature)
            .Where(creature => creature.IsAlive)
            .ToList();
        if (enemies == null || enemies.Count == 0)
            return null;

        var rng = ownerPlayer?.RunState?.Rng?.CombatTargets;
        return rng != null
            ? enemies[rng.NextInt(enemies.Count)]
            : enemies.FirstOrDefault();
    }
}
