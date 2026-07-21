using System.Diagnostics.CodeAnalysis;

namespace InstaDM.Core.Navigation;

/// <summary>
/// Canonicalizes URLs before any policy decision. Deliberately strict —
/// everything unusual fails canonicalization and is therefore blocked.
///
/// Rules (each one closes a real bypass class):
/// <list type="bullet">
/// <item>https only — http, javascript:, data:, file:, custom schemes all fail.</item>
/// <item>Host lowercased (mixed-case hosts judged inconsistently was a
/// verified defect in the macOS source) and a single trailing dot removed
/// (`instagram.com.` is DNS-identical to `instagram.com`).</item>
/// <item>Non-ASCII or punycode (`xn--`) host labels fail — Instagram's real
/// hosts are plain ASCII; Unicode lookalikes are treated as hostile.</item>
/// <item>Userinfo fails — `https://www.instagram.com@evil.com/` must never
/// be judged by the part before the `@`.</item>
/// <item>Only the default port (443) is accepted.</item>
/// <item>Dot segments (literal or %2e-encoded) are resolved by
/// <see cref="Uri"/> per RFC 3986 before the path is read — the same
/// resolution a browser applies — so the policy judges the page that would
/// actually load. Any dot segment that somehow survives resolution fails.</item>
/// <item>Paths still containing percent-escapes or backslashes after
/// resolution fail — encoded slashes exist only to confuse prefix matchers,
/// and no legitimate Instagram messaging/auth path needs them.</item>
/// </list>
/// </summary>
public static class UrlCanonicalizer
{
    public static bool TryCanonicalize(string? rawUrl, [NotNullWhen(true)] out CanonicalUrl? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        if (!uri.IsDefaultPort && uri.Port != 443)
        {
            return false;
        }

        var host = uri.Host;
        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        host = host.ToLowerInvariant().TrimEnd('.');
        if (host.Length == 0 || !IsPlainAsciiHost(host))
        {
            return false;
        }

        // AbsolutePath keeps %2F etc. escaped rather than decoding them into
        // path separators — exactly why we reject any remaining '%'.
        var path = uri.AbsolutePath;
        if (path.Length == 0)
        {
            path = "/";
        }

        if (path.Contains('%') || path.Contains('\\'))
        {
            return false;
        }

        if (HasDotSegments(path))
        {
            return false;
        }

        result = new CanonicalUrl { Host = host, Path = path };
        return true;
    }

    private static bool IsPlainAsciiHost(string host)
    {
        foreach (var label in host.Split('.'))
        {
            if (label.Length == 0)
            {
                return false; // empty label => malformed (e.g. "a..b")
            }
            if (label.StartsWith("xn--", StringComparison.Ordinal))
            {
                return false; // punycode lookalike
            }
        }

        foreach (var c in host)
        {
            var ok = c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '.';
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasDotSegments(string path)
    {
        foreach (var segment in path.Split('/'))
        {
            if (segment is "." or "..")
            {
                return true;
            }
        }

        return false;
    }
}
