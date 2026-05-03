using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class InvokeSpiritsPower : AstralPartyPowerModel
{
    [SavedProperty] public ulong AstralParty_InvokeSpiritsZhaoPlayerNetId { get; set; }
    [SavedProperty] public bool AstralParty_InvokeSpiritsHasReachedNextOwnerTurn { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FoxfirePower>(),
        HoverTipFactory.FromPower<ExtraAttackPower>()
    ];

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (Owner?.Player != player)
            return Task.CompletedTask;

        if (!AstralParty_InvokeSpiritsHasReachedNextOwnerTurn)
        {
            AstralParty_InvokeSpiritsHasReachedNextOwnerTurn = true;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || Owner.Side != side)
            return;
        if (!AstralParty_InvokeSpiritsHasReachedNextOwnerTurn)
            return;

        await PowerCmd.Remove(this);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == null)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;
        if (cardPlay.Card.Type != CardType.Attack)
            return;

        var zhaoPlayer = ResolveZhaoPlayer();
        var zhaoCreature = zhaoPlayer?.Creature;
        var zhaoRelic = zhaoPlayer?.GetRelic<PersonZhao>();
        if (zhaoPlayer == null || zhaoCreature == null || zhaoRelic == null)
            return;

        var foxfirePower = zhaoCreature.GetPower<FoxfirePower>();
        var extraAttackPower = zhaoCreature.GetPower<ExtraAttackPower>();
        if (foxfirePower == null || extraAttackPower == null)
            return;
        if (extraAttackPower.IsTriggeredAttack(cardPlay.Card))
            return;
        if (foxfirePower.Amount <= 0m)
            return;

        var bonusDamage = foxfirePower.Amount;
        await PowerCmd.ModifyAmount(foxfirePower, -1m, zhaoCreature, cardPlay.Card, true);

        var target = cardPlay.Target;
        if (target == null || !target.IsAlive || target.Side == zhaoCreature.Side)
            return;

        zhaoRelic.Flash();
        await ZhaoCombatHelper.AutoPlayRandomAttackForZhao(choiceContext, zhaoPlayer, target, bonusDamage, this);
    }

    private MegaCrit.Sts2.Core.Entities.Players.Player? ResolveZhaoPlayer()
    {
        return Owner?.CombatState?.Players.FirstOrDefault(player => player.NetId == AstralParty_InvokeSpiritsZhaoPlayerNetId);
    }
}
