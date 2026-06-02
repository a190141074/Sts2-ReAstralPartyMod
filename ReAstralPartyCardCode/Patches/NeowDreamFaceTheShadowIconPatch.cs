using Godot;
using MegaCrit.Sts2.Core.Nodes.Events;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class NeowDreamFaceTheShadowIconPatch
{
    private const string StartingPersonaReadyTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.STARTING_PERSONA_READY.options.READY";

    private const string StartingPersonaReadyIconPath = "res://ReAstralPartyMod/images/ancient/starting_persona_ready.png";

    public static void Postfix(NEventOptionButton __instance)
    {
        var textKey = __instance.Option?.TextKey;
        var iconPath = ResolveIconPath(textKey);
        if (iconPath == null)
            return;

        var texture = GD.Load<Texture2D>(iconPath);
        if (texture == null)
        {
            MainFile.Logger.Warn($"[NeowDreamFaceTheShadowIconPatch] Failed to load icon at {iconPath}.");
            return;
        }

        var icon = __instance.GetNodeOrNull<TextureRect>("%RelicIcon");
        if (icon == null)
        {
            MainFile.Logger.Warn("[NeowDreamFaceTheShadowIconPatch] %RelicIcon node was not found.");
            return;
        }

        icon.Texture = texture;
        icon.Visible = true;

        var outline = icon.GetNodeOrNull<TextureRect>("%Outline");
        if (outline != null)
            outline.Texture = texture;

        MainFile.Logger.Info($"[NeowDreamFaceTheShadowIconPatch] Applied Neow option icon for {textKey}.");
    }

    private static string? ResolveIconPath(string? textKey)
    {
        var customIconPath = NeowOptionInjectionHelper.ResolveCustomNeowOptionIconPath(textKey);
        if (customIconPath != null)
            return customIconPath;

        if (string.Equals(textKey, StartingPersonaReadyTextKey, StringComparison.OrdinalIgnoreCase))
            return StartingPersonaReadyIconPath;

        return null;
    }
}
