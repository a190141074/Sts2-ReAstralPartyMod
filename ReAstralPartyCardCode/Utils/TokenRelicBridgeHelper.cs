using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class TokenRelicBridgeHelper
{
    private static readonly HashSet<string> AllowedOverrideNames =
    [
        nameof(AbstractModel.BeforeCombatStart),
        nameof(AbstractModel.AfterCombatEnd),
        nameof(AbstractModel.BeforeSideTurnStart),
        nameof(AbstractModel.AfterSideTurnStart),
        nameof(AbstractModel.AfterPlayerTurnStart),
        nameof(AbstractModel.BeforeCardPlayed),
        nameof(AbstractModel.AfterCardPlayed),
        nameof(AbstractModel.AfterCardDrawn),
        nameof(AbstractModel.AfterDamageGiven),
        nameof(AbstractModel.AfterDamageReceived),
        nameof(AbstractModel.AfterSideTurnEnd),
        nameof(AbstractModel.AfterGoldGained),
        nameof(AbstractModel.AfterPowerAmountChanged),
        nameof(AbstractModel.BeforeAttack),
        nameof(AbstractModel.AfterEnergySpent),
        nameof(AbstractModel.ModifyDamageAdditive),
        nameof(AbstractModel.ModifyMaxEnergy),
        nameof(AbstractModel.TryModifyPowerAmountReceived),
        nameof(RelicModel.AfterObtained),
        nameof(RelicModel.AfterRemoved),
        "get_ShowCounter",
        "get_DisplayAmount",
        "get_ExtraHoverTips",
        "get_Rarity",
        "get_ShouldReceiveCombatHooks",
        "get_FlashSfx",
        "get_ShouldFlashOnPlayer",
        "get_Title",
        "get_DynamicDescription",
        "get_HoverTipsExcludingRelic",
        "get_PackedIconPath",
        "get_BigIconPath",
        "get_PackedIconOutlinePath",
        "get_AssetProfile",
        "get_IconBasePath",
        "get_RelicId"
    ];

    public static async Task<TokenRelicBridgePower?> ApplyTokenRelicPower<TTokenRelic>(
        Creature target,
        Creature? applier = null,
        CardModel? cardSource = null,
        bool removeExisting = true,
        TokenRelicBridgeInitializationMode initializationMode = TokenRelicBridgeInitializationMode.None)
        where TTokenRelic : RelicModel
    {
        return await ApplyTokenRelicPower(
            target,
            ModelDb.GetId(typeof(TTokenRelic)),
            applier,
            cardSource,
            removeExisting,
            initializationMode);
    }

    public static async Task<TokenRelicBridgePower?> ApplyTokenRelicPower(
        Creature target,
        ModelId tokenRelicId,
        Creature? applier = null,
        CardModel? cardSource = null,
        bool removeExisting = true,
        TokenRelicBridgeInitializationMode initializationMode = TokenRelicBridgeInitializationMode.None)
    {
        if (!CanBridgeTokenRelic(tokenRelicId, out var reason))
        {
            MainFile.Logger.Warn(
                $"[TokenRelicBridgeHelper] Refused to apply bridge power for {tokenRelicId}: {reason}");
            return null;
        }

        if (target.Player == null && target.PetOwner == null)
        {
            MainFile.Logger.Warn(
                $"[TokenRelicBridgeHelper] Refused to apply bridge power for {tokenRelicId}: target {target.LogName} is not owned by a player.");
            return null;
        }

        var existing = GetExistingBridgePower(target, tokenRelicId);
        if (existing != null)
            return existing;

        if (removeExisting)
            await RemoveAllBridgePowersForToken(target, tokenRelicId);

        var power = ModelDb.Power<TokenRelicBridgePower>().ToMutable() as TokenRelicBridgePower;
        if (power == null)
            throw new InvalidOperationException("Failed to create mutable TokenRelicBridgePower instance.");

        power.Configure(tokenRelicId, initializationMode);
        await PowerCmd.Apply(power, target, 1m, applier, cardSource, false);
        return power;
    }

    public static TokenRelicBridgePower? GetExistingBridgePower(Creature target, ModelId tokenRelicId)
    {
        return target
            .GetPowerInstances<TokenRelicBridgePower>()
            .FirstOrDefault(power => power.BridgedTokenRelicId == tokenRelicId);
    }

    public static async Task RemoveAllBridgePowersForToken(Creature target, ModelId tokenRelicId)
    {
        var existingPowers = target
            .GetPowerInstances<TokenRelicBridgePower>()
            .Where(power => power.BridgedTokenRelicId == tokenRelicId)
            .ToList();

        foreach (var power in existingPowers)
            await PowerCmd.Remove(power);
    }

    public static bool CanBridgeTokenRelic<TTokenRelic>(out string reason)
        where TTokenRelic : RelicModel
    {
        return CanBridgeTokenRelic(ModelDb.GetId(typeof(TTokenRelic)), out reason);
    }

    public static bool CanBridgeTokenRelic(ModelId tokenRelicId, out string reason)
    {
        var relic = ModelDb.GetById<RelicModel>(tokenRelicId);
        return CanBridgeTokenRelic(relic, out reason);
    }

    public static bool CanBridgeTokenRelic(RelicModel relic, out string reason)
    {
        if (!TokenRelicRegistry.IsTokenRelic(relic))
        {
            reason = "not a registered token relic";
            return false;
        }

        var unsupportedOverrides = GetUnsupportedOverrides(relic.GetType());
        if (unsupportedOverrides.Count > 0)
        {
            reason = $"unsupported overrides: {string.Join(", ", unsupportedOverrides)}";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static IReadOnlyList<string> GetUnsupportedOverrides(Type relicType)
    {
        var unsupported = new SortedSet<string>(StringComparer.Ordinal);

        for (var current = relicType;
             current != null && current != typeof(AstralPartyRelicModel) && current != typeof(RelicModel);
             current = current.BaseType)
        {
            foreach (var method in current.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                                      BindingFlags.NonPublic |
                                                      BindingFlags.DeclaredOnly))
            {
                if (method.IsStatic)
                    continue;
                if (method.GetBaseDefinition().DeclaringType == method.DeclaringType)
                    continue;
                if (AllowedOverrideNames.Contains(method.Name))
                    continue;

                unsupported.Add(method.Name);
            }

            foreach (var property in current.GetProperties(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                var getter = property.GetGetMethod(true);
                if (getter == null || getter.IsStatic)
                    continue;
                if (getter.GetBaseDefinition().DeclaringType == getter.DeclaringType)
                    continue;
                if (AllowedOverrideNames.Contains(getter.Name))
                    continue;

                unsupported.Add(getter.Name);
            }
        }

        return unsupported.ToList();
    }
}
