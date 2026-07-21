using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace InstaDM.Core.WebHost;

/// <summary>
/// A schema-validated message from the containment guard. The bridge accepts
/// EXACTLY <c>{"v":1,"source":"instadm-guard","kind":…,"surface":…}</c> with
/// enum-like string values; anything else — extra keys, wrong types, unknown
/// values, oversized payloads — is rejected without partial parsing. Page
/// script is untrusted: Instagram's own code (or anything injected into it)
/// can call window.chrome.webview.postMessage too, so this parser is a
/// security boundary, not a convenience.
/// </summary>
public sealed record GuardMessage(GuardMessageKind Kind, GuardReportedSurface Surface)
{
    public const int SchemaVersion = 1;
    public const string ExpectedSource = "instadm-guard";

    /// <summary>Defensive cap; genuine guard messages are ~90 chars.</summary>
    public const int MaxLength = 256;

    public static bool TryParse(string? json, [NotNullWhen(true)] out GuardMessage? message)
    {
        message = null;
        if (string.IsNullOrEmpty(json) || json.Length > MaxLength)
        {
            return false;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            int propertyCount = 0;
            foreach (var _ in root.EnumerateObject()) { propertyCount++; }
            if (propertyCount != 4)
            {
                return false;
            }

            if (!root.TryGetProperty("v", out var v) ||
                v.ValueKind != JsonValueKind.Number ||
                !v.TryGetInt32(out var version) || version != SchemaVersion)
            {
                return false;
            }

            if (!root.TryGetProperty("source", out var source) ||
                source.ValueKind != JsonValueKind.String ||
                source.GetString() != ExpectedSource)
            {
                return false;
            }

            if (!root.TryGetProperty("kind", out var kindElement) ||
                kindElement.ValueKind != JsonValueKind.String ||
                !TryParseKind(kindElement.GetString(), out var kind))
            {
                return false;
            }

            if (!root.TryGetProperty("surface", out var surfaceElement) ||
                surfaceElement.ValueKind != JsonValueKind.String ||
                !TryParseSurface(surfaceElement.GetString(), out var surface))
            {
                return false;
            }

            message = new GuardMessage(kind, surface);
            return true;
        }
    }

    private static bool TryParseKind(string? value, out GuardMessageKind kind)
    {
        kind = value switch
        {
            "blockedClick" => GuardMessageKind.BlockedClick,
            "blockedHistory" => GuardMessageKind.BlockedHistory,
            "guardInactive" => GuardMessageKind.GuardInactive,
            _ => (GuardMessageKind)(-1),
        };
        return (int)kind >= 0;
    }

    private static bool TryParseSurface(string? value, out GuardReportedSurface surface)
    {
        surface = value switch
        {
            "malformed" => GuardReportedSurface.Malformed,
            "offPlatform" => GuardReportedSurface.OffPlatform,
            "feed" => GuardReportedSurface.Feed,
            "explore" => GuardReportedSurface.Explore,
            "reels" => GuardReportedSurface.Reels,
            "stories" => GuardReportedSurface.Stories,
            "post" => GuardReportedSurface.Post,
            "directShell" => GuardReportedSurface.DirectShell,
            "other" => GuardReportedSurface.Other,
            _ => (GuardReportedSurface)(-1),
        };
        return (int)surface >= 0;
    }
}

/// <summary>What the guard did. Mirrors the `kind` strings in containment-guard.js.</summary>
public enum GuardMessageKind
{
    BlockedClick,
    BlockedHistory,
    GuardInactive,
}

/// <summary>Coarse surface category reported by the guard. Mirrors the
/// `surface` strings in containment-guard.js. Deliberately narrower than
/// <see cref="Navigation.InstagramSurface"/> — the guard only knows what it
/// needs to report, never URLs.</summary>
public enum GuardReportedSurface
{
    Malformed,
    OffPlatform,
    Feed,
    Explore,
    Reels,
    Stories,
    Post,
    DirectShell,
    Other,
}
