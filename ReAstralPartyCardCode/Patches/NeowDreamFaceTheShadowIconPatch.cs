using Godot;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class NeowDreamFaceTheShadowIconPatch
{
    private const string DreamFaceTheShadowTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.DREAM_FACE_THE_SHADOW";

    private const string IconPath = "res://ReAstralPartyMod/images/ancient/dream_face_the_shadow.png";

    public static void Postfix(NEventOptionButton __instance)
    {
        if (!string.Equals(__instance.Option?.TextKey, DreamFaceTheShadowTextKey, StringComparison.OrdinalIgnoreCase))
            return;

        var texture = GD.Load<Texture2D>(IconPath);
        if (texture == null)
        {
            MainFile.Logger.Warn($"[NeowDreamFaceTheShadowIconPatch] Failed to load icon at {IconPath}.");
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

        MainFile.Logger.Info("[NeowDreamFaceTheShadowIconPatch] Applied Dream Face the Shadow icon.");
    }
}
