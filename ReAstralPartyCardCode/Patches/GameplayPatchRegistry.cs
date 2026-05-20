using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Daily;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public static class GameplayPatchRegistry
{
    public static void RegisterAndApply()
    {
        var patcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "gameplay-patches", "gameplay patches");
        patcher.RegisterPatch<MainMenuLoadedToastPatch>();
        patcher.RegisterPatches<GameplayStaticPatches>();
        patcher.ApplyDynamic(GameplayDynamicPatches.CreateBuilder(), true);

        if (!patcher.PatchAll())
            throw new InvalidOperationException($"Failed to apply required gameplay patches for {MainFile.ModId}.");
    }
}

public sealed class GameplayStaticPatches : IModPatches
{
    public static void AddTo(ModPatcher patcher)
    {
        GameplayStaticPatchCatalog.RegisterAll(patcher);
    }
}

public static class GameplayDynamicPatches
{
    public static DynamicPatchBuilder CreateBuilder()
    {
        return GameplayDynamicPatchCatalog.CreateBuilder();
    }
}

internal static class GameplayStaticPatchCatalog
{
    public static void RegisterAll(ModPatcher patcher)
    {
        RegisterUiPatches(patcher);
        RegisterGameplayPatches(patcher);
        RegisterFragileGameplayPatches(patcher);
    }

    private static void RegisterUiPatches(ModPatcher patcher)
    {
        patcher.RegisterPatches(
        [
            new ModPatchInfo(
                "choose_relic_header_patch",
                typeof(NChooseARelicSelection),
                "_Ready",
                typeof(ChooseRelicHeaderPatch),
                false,
                "UI patch: replace the shared relic-selection header text"),
            new ModPatchInfo(
                "persona_relic_collection_patch",
                typeof(NRelicCollectionCategory),
                "LoadRelics",
                typeof(PersonaRelicCollectionPatch),
                false,
                "UI patch: inject persona relic subsection into the compendium"),
            new ModPatchInfo(
                "skill_famous_blade_title_patch",
                typeof(CardModel),
                nameof(CardModel.Title),
                typeof(SkillFamousBladeTitlePatch),
                false,
                "UI patch: override Famous Blade title getter",
                harmonyMethodType: MethodType.Getter),
            new ModPatchInfo(
                "skill_famous_blade_description_patch",
                typeof(CardModel),
                nameof(CardModel.Description),
                typeof(SkillFamousBladeDescriptionPatch),
                false,
                "UI patch: override Famous Blade description getter",
                harmonyMethodType: MethodType.Getter),
            new ModPatchInfo(
                "top_bar_open_token_series_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar),
                nameof(MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar.Initialize),
                typeof(NTopBarOpenTokenSeriesPatch),
                false,
                "UI patch: append current open token series icon to the normal-mode top bar",
                [typeof(IRunState)]),
            new ModPatchInfo(
                "event_option_locked_hover_focus_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),
                "OnFocus",
                typeof(EventOptionLockedHoverFocusPatch),
                false,
                "UI patch: allow locked event options with hover data to still show hover tips"),
            new ModPatchInfo(
                "event_option_locked_hover_unfocus_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),
                "OnUnfocus",
                typeof(EventOptionLockedHoverUnfocusPatch),
                false,
                "UI patch: clean up hover tips for locked event options"),
            new ModPatchInfo(
                "multiplayer_relic_animation_safety_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder),
                nameof(MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder.PlayNewlyAcquiredAnimation),
                typeof(MultiplayerRelicAnimationSafetyPatch),
                false,
                "UI patch: skip newly acquired relic animations when the inventory holder is no longer usable",
                [typeof(Godot.Vector2?), typeof(Godot.Vector2?)])
        ]);
    }

