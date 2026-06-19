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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class VariantPersonManosabaLinHiro : PersonaRelicBase
{
    private const int RewardBandSize = 100;
    private const decimal TeamDamageWithGain = 10m;

    [SavedProperty] public int AstralParty_VariantPersonManosabaLinHiroLastRewardBand { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public override Task BeforeCombatStart()
    {
        AstralParty_VariantPersonManosabaLinHiroLastRewardBand = GetCurrentRewardBand();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null || !ManosabaLinCompat.IsLoaded())
            return;

        await GrantSaveOrReplayExisting(this);
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
                await GrantSaveOrReplayExisting(this);
        }

        AstralParty_VariantPersonManosabaLinHiroLastRewardBand = currentBand;
    }

    private int GetCurrentRewardBand()
    {
        return StableNumericStateHelper.FloorDivisionToNonNegativeInt(GetCurrentWithAmount(), RewardBandSize);
    }

    private decimal GetCurrentWithAmount()
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null || !ManosabaLinCompat.IsLoaded())
            return 0m;

        if (!ManosabaLinCompat.TryFindWithPower(out var externalWithPower))
            return 0m;

        return ownerCreature.Powers
            .FirstOrDefault(power => power.Id == externalWithPower.Id)
            ?.Amount ?? 0m;
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

    private async Task GrantSaveOrReplayExisting(AbstractModel source)
    {
        if (Owner?.Creature?.CombatState == null || !ManosabaLinCompat.IsLoaded())
            return;
        if (!ManosabaLinCompat.TryFindSaveCard(out var saveCard))
            return;

        var existingSave = PileType.Hand.GetPile(Owner).Cards
            .FirstOrDefault(card => card.Id == saveCard.Id || card.CanonicalInstance?.Id == saveCard.Id);
        if (existingSave != null)
        {
            SinkouSetHelper.AddReplayViaReflection(existingSave, 1);
            CardCmd.Preview(existingSave);
            Flash();
            return;
        }

        var card = Owner.Creature.CombatState.CreateCard(saveCard.CanonicalInstance ?? saveCard, Owner);
        if (source is CardModel sourceCard && (sourceCard.Id == saveCard.Id || sourceCard.CanonicalInstance?.Id == saveCard.Id))
            SinkouSetHelper.AddReplayViaReflection(card, 1);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, source);
        Flash();
    }
}
