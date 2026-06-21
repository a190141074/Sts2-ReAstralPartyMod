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
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.ManosabaLin;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class VariantPersonManosabaLinHiro : PersonRelicBase
{
    private const int RewardBandSize = 100;
    private const int TransferAmount = 40;
    private const decimal TeamDamageWithGain = 10m;

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
        AstralParty_VariantPersonManosabaLinHiroLastRewardBand = GetCurrentRewardBand();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null || !ManosabaLinCompat.IsLoaded())
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
        if (!ManosabaLinCompat.IsLoaded())
            return;

        await ApplyExternalWithPower(ownerCreature, TeamDamageWithGain, null);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || !IsExternalWithPower(power))
            return;

        var currentBand = StableNumericStateHelper.FloorDivisionToNonNegativeInt(power.Amount, RewardBandSize);
        if (amount > 0m && currentBand > AstralParty_VariantPersonManosabaLinHiroLastRewardBand)
        {
            var rewardCount = currentBand - AstralParty_VariantPersonManosabaLinHiroLastRewardBand;
            for (var i = 0; i < rewardCount; i++)
                await GrantFirewoodOrReplayExisting(this);
        }

        AstralParty_VariantPersonManosabaLinHiroLastRewardBand = currentBand;
    }

    private int GetCurrentRewardBand()
    {
        return StableNumericStateHelper.FloorDivisionToNonNegativeInt(GetCurrentWithAmount(), RewardBandSize);
    }

    internal int GetCurrentWithPower()
    {
        return StableNumericStateHelper.FloorToNonNegativeInt(GetCurrentWithAmount());
    }

    internal decimal GetWithPowerAmount(Creature? creature)
    {
        if (creature == null || !ManosabaLinCompat.IsLoaded())
            return 0m;

        if (!ManosabaLinCompat.TryFindWithPower(out var externalWithPower))
            return 0m;

        return creature.Powers
            .FirstOrDefault(power => power.Id == externalWithPower.Id)
            ?.Amount ?? 0m;
    }

    internal async Task TryTransferWithPowerAndRegainCard(Creature target, AbstractModel source)
    {
        var ownerCreature = Owner?.Creature;
        if (Owner == null || ownerCreature == null || target.Player == null)
            return;
        if (!ManosabaLinCompat.IsLoaded())
            return;

        var currentWithAmount = GetCurrentWithAmount();
        if (currentWithAmount < TransferAmount)
            return;
        if (!ManosabaLinCompat.TryFindWithPower(out var externalWithPower))
            return;

        if (ownerCreature.Powers.FirstOrDefault(power => power.Id == externalWithPower.Id) is not { } ownerWithPower)
            return;

        await PowerCmd.ModifyAmount(ownerWithPower, -TransferAmount, ownerCreature, null, false);
        await ApplyExternalWithPower(target, TransferAmount, null);
        await GrantFirewoodOrReplayExisting(source);
    }

    private decimal GetCurrentWithAmount()
    {
        return GetWithPowerAmount(Owner?.Creature);
    }

    private bool IsExternalWithPower(PowerModel power)
    {
        if (Owner?.Creature == null || power.Owner != Owner.Creature || !ManosabaLinCompat.IsLoaded())
            return false;
        if (!ManosabaLinCompat.TryFindWithPower(out var externalWithPower))
            return false;

        return power.Id == externalWithPower.Id;
    }

    private async Task ApplyExternalWithPower(Creature target, decimal amount, CardModel? sourceCard)
    {
        if (!ManosabaLinCompat.TryFindWithPower(out var externalWithPower))
            return;

        await PowerCmd.Apply(externalWithPower.ToMutable(), target, amount, Owner?.Creature, sourceCard, false);
    }

    private async Task GrantFirewoodOrReplayExisting(AbstractModel source)
    {
        if (Owner?.Creature?.CombatState == null || !ManosabaLinCompat.IsLoaded())
            return;

        var existingFirewood = PileType.Hand.GetPile(Owner).Cards
            .FirstOrDefault(FateFirewoodStickCombatHelper.IsFirewoodCard);
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
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, source);
        Flash();
    }
}
