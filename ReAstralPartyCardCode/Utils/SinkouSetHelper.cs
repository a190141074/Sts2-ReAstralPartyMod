using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class SinkouSetHelper
{
    private static readonly string[] ReplayMemberNames =
    [
        "Replay",
        "ReplayCount",
        "ReplayAmount"
    ];

    private static readonly string[] RunDeckMemberNames =
    [
        "Deck",
        "Cards",
        "CardModels",
        "MasterDeck",
        "StartingDeck"
    ];

    public static bool HasVariantSinkou(Player? owner)
    {
        return owner?.GetRelic<VariantPersonSinkou>() != null;
    }

    public static bool HasMiniatureMilen(Player? owner)
    {
        return owner?.GetRelic<JewelryMiniatureMilen>() != null;
    }

    public static bool HasSolarCrown(Player? owner)
    {
        return owner?.GetRelic<JewelrySolarCrown>() != null;
    }

    public static bool HasEchoOfDivineLight(Player? owner)
    {
        return owner?.GetRelic<JewelryEchoOfDivineLight>() != null;
    }

    public static bool HasFullListeningToSolarRoarSet(Player? owner)
    {
        return RelicOwnershipHelper.HasAllRelics(
            owner,
            typeof(VariantPersonSinkou),
            typeof(JewelryMiniatureMilen),
            typeof(JewelrySolarCrown),
            typeof(JewelryEchoOfDivineLight));
    }

    public static bool ShouldPunitiveJudgmentExtraDamageScaleWithBurn(Player? owner)
    {
        return HasVariantSinkou(owner) && HasMiniatureMilen(owner);
    }

    public static bool CanTriggerRageBurn(Player? owner)
    {
        return HasVariantSinkou(owner)
               || owner?.Creature?.HasPower<WhereDivineLightShinesPower>() == true;
    }

    public static int GetCurrentAct(Player? owner)
    {
        return Math.Max((owner?.RunState?.CurrentActIndex ?? 0) + 1, 1);
    }

    public static async Task TriggerExtraBurnAtTurnStart(Player owner, AbstractModel? source)
    {
        if (owner.Creature?.CombatState == null)
            return;

        foreach (var enemy in CombatTargetSnapshotHelper.GetAliveOpponents(owner.Creature))
        {
            var burn = enemy.GetPower<BlazingSolarBurnPower>();
            if (burn == null || burn.Amount <= 0m)
                continue;

            var context = CreateImmediateHookContext(owner, source);
            await burn.AfterPlayerTurnStart(context, owner);
        }
    }

    public static async Task UpgradePunitiveJudgmentInHand(CardModel card)
    {
        if (card is not SkillPunitiveJudgment)
            return;

        if (!card.IsUpgraded)
            CardCmd.Upgrade(card);

        AddReplayViaReflection(card, 1);
        await Task.CompletedTask;
    }

    public static void AddReplayViaReflection(CardModel card, int amount)
    {
        if (amount <= 0)
            return;

        foreach (var memberName in ReplayMemberNames)
        {
            if (TryModifyReplayProperty(card, memberName, amount))
                return;
        }

        MainFile.Logger.Warn($"[SinkouSetHelper] Failed to locate replay member on card {card.Id.Entry}.");
    }

    public static bool TryAddUpgradedPunitiveJudgmentToDeck(Player owner, AbstractModel? source)
    {
        if (owner.RunState == null)
            return false;

        var mutableCard = ModelDb.Card<SkillPunitiveJudgment>().ToMutable();
        mutableCard.Owner = owner;
        CardCmd.Upgrade(mutableCard);

        var runStateType = owner.RunState.GetType();
        foreach (var memberName in RunDeckMemberNames)
        {
            var member = runStateType.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            if (ReadMemberValue(owner.RunState, member) is not System.Collections.IList list)
                continue;

            list.Add(mutableCard);
            MainFile.Logger.Info(
                $"[SinkouSetHelper] Added upgraded Punitive Judgment to run deck | owner={owner.NetId} | member={memberName} | source={source?.Id.Entry}");
            return true;
        }

        MainFile.Logger.Warn($"[SinkouSetHelper] Failed to locate run deck container for owner {owner.NetId}.");
        return false;
    }

    public static IEnumerable<IHoverTip> BuildSetHoverTips(Player? owner)
    {
        return
        [
            AstralKeywords.CreateHoverTip(AstralKeywords.AstralListeningToSolarRoarSetId),
            BuildCurrentSetHoverTip(owner)
        ];
    }

    public static IEnumerable<DynamicVar> BuildSetDynamicVars(Player? owner)
    {
        return
        [
            new StringVar("CurrentSetLine", GetCurrentSetLineText(owner))
        ];
    }

    public static string GetCurrentSetLineText(Player? owner)
    {
        return GetCurrentSetLine(owner)?.GetRawText()
               ?? new LocString("relics",
                   "RE_ASTRAL_PARTY_MOD_RELIC_SINKOU_SET.current_set_none").GetRawText();
    }

    public static HoverTip BuildCurrentSetHoverTip(Player? owner)
    {
        var title = new LocString("relics", "RE_ASTRAL_PARTY_MOD_RELIC_SINKOU_SET.current_set_title");
        var body = GetCurrentSetLineText(owner);
        return new HoverTip(title, body, GD.Load<Texture2D>("res://ReAstralPartyMod/images/relic/jewelry_solar_crown.png"));
    }

    public static LocString? GetCurrentSetLine(Player? owner)
    {
        return HasFullListeningToSolarRoarSet(owner)
            ? new LocString("relics", "RE_ASTRAL_PARTY_MOD_RELIC_SINKOU_SET.current_set_line_active")
            : new LocString("relics", "RE_ASTRAL_PARTY_MOD_RELIC_SINKOU_SET.current_set_none");
    }

    private static PlayerChoiceContext CreateImmediateHookContext(Player owner, AbstractModel? source)
    {
        return new ThrowingPlayerChoiceContext();
    }

    private static bool TryModifyReplayProperty(CardModel card, string memberName, int amount)
    {
        var property = card.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.CanRead && property.CanWrite)
        {
            var currentValue = property.GetValue(card);
            if (TryConvertToInt(currentValue, out var currentReplay))
            {
                property.SetValue(card, currentReplay + amount);
                return true;
            }
        }

        var field = card.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            var currentValue = field.GetValue(card);
            if (TryConvertToInt(currentValue, out var currentReplay))
            {
                field.SetValue(card, currentReplay + amount);
                return true;
            }
        }

        return false;
    }

    private static object? ReadMemberValue(object source, MemberInfo member)
    {
        return member switch
        {
            PropertyInfo propertyInfo => propertyInfo.GetValue(source),
            FieldInfo fieldInfo => fieldInfo.GetValue(source),
            _ => null
        };
    }

    private static bool TryConvertToInt(object? value, out int result)
    {
        switch (value)
        {
            case int intValue:
                result = intValue;
                return true;
            case decimal decimalValue:
                result = Convert.ToInt32(decimalValue);
                return true;
            case long longValue:
                result = (int)longValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case byte byteValue:
                result = byteValue;
                return true;
            default:
                result = 0;
                return false;
        }
    }
}
