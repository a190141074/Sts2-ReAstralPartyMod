namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class AstralPartyAssetPaths
{
    public const string ModRoot = "res://ReAstralPartyMod";
    public const string ImagesRoot = ModRoot + "/images";
    public const string CardPortraitsRoot = ImagesRoot + "/card_portraits";
    public const string EventsRoot = ImagesRoot + "/events";
    public const string RelicsRoot = ImagesRoot + "/relic";
    public const string PotionsRoot = ImagesRoot + "/potion";
    public const string PowersRoot = ImagesRoot + "/powers";

    public static string EventPortrait(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        return $"{EventsRoot}/{eventId}.png";
    }

    public static string CardPortrait(string cardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardId);
        return $"{CardPortraitsRoot}/{cardId}.png";
    }

    public static string RelicIcon(string relicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relicId);
        return $"{RelicsRoot}/{relicId}.png";
    }

    public static string PotionImage(string potionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(potionId);
        return $"{PotionsRoot}/{potionId}.png";
    }

    public static string PowerIcon(string powerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(powerId);
        return $"{PowersRoot}/{powerId}.png";
    }
}
