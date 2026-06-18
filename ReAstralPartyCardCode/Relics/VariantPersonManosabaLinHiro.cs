using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class VariantPersonManosabaLinHiro : PersonaRelicBase
{
    private const int TransferAmount = 40;
    private const int TeamDamageWithGain = 10;

    [SavedProperty] public int AstralParty_VariantPersonManosabaLinHiroLastRewardBand { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFateFirewoodStick>(),
        HoverTipFactory.FromPower<WithPower>(),
        HoverTipFactory.FromPower<FateFirewoodNodePower>()
    ];

    public override Task BeforeCombatStart()
    {
        AstralParty_VariantPersonManosabaLinHiroLastRewardBand = StableNumericStateHelper.FloorDivisionToNonNegativeInt(
            Owner?.Creature?.GetPowerAmount<WithPower>() ?? 0m,
            100m);
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        await GrantFirewoodOrReplayExisting(this);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null)
            return;
        if (target.Player == null || target.Player == Owner)
            return;
        if (target.Side != ownerCreature.Side)
            return;
        if (result.TotalDamage <= 0m)
            return;

        await PowerCmd.Apply<WithPower>(ownerCreature, TeamDamageWithGain, ownerCreature, null, false);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;
        if (power is not WithPower || power.Owner != Owner.Creature)
            return;

        var currentBand = StableNumericStateHelper.FloorDivisionToNonNegativeInt(power.Amount, 100m);
        if (amount > 0m && currentBand > AstralParty_VariantPersonManosabaLinHiroLastRewardBand)
        {
            var rewardCount = currentBand - AstralParty_VariantPersonManosabaLinHiroLastRewardBand;
            for (var i = 0; i < rewardCount; i++)
                await GrantFirewoodOrReplayExisting(this);
        }

        AstralParty_VariantPersonManosabaLinHiroLastRewardBand = currentBand;
    }

    internal int GetCurrentWithPower()
    {
        return StableNumericStateHelper.FloorToNonNegativeInt(Owner?.Creature?.GetPowerAmount<WithPower>() ?? 0m);
    }

    internal async Task TryTransferWithPowerAndRegainCard(Creature target, AbstractModel source)
    {
        var ownerCreature = Owner?.Creature;
        if (Owner == null || ownerCreature == null || target.Player == null)
            return;

        var withPower = ownerCreature.GetPower<WithPower>();
        if (withPower == null || withPower.Amount < TransferAmount)
            return;

        await PowerCmd.ModifyAmount(withPower, -TransferAmount, ownerCreature, null, false);
        await PowerCmd.Apply<WithPower>(target, TransferAmount, ownerCreature, null, false);
        await GrantFirewoodOrReplayExisting(source);
    }

    private async Task GrantFirewoodOrReplayExisting(AbstractModel source)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var existingFirewood = PileType.Hand.GetPile(Owner).Cards
            .Where(FateFirewoodStickCombatHelper.IsFirewoodCard)
            .FirstOrDefault();
        if (existingFirewood != null)
        {
            SinkouSetHelper.AddReplayViaReflection(existingFirewood, 1);
            CardCmd.Preview(existingFirewood);
            Flash();
            return;
        }

        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFateFirewoodStick>(), Owner);
        if (source is CardModel sourceCard && FateFirewoodStickCombatHelper.IsFirewoodCard(sourceCard))
            SinkouSetHelper.AddReplayViaReflection(card, 1);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, source);
        Flash();
    }
}
