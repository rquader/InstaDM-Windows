namespace InstaDM.Core.Navigation;

/// <summary>
/// A URL that survived <see cref="UrlCanonicalizer"/>: https-only, a
/// normalized lowercase host, no userinfo smuggling, default port, and a
/// path with no percent-escapes or backslashes. Anything that could not be
/// canonicalized never becomes a <see cref="CanonicalUrl"/> — callers treat
/// that as <see cref="InstagramSurface.Malformed"/> and fail closed.
/// </summary>
public sealed record CanonicalUrl
{
    /// <summary>Lowercase host with any single trailing dot removed.</summary>
    public required string Host { get; init; }

    /// <summary>The absolute path, always starting with '/'. Case is preserved
    /// (Instagram paths are lowercase; anything else fails path matching and
    /// is blocked, which is the conservative outcome).</summary>
    public required string Path { get; init; }
}
