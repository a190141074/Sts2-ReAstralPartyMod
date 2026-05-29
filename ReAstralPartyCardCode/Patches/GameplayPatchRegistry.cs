using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Daily;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
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
                "neow_face_the_shadow_icon_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),
                "_Ready",
                typeof(NeowDreamFaceTheShadowIconPatch),
                false,
                "UI patch: attach the Face the Shadow icon"),
            new ModPatchInfo(
                "dream_endless_mouth_of_destruction_option_ui_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),
                "_Ready",
                typeof(DreamEndlessMouthOfDestructionOptionUiPatch),
                false,
                "UI patch: render the Dream Endless Mouth of Destruction info option as a non-interactable display button"),
            new ModPatchInfo(
                "dream_endless_mouth_of_destruction_info_option_focus_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),
                "OnFocus",
                typeof(DreamEndlessMouthOfDestructionInfoOptionFocusPatch),
                false,
                "UI patch: suppress hover/focus interactions for the Dream Endless Mouth of Destruction info option"),
            new ModPatchInfo(
                "dream_endless_mouth_of_destruction_info_option_unfocus_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),
                "OnUnfocus",
                typeof(DreamEndlessMouthOfDestructionInfoOptionUnfocusPatch),
                false,
                "UI patch: suppress hover cleanup interactions for the Dream Endless Mouth of Destruction info option"),
            new ModPatchInfo(
                "dream_endless_mouth_of_destruction_info_option_release_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),
                "OnRelease",
                typeof(DreamEndlessMouthOfDestructionInfoOptionReleasePatch),
                false,
                "UI patch: swallow direct activation of the Dream Endless Mouth of Destruction info option"),
            new ModPatchInfo(
                "dream_endless_mouth_of_destruction_info_option_event_room_guard_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NEventRoom),
                "OptionButtonClicked",
                typeof(DreamEndlessMouthOfDestructionInfoOptionEventRoomGuardPatch),
                false,
                "UI patch: prevent event-room activation of the Dream Endless Mouth of Destruction info option",
                [typeof(EventOption), typeof(int)]),
            new ModPatchInfo(
                "multiplayer_relic_animation_safety_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder),
                nameof(MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder.PlayNewlyAcquiredAnimation),
                typeof(MultiplayerRelicAnimationSafetyPatch),
                false,
                "UI patch: skip newly acquired relic animations when the inventory holder is no longer usable",
                [typeof(Godot.Vector2?), typeof(Godot.Vector2?)]),
        ]);
    }

    private static void RegisterGameplayPatches(ModPatcher patcher)
    {
        patcher.RegisterPatches(
        [
            new ModPatchInfo(
                "card_model_paranoid_should_receive_hooks_patch",
                typeof(CardModel),
                nameof(CardModel.ShouldReceiveCombatHooks),
                typeof(CardModelParanoidShouldReceiveCombatHooksPatch),
                false,
                "Gameplay patch: force all cards with the paranoid essence enchantment to receive combat hooks",
                harmonyMethodType: MethodType.Getter),
            new ModPatchInfo(
                "card_model_paranoid_is_playable_patch",
                typeof(CardModel),
                "IsPlayable",
                typeof(CardModelParanoidIsPlayablePatch),
                false,
                "Gameplay patch: prevent manual play for all cards with the paranoid essence enchantment",
                harmonyMethodType: MethodType.Getter),
            new ModPatchInfo(
                "tyrant_form_manual_play_patch",
                typeof(CardModel),
                "IsPlayable",
                typeof(TyrantFormManualPlayPatch),
                false,
                "Gameplay patch: prevent manual play of Sovereign Blade while Tyrant Form is active",
                harmonyMethodType: MethodType.Getter),
            new ModPatchInfo(
                "card_model_paranoid_after_current_hp_changed_patch",
                typeof(AbstractModel),
                nameof(CardModel.AfterCurrentHpChanged),
                typeof(CardModelParanoidAfterCurrentHpChangedPatch),
                false,
                "Gameplay patch: auto-play all cards with the paranoid essence enchantment when their owner loses HP",
                [typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature), typeof(decimal)]),
            new ModPatchInfo(
                "card_model_essence_enchantment_should_receive_hooks_patch",
                typeof(CardModel),
                nameof(CardModel.ShouldReceiveCombatHooks),
                typeof(CardModelEssenceEnchantmentShouldReceiveCombatHooksPatch),
                false,
                "Gameplay patch: force all cards with eye of sun or sacred faith to receive combat hooks",
                harmonyMethodType: MethodType.Getter),
            new ModPatchInfo(
                "card_model_essence_enchantment_after_card_played_patch",
                typeof(AbstractModel),
                nameof(CardModel.AfterCardPlayed),
                typeof(CardModelEssenceEnchantmentAfterCardPlayedPatch),
                false,
                "Gameplay patch: advance eye of sun per-instance play counts and trigger burn on multiples of ten"),
            new ModPatchInfo(
                "card_model_essence_enchantment_modify_damage_patch",
                typeof(AbstractModel),
                nameof(CardModel.ModifyDamageAdditive),
                typeof(CardModelEssenceEnchantmentModifyDamagePatch),
                false,
                "Gameplay patch: apply sacred faith permanent instance damage scaling"),
            new ModPatchInfo(
                "card_model_essence_enchantment_after_damage_given_patch",
                typeof(AbstractModel),
                nameof(CardModel.AfterDamageGiven),
                typeof(CardModelEssenceEnchantmentAfterDamageGivenPatch),
                false,
                "Gameplay patch: record sacred faith permanent instance growth after kills"),
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
        patcher.RegisterPatch<DevConsoleUltimateChargeCommandPatch>();
        patcher.RegisterPatch<ExtremeModeTurnLimitPatch>();
        patcher.RegisterPatch<PunitiveJudgmentHandEntryPatch>();
    }

    private static void RegisterFragileGameplayPatches(ModPatcher patcher)
    {
        patcher.RegisterPatch<StartingPersonaRelicSelectionPatch>();
        patcher.RegisterPatches(
        [
            new ModPatchInfo(
                "starting_persona_neow_ready_initial_state_patch",
                typeof(AncientEventModel),
                "SetInitialEventState",
                typeof(StartingPersonaNeowReadyInitialStatePatch),
                false,
                "Gameplay patch: replace the finished initial Neow page with a starting-persona ready gate when the feature is enabled",
                [typeof(bool)]),
            new ModPatchInfo(
                "starting_persona_neow_ready_option_release_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),
                "OnRelease",
                typeof(StartingPersonaNeowReadyOptionReleasePatch),
                false,
                "Gameplay patch: intercept the starting-persona ready option before it enters the default Neow event flow"),
            new ModPatchInfo(
                "starting_persona_neow_ready_option_ui_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),
                "_Ready",
                typeof(StartingPersonaNeowReadyOptionUiPatch),
                false,
                "Gameplay patch: make the starting-persona ready option locally non-interactable for clients"),
            new ModPatchInfo(
                "starting_persona_neow_ready_event_room_guard_patch",
                typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NEventRoom),
                "OptionButtonClicked",
                typeof(StartingPersonaNeowReadyEventRoomGuardPatch),
                false,
                "Gameplay patch: swallow any ready-option clicks that leak into the default Neow event room handler",
                [typeof(EventOption), typeof(int)]),
        ]);
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
        TryRegisterNeowDiagnosticsPatches(builder);
        CharacterSelectGameplayPreviewPatchRegistrar.TryRegister(builder);
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

    private static void TryRegisterNeowDiagnosticsPatches(DynamicPatchBuilder builder)
    {
        TryRegisterOptionalReadyPatch(
            builder,
            "MegaCrit.Sts2.Core.Nodes.Rooms.NEventRoom",
            typeof(EventRoomNeowDiagnosticsPatch),
            nameof(EventRoomNeowDiagnosticsPatch.Postfix),
            "event_room_neow_diagnostics_ready",
            "UI patch: capture targeted NEOW event-room diagnostics");

        TryRegisterOptionalReadyPatch(
            builder,
            "MegaCrit.Sts2.Core.Nodes.Events.NAncientEventLayout",
            typeof(AncientEventLayoutNeowDiagnosticsPatch),
            nameof(AncientEventLayoutNeowDiagnosticsPatch.Postfix),
            "ancient_event_layout_neow_diagnostics_ready",
            "UI patch: capture targeted Ancient layout diagnostics");
    }

    private static void TryRegisterOptionalReadyPatch(
        DynamicPatchBuilder builder,
        string typeName,
        Type patchType,
        string patchMethodName,
        string patchId,
        string description)
    {
        var targetType = AccessTools.TypeByName(typeName);
        if (targetType == null)
            return;

        var readyMethod = AccessTools.DeclaredMethod(targetType, "_Ready", Type.EmptyTypes);
        if (readyMethod == null)
            return;

        builder.Add(
            readyMethod,
            postfix: DynamicPatchBuilder.FromMethod(patchType, patchMethodName),
            isCritical: false,
            description: description,
            patchId: patchId);
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