    private static void RegisterGameplayPatches(ModPatcher patcher)
    {
        patcher.RegisterPatches(
        [
            new ModPatchInfo(
                "persona_max_hand_size_guard_patch",
                typeof(MaxHandSizeCalculator),
                nameof(MaxHandSizeCalculator.ApplyHookListenerModifiers),
                typeof(PersonaMaxHandSizeGuardPatch),
                false,
                "Gameplay patch: guard persona relic max-hand-size modifiers when hook-listener enumeration misses them",
                [typeof(Player), typeof(int)]),
            new ModPatchInfo(
                "astral_relic_store_event_override_patch",
                typeof(ActModel),
                nameof(ActModel.PullNextEvent),
                typeof(AstralRelicStoreEventOverridePatch),
                false,
                "Gameplay patch: force the first actual second-act event pull to become Astral Relic Store",
                [typeof(RunState)]),
            new ModPatchInfo(
                "relic_grab_bag_populate_series_filter_patch",
                typeof(RelicGrabBag),
                nameof(RelicGrabBag.Populate),
                typeof(RelicGrabBagPopulateSeriesFilterPatch),
                false,
                "Gameplay patch: remove unopened special token series relics from random relic grab bags at run setup",
                [typeof(Player), typeof(MegaCrit.Sts2.Core.Random.Rng)])
        ]);

        patcher.RegisterPatch<PersonaSkillNaturalObtainFilterPatch>();
        patcher.RegisterPatch<CreatureHealBaiZeBlessingPatch>();
        patcher.RegisterPatch<CreatureHealDorothyWarmPatch>();
        patcher.RegisterPatch<ExtremeModeTurnLimitPatch>();
    }

    private static void RegisterFragileGameplayPatches(ModPatcher patcher)
    {
        patcher.RegisterPatch<StartingPersonaRelicSelectionPatch>();
        patcher.RegisterPatch<AstralTelemetryStartRunPatch>();
        patcher.RegisterPatch<AstralTelemetryLoadRunPatch>();
        patcher.RegisterPatch<AstralTelemetryRunEndedPatch>();
        patcher.RegisterPatch<AstralTelemetryAbandonPatch>();
        patcher.RegisterPatch<AstralTelemetryDeleteCurrentRunPatch>();
        patcher.RegisterPatch<ReAstralPartyRunSettingsLoadPatch>();
    }
}

internal static class GameplayDynamicPatchCatalog
{
    public static DynamicPatchBuilder CreateBuilder()
    {
        var builder = new DynamicPatchBuilder($"{MainFile.ModId}_dynamic");
        RegisterUiPatches(builder);
        RegisterMultiplayerPatches(builder);
        RegisterCompatibilityBridgePatches(builder);
        return builder;
    }

    private static void RegisterUiPatches(DynamicPatchBuilder builder)
    {
        builder.AddMethod(
            typeof(NHandCardHolder),
            nameof(NHandCardHolder.UpdateCard),
            Type.EmptyTypes,
            postfix: DynamicPatchBuilder.FromMethod(
                typeof(TemporaryCardHighlightPatch),
                nameof(TemporaryCardHighlightPatch.UpdateCardPostfix)),
            isCritical: false,
            description: "UI patch: highlight temporary cards when hand cards refresh",
            patchId: "temporary_card_highlight_update");
        builder.AddMethod(
            typeof(NHandCardHolder),
            nameof(NHandCardHolder.Flash),
            Type.EmptyTypes,
            postfix: DynamicPatchBuilder.FromMethod(
                typeof(TemporaryCardHighlightPatch),
                nameof(TemporaryCardHighlightPatch.FlashPostfix)),
            isCritical: false,
            description: "UI patch: re-apply temporary card highlight after flash",
            patchId: "temporary_card_highlight_flash");
        builder.AddMethod(
            typeof(NMapScreen),
            "RecalculateTravelability",
            Type.EmptyTypes,
            postfix: DynamicPatchBuilder.FromMethod(
                typeof(SpeedRollerFlightTravelabilityPatch),
                nameof(SpeedRollerFlightTravelabilityPatch.Postfix)),
            isCritical: false,
            description: "UI patch: allow single-use Speed Roller flight to open the next row's map points",
            patchId: "speed_roller_flight_travelability");
        builder.AddMethod(
            typeof(NNormalMapPoint),
            "_Ready",
            Type.EmptyTypes,
            postfix: DynamicPatchBuilder.FromMethod(
                typeof(JunkBotQuestIconMapPatch),
                nameof(JunkBotQuestIconMapPatch.Postfix)),
            isCritical: false,
            description: "UI patch: replace Junk Bot quest markers with the custom map icon",
            patchId: "junk_bot_quest_icon_map");
        TryRegisterJunkBotMapScreenPatch(builder);
        builder.AddMethod(
            typeof(MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen.NMapPointHistoryEntry),
            nameof(MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen.NMapPointHistoryEntry.SetPlayer),
            [typeof(RunHistoryPlayer)],
            postfix: DynamicPatchBuilder.FromMethod(
                typeof(JunkBotQuestIconHistoryPatch),
                nameof(JunkBotQuestIconHistoryPatch.Postfix)),
            isCritical: false,
            description: "UI patch: replace Junk Bot completed quest markers in run history",
            patchId: "junk_bot_quest_icon_history");
    }

