using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal enum AstralRelicDiagnosticKind
{
    Persona,
    Token
}

internal static class AstralRelicDiagnosticHelper
{
    public static bool TryClassifyAstralRelic(
        RelicModel? relic,
        out AstralNotificationArea area,
        out AstralRelicDiagnosticKind kind,
        out string displayKind)
    {
        area = default;
        kind = default;
        displayKind = string.Empty;

        if (relic == null)
            return false;

        var canonicalRelic = relic.CanonicalInstance ?? relic;
        if (TokenRelicRegistry.IsTokenRelic(canonicalRelic))
        {
            area = AstralNotificationArea.TokenRelic;
            kind = AstralRelicDiagnosticKind.Token;
            displayKind = "筹码";
            return true;
        }

        if (PersonRelicRegistry.IsPersonaRelic(canonicalRelic)
            || PersonRelicRegistry.IsVariantPersonaRelic(canonicalRelic)
            || canonicalRelic.GetType().Name.StartsWith("PersonalityDerivative", StringComparison.Ordinal))
        {
            area = AstralNotificationArea.PersonaRelic;
            kind = AstralRelicDiagnosticKind.Persona;
            displayKind = "人格";
            return true;
        }

        return false;
    }

    public static void ShowObtainFailure(Player owner, RelicModel relic, string stage, int number, Exception ex)
    {
        if (!TryClassifyAstralRelic(relic, out var area, out _, out var displayKind))
            return;

        AstralNotificationService.ShowDiagnosticError(
            AstralNotificationModule.Multiplayer,
            area,
            number,
            $"{displayKind}遗物发放失败：{GetRelicDisplayName(relic)}，玩家 {owner.NetId}。请反馈编号和日志。\n异常：{ex.GetType().Name}",
            stage);
    }

    public static void ShowFallbackWarning(Player owner, RelicModel relic, string stage, int number, string body)
    {
        if (!TryClassifyAstralRelic(relic, out var area, out _, out _))
            return;

        AstralNotificationService.ShowDiagnosticWarning(
            AstralNotificationModule.Multiplayer,
            area,
            number,
            $"{body}\n遗物：{GetRelicDisplayName(relic)}\n玩家：{owner.NetId}",
            stage);
    }

    public static string GetRelicDisplayName(RelicModel relic)
    {
        var canonicalRelic = relic.CanonicalInstance ?? relic;
        var title = canonicalRelic.Title.GetRawText();
        return string.IsNullOrWhiteSpace(title) ? canonicalRelic.Id.Entry : title;
    }
}
