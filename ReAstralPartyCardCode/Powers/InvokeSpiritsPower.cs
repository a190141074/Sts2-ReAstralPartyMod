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
    [SavedProperty] public string AstralParty_InvokeSpiritsZhaoPlayerNetIdRaw { get; set; } = string.Empty;

    public ulong AstralParty_InvokeSpiritsZhaoPlayerNetId
    {
        get => ulong.TryParse(AstralParty_InvokeSpiritsZhaoPlayerNetIdRaw, out var value) ? value : 0UL;
        set => AstralParty_InvokeSpiritsZhaoPlayerNetIdRaw = value.ToString();
    }

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

        return PowerCmd.Remove(this);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == null)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;
        if (cardPlay.Card.Type != CardType.Attack)
            return;

        var target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;
        await TryTriggerChase(choiceContext, Owner, target, cardPlay.Card, this);
    }

    private MegaCrit.Sts2.Core.Entities.Players.Player? ResolveZhaoPlayer()
    {
        return Owner?.CombatState?.Players.FirstOrDefault(player => player.NetId == AstralParty_InvokeSpiritsZhaoPlayerNetId);
    }

    public static async Task<bool> TryTriggerChase(
        PlayerChoiceContext choiceContext,
        Creature? invokeOwner,
        Creature target,
        CardModel? triggeringCard,
        AbstractModel source)
    {
        if (invokeOwner?.Player == null || !target.IsAlive)
            return false;

        var invokePower = invokeOwner.GetPower<InvokeSpiritsPower>();
        if (invokePower == null)
            return false;

        var zhaoPlayer = invokePower.ResolveZhaoPlayer();
        var zhaoCreature = zhaoPlayer?.Creature;
        var zhaoRelic = zhaoPlayer?.GetRelic<PersonZhao>();
        if (zhaoPlayer == null || zhaoCreature == null || zhaoRelic == null)
            return false;
        if (target.Side == zhaoCreature.Side)
            return false;

        var foxfirePower = zhaoCreature.GetPower<FoxfirePower>();
        var extraAttackPower = zhaoCreature.GetPower<ExtraAttackPower>();
        if (foxfirePower == null || extraAttackPower == null)
            return false;
        if (triggeringCard != null && extraAttackPower.IsTriggeredAttack(triggeringCard))
            return false;
        if (foxfirePower.Amount <= 0m)
            return false;

        var bonusDamage = foxfirePower.Amount;
        await PowerCmd.ModifyAmount(foxfirePower, -1m, zhaoCreature, triggeringCard, true);

        zhaoRelic.Flash();
        await ZhaoCombatHelper.AutoPlayRandomAttackForZhao(choiceContext, zhaoPlayer, target, bonusDamage, source);
        return true;
    }
}
