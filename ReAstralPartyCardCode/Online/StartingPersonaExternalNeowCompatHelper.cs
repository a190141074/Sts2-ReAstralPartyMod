using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class StartingPersonaExternalNeowCompatHelper
{
    private const string YuWanCardModId = "YuWanCard";
    private const string RingOfTheSevenCursesModId = "RingOfTheSevenCurses";

    private static readonly string[] SevenCursesOptionMarkers =
    [
        "SEVEN_CURSES",
        "RING_OF_SEVEN_CURSES"
    ];

    internal enum CachedNeowPageKind
    {
        NormalNeowPage = 0,
        ExternalSevenCursesPage = 1
    }

    public static CachedNeowPageKind ResolveCachedPageKind(Neow neow)
    {
        ArgumentNullException.ThrowIfNull(neow);

        if (!IsAnySupportedExternalModLoaded())
            return CachedNeowPageKind.NormalNeowPage;

        return LooksLikeExternalSevenCursesPage(neow.CurrentOptions)
            ? CachedNeowPageKind.ExternalSevenCursesPage
            : CachedNeowPageKind.NormalNeowPage;
    }

    public static string DescribePageKind(CachedNeowPageKind pageKind)
    {
        return pageKind switch
        {
            CachedNeowPageKind.ExternalSevenCursesPage => "external_seven_curses_page",
            _ => "normal_neow_page"
        };
    }

    public static string DescribeDetectedSource()
    {
        var loadedSources = new List<string>(2);
        if (OptionalModCompatRegistry.IsModLoaded(YuWanCardModId))
            loadedSources.Add(YuWanCardModId);
        if (OptionalModCompatRegistry.IsModLoaded(RingOfTheSevenCursesModId))
            loadedSources.Add(RingOfTheSevenCursesModId);

        return loadedSources.Count == 0 ? "none" : string.Join("+", loadedSources);
    }

    private static bool IsAnySupportedExternalModLoaded()
    {
        return OptionalModCompatRegistry.IsModLoaded(YuWanCardModId)
               || OptionalModCompatRegistry.IsModLoaded(RingOfTheSevenCursesModId);
    }

    private static bool LooksLikeExternalSevenCursesPage(IEnumerable<EventOption>? options)
    {
        if (options == null)
            return false;

        foreach (var option in options)
        {
            var textKey = option.TextKey;
            if (string.IsNullOrWhiteSpace(textKey))
                continue;

            foreach (var marker in SevenCursesOptionMarkers)
            {
                if (textKey.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
