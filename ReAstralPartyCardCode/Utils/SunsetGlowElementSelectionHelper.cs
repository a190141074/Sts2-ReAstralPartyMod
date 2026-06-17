using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class SunsetGlowElementSelectionHelper
{
    public const string TsunamiBranch = "tsunami";
    public const string SettingSunBranch = "setting_sun";
    public const string ThundersBreathBranch = "thunders_breath";

    private const string ChoiceContextVersion = "v1";
    private const string TsunamiIconPath = "res://ReAstralPartyMod/images/powers/tsunami_power.png";
    private const string SettingSunIconPath = "res://ReAstralPartyMod/images/powers/setting_sun_power.png";
    private const string ThundersBreathIconPath = "res://ReAstralPartyMod/images/powers/thunders_breath_power.png";
    private static readonly Color TsunamiColor = new(0.28f, 0.78f, 1f, 1f);
    private static readonly Color SettingSunColor = new(1f, 0.42f, 0.24f, 1f);
    private static readonly Color ThundersBreathColor = new(0.88f, 0.76f, 0.2f, 1f);

    public static async Task<string?> SelectBranchAsync(
        Player owner,
        string title,
        string subtitle,
        IReadOnlyList<SunsetGlowElementSelectionOption> options,
        string context)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Count == 0)
            return null;

        var runManager = RunManager.Instance;
        var gameType = runManager.NetService.Type;
        if (gameType is NetGameType.Singleplayer or NetGameType.None)
            return (await ShowLocalSelectionAsync(owner, title, subtitle, options))?.BranchId;

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
            return (await ShowLocalSelectionAsync(owner, title, subtitle, options))?.BranchId;

        var choiceId = synchronizer.ReserveChoiceId(owner);
        var sessionKey = $"{AstralChoiceKind.SunsetGlowElementSelection}|{context}|{owner.NetId}|{ChoiceContextVersion}";
        if (owner.NetId == runManager.NetService.NetId)
        {
            var localResult = await ShowLocalSelectionAsync(owner, title, subtitle, options);
            var selectedIndex = localResult?.SelectedIndex ?? -1;
            synchronizer.SyncLocalChoice(
                owner,
                choiceId,
                AstralChoiceProtocol.CreateIndexedEnvelope(
                    AstralChoiceKind.SunsetGlowElementSelection,
                    owner.RunState as RunState,
                    sessionKey,
                    0,
                    [selectedIndex]));
            return selectedIndex >= 0 && selectedIndex < options.Count
                ? options[selectedIndex].BranchId
                : null;
        }

        var remoteChoice = await DeterministicMultiplayerChoiceHelper.WaitForRemoteIndexedEnvelope(
            synchronizer,
            owner,
            choiceId,
            AstralChoiceKind.SunsetGlowElementSelection,
            owner.RunState as RunState,
            sessionKey,
            context);
        var payload = remoteChoice?.Payload ?? [];
        var remoteIndex = payload.Count > 0 ? payload[0] : -1;
        return remoteIndex >= 0 && remoteIndex < options.Count
            ? options[remoteIndex].BranchId
            : null;
    }

    public static IReadOnlyList<SunsetGlowElementSelectionOption> BuildOptions(
        string tsunamiTitle,
        string tsunamiDescription,
        string settingSunTitle,
        string settingSunDescription,
        string thundersBreathTitle,
        string thundersBreathDescription,
        int cycleStage)
    {
        return
        [
            new SunsetGlowElementSelectionOption(TsunamiBranch, tsunamiTitle, tsunamiDescription, TsunamiIconPath, TsunamiColor),
            new SunsetGlowElementSelectionOption(SettingSunBranch, settingSunTitle, settingSunDescription, SettingSunIconPath, SettingSunColor),
            new SunsetGlowElementSelectionOption(ThundersBreathBranch, thundersBreathTitle, thundersBreathDescription, ThundersBreathIconPath, ThundersBreathColor)
        ];
    }

    private static async Task<SunsetGlowElementSelectionResult?> ShowLocalSelectionAsync(
        Player owner,
        string title,
        string subtitle,
        IReadOnlyList<SunsetGlowElementSelectionOption> options)
    {
        var overlayStack = await WaitForOverlayStackAsync();
        if (overlayStack == null)
            return options.Count > 0 ? new SunsetGlowElementSelectionResult(options[0].BranchId, 0, options) : null;

        var screen = SunsetGlowElementSelectionScreen.Create(owner, options, title, subtitle);
        overlayStack.Push(screen);
        var result = await screen.WaitForResult();
        screen.Close();
        await screen.WaitUntilClosedAsync();
        await WaitForOverlaySettleFramesAsync(2);
        return result;
    }

    private static async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync(RunManager runManager)
    {
        for (var index = 0; index < 60; index++)
        {
            if (runManager.PlayerChoiceSynchronizer != null)
                return runManager.PlayerChoiceSynchronizer;

            await Task.Yield();
        }

        return runManager.PlayerChoiceSynchronizer;
    }

    private static async Task<NOverlayStack?> WaitForOverlayStackAsync()
    {
        for (var index = 0; index < 60; index++)
        {
            if (NOverlayStack.Instance != null)
                return NOverlayStack.Instance;

            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        }

        return NOverlayStack.Instance;
    }

    private static async Task WaitForOverlaySettleFramesAsync(int frames)
    {
        for (var index = 0; index < frames; index++)
        {
            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        }
    }
}
