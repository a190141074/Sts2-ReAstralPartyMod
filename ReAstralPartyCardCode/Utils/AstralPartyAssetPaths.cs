namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class AstralPartyAssetPaths
{
    public const string ModRoot = "res://ReAstralPartyMod";
    public const string ImagesRoot = ModRoot + "/images";
    public const string EventsRoot = ImagesRoot + "/events";

    public static string EventPortrait(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        return $"{EventsRoot}/{eventId}.png";
    }
}