    private static void TryRegisterJunkBotMapScreenPatch(DynamicPatchBuilder builder)
    {
        var postfix = DynamicPatchBuilder.FromMethod(
            typeof(JunkBotQuestIconMapScreenPatch),
            nameof(JunkBotQuestIconMapScreenPatch.Postfix));

        var target = AccessTools.DeclaredMethod(
            typeof(NMapScreen),
            "SetMap",
            [typeof(MegaCrit.Sts2.Core.Map.ActMap), typeof(uint), typeof(bool)]);
        if (target != null)
            builder.Add(
                target,
                postfix: postfix,
                isCritical: false,
                description: "UI patch: re-apply Junk Bot quest markers after map screen refresh",
                patchId: "junk_bot_quest_icon_map_screen_refresh");
    }

    private static void RegisterCompatibilityBridgePatches(DynamicPatchBuilder builder)
    {
        var cooldownPrefix = DynamicPatchBuilder.FromMethod(
            typeof(CooldownEnchantmentGrantPatch),
            nameof(CooldownEnchantmentGrantPatch.Prefix));

        TryRegisterCooldownBridgePatch(
            builder,
            "cooldown_enchantment_grant_4arg",
            [typeof(CardModel), typeof(PileType), typeof(bool), typeof(CardPilePosition)],
            cooldownPrefix);

        TryRegisterCooldownBridgePatch(
            builder,
            "cooldown_enchantment_grant_3arg",
            [typeof(CardModel), typeof(PileType), typeof(bool)],
            cooldownPrefix);
    }

    private static void TryRegisterCooldownBridgePatch(
        DynamicPatchBuilder builder,
        string patchId,
        Type[] parameterTypes,
        HarmonyMethod cooldownPrefix)
    {
        var target = AccessTools.DeclaredMethod(
            typeof(CardPileCmd),
            nameof(CardPileCmd.AddGeneratedCardToCombat),
            parameterTypes);
        if (target == null)
            return;

        builder.Add(
            target,
            cooldownPrefix,
            description: "Compatibility patch: auto-apply cooldown enchantment to generated persona skills",
            patchId: patchId);
    }

    private static void RegisterMultiplayerPatches(DynamicPatchBuilder builder)
    {
        var speedRollerConsumeTarget = AccessTools.DeclaredMethod(
            typeof(MegaCrit.Sts2.Core.GameActions.MoveToMapCoordAction),
            "ExecuteAction",
            Type.EmptyTypes);
        if (speedRollerConsumeTarget != null)
            builder.Add(
                speedRollerConsumeTarget,
                DynamicPatchBuilder.FromMethod(
                    typeof(SpeedRollerFlightConsumePatch),
                    nameof(SpeedRollerFlightConsumePatch.Prefix)),
                isCritical: false,
                description:
                "Gameplay patch: consume one Speed Roller flight charge when moving to an otherwise unreachable next-row map point",
                patchId: "speed_roller_flight_consume");
    }
}
