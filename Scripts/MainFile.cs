// 在您的MainFile.cs中

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace AstralPartyMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "AstralPartyMod";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        Logger.Info("AstralPartyMod initialized");
    }
}