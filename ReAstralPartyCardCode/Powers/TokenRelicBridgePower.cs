using System.Text.Json;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class TokenRelicBridgePower : AstralPartyPowerModel
{
    private RelicModel? _bridgedRelic;
    private string? _pendingRelicStateJson;
    private ModelId _bridgedTokenRelicId = ModelId.none;

    [SavedProperty]
    private string AstralParty_BridgedTokenRelicId
    {
        get => _bridgedTokenRelicId == ModelId.none ? string.Empty : _bridgedTokenRelicId.ToString();
        set => _bridgedTokenRelicId = DeserializeModelIdOrNone(value);
    }

    [SavedProperty]
    public bool AstralParty_RunRelicAfterObtainedOnFirstApply { get; set; }

    [SavedProperty]
    public int AstralParty_BridgeInitializationModeValue { get; set; }

    [SavedProperty]
    private string AstralParty_BridgedTokenRelicStateJson
    {
        get => SerializeBridgedRelicState();
        set => _pendingRelicStateJson = value;
    }

    public override PowerType Type => PowerType.Buff;

    public override bool IsInstanced => true;

    public override PowerStackType StackType => PowerStackType.None;

    public override string? CustomIconPath => GetBridgedRelic()?.PackedIconPath ?? base.CustomIconPath;

    public override string? CustomBigIconPath =>
        (GetBridgedRelic() as AstralPartyRelicModel)?.PublicBigIconPath
        ?? GetBridgedRelic()?.PackedIconPath
        ?? base.CustomBigIconPath;

    public override LocString Title => GetBridgedRelic()?.Title ?? base.Title;

    public override LocString Description => GetBridgedRelic()?.DynamicDescription ?? base.Description;

    public override int DisplayAmount
    {
        get
        {
            var relic = GetBridgedRelic();
            if (relic?.ShowCounter != true)
                return 0;

            return relic.DisplayAmount;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetBridgedRelic()?.HoverTipsExcludingRelic ?? [];

    public ModelId BridgedTokenRelicId
    {
        get => _bridgedTokenRelicId;
        set => _bridgedTokenRelicId = value;
    }

    public void Configure(
        ModelId relicId,
        TokenRelicBridgeInitializationMode initializationMode = TokenRelicBridgeInitializationMode.None)
    {
        BridgedTokenRelicId = relicId;
        AstralParty_RunRelicAfterObtainedOnFirstApply =
            initializationMode != TokenRelicBridgeInitializationMode.None;
        AstralParty_BridgeInitializationModeValue = (int)initializationMode;
        _bridgedRelic = null;
        _pendingRelicStateJson = null;
        InvokeDisplayAmountChanged();
    }

    public RelicModel? GetBridgedRelic()
    {
        if (_bridgedRelic != null)
            return _bridgedRelic;

        if (BridgedTokenRelicId == ModelId.none)
            return null;

        if (!TokenRelicBridgeHelper.CanBridgeTokenRelic(BridgedTokenRelicId, out var reason))
        {
            MainFile.Logger.Warn(
                $"[TokenRelicBridgePower] Refused to initialize bridge for {BridgedTokenRelicId}: {reason}");
            return null;
        }

        var ownerPlayer = Owner?.Player ?? Owner?.PetOwner;
        if (ownerPlayer == null)
            return null;

        var relic = ModelDb.GetById<RelicModel>(BridgedTokenRelicId).ToMutable();
        relic.Owner = ownerPlayer;
        RestoreBridgedRelicState(relic);
        relic.Flashed += HandleRelicFlashed;
        relic.DisplayAmountChanged += HandleRelicDisplayAmountChanged;
        _bridgedRelic = relic;
        return _bridgedRelic;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var relic = GetBridgedRelic();
        if (relic == null)
            return;

        var initializationMode = GetInitializationMode();
        if (initializationMode != TokenRelicBridgeInitializationMode.None)
        {
            using var _ = TokenRelicBridgeInitializationContext.Push(
                initializationMode,
                BridgedTokenRelicId);
            await relic.AfterObtained();
        }

        await ReplayCurrentTurnStartHooksIfNeeded(relic);
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeCombatStart()
    {
        return ForwardAsync(relic => relic.BeforeCombatStart());
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        return ForwardAsync(relic => relic.AfterCombatEnd(room));
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        return ForwardAsync(relic => relic.BeforeSideTurnStart(choiceContext, side, combatState));
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        return ForwardAsync(relic => relic.AfterPlayerTurnStart(choiceContext, player));
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        return ForwardAsync(relic => relic.BeforeCardPlayed(cardPlay));
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        return ForwardAsync(relic => relic.AfterCardPlayed(context, cardPlay));
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        return ForwardAsync(relic => relic.AfterCardDrawn(choiceContext, card, fromHandDraw));
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        return ForwardAsync(relic => relic.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource));
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return ForwardAsync(
            relic => relic.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource));
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return ForwardAsync(relic => relic.AfterTurnEnd(choiceContext, side));
    }

    public override Task AfterGoldGained(Player player)
    {
        return ForwardAsync(relic => relic.AfterGoldGained(player));
    }

    public override Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        return ForwardAsync(relic => relic.AfterPowerAmountChanged(power, amount, applier, cardSource));
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        return ForwardAsync(relic => relic.BeforeAttack(command));
    }

    public override Task AfterEnergySpent(CardModel card, int amount)
    {
        return ForwardAsync(relic => relic.AfterEnergySpent(card, amount));
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return Forward(
            relic => relic.ModifyDamageAdditive(target, amount, props, dealer, cardSource),
            0m);
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return Forward(relic => relic.ModifyMaxEnergy(player, amount), amount);
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        var relic = GetBridgedRelic();
        if (relic == null)
        {
            modifiedAmount = amount;
            return false;
        }

        return relic.TryModifyPowerAmountReceived(canonicalPower, target, amount, applier, out modifiedAmount);
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (_bridgedRelic != null)
        {
            _bridgedRelic.Flashed -= HandleRelicFlashed;
            _bridgedRelic.DisplayAmountChanged -= HandleRelicDisplayAmountChanged;
            _bridgedRelic = null;
        }

        return Task.CompletedTask;
    }

    private Task ForwardAsync(Func<RelicModel, Task> callback)
    {
        var relic = GetBridgedRelic();
        if (relic == null)
            return Task.CompletedTask;

        return callback(relic);
    }

    private T Forward<T>(Func<RelicModel, T> callback, T fallback)
    {
        var relic = GetBridgedRelic();
        return relic == null ? fallback : callback(relic);
    }

    private string SerializeBridgedRelicState()
    {
        var relic = _bridgedRelic;
        if (relic == null)
            return _pendingRelicStateJson ?? string.Empty;

        var savedProperties = SavedProperties.From(relic);
        if (savedProperties == null)
            return string.Empty;

        return JsonSerializer.Serialize(savedProperties);
    }

    private void RestoreBridgedRelicState(RelicModel relic)
    {
        if (string.IsNullOrWhiteSpace(_pendingRelicStateJson))
            return;

        try
        {
            var savedProperties = JsonSerializer.Deserialize<SavedProperties>(_pendingRelicStateJson);
            savedProperties?.Fill(relic);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"[TokenRelicBridgePower] Failed to restore bridge state for {BridgedTokenRelicId}: {ex}");
        }
    }

    private void HandleRelicFlashed(RelicModel _, IEnumerable<Creature> __)
    {
        Flash();
    }

    private void HandleRelicDisplayAmountChanged()
    {
        InvokeDisplayAmountChanged();
    }

    private async Task ReplayCurrentTurnStartHooksIfNeeded(RelicModel relic)
    {
        var owner = Owner;
        var ownerPlayer = owner?.Player;
        var combatState = owner?.CombatState;
        if (ownerPlayer == null || combatState == null)
            return;

        if (combatState.CurrentSide != owner.Side)
            return;

        var choiceContext = CreateImmediateHookContext();

        await relic.BeforeSideTurnStart(choiceContext, combatState.CurrentSide, combatState);
        await relic.AfterPlayerTurnStart(choiceContext, ownerPlayer);
    }

    private TokenRelicBridgeInitializationMode GetInitializationMode()
    {
        if (Enum.IsDefined(typeof(TokenRelicBridgeInitializationMode), AstralParty_BridgeInitializationModeValue))
            return (TokenRelicBridgeInitializationMode)AstralParty_BridgeInitializationModeValue;

        return AstralParty_RunRelicAfterObtainedOnFirstApply
            ? TokenRelicBridgeInitializationMode.RunAfterObtained
            : TokenRelicBridgeInitializationMode.None;
    }

    private static ModelId DeserializeModelIdOrNone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ModelId.none;

        try
        {
            return ModelId.Deserialize(value);
        }
        catch
        {
            return ModelId.none;
        }
    }

    private PlayerChoiceContext CreateImmediateHookContext()
    {
        if (Owner?.CombatState != null && LocalContext.NetId.HasValue)
            return new HookPlayerChoiceContext(this, LocalContext.NetId.Value, Owner.CombatState, GameActionType.Combat);

        return NoChoicePlayerChoiceContext.Instance;
    }

    private sealed class NoChoicePlayerChoiceContext : PlayerChoiceContext
    {
        public static readonly NoChoicePlayerChoiceContext Instance = new();

        public override Task SignalPlayerChoiceBegun(PlayerChoiceOptions options)
        {
            return Task.CompletedTask;
        }

        public override Task SignalPlayerChoiceEnded()
        {
            return Task.CompletedTask;
        }
    }
}
