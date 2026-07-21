namespace InstaDM.Core.Navigation;

/// <summary>
/// Directory-boundary path matching — the single matcher semantic shared by
/// the native policy and the generated JavaScript guard.
///
/// A prefix matches only on a directory boundary: exactly equal, or followed
/// by '/'. Plain "starts with" would let `/p` match `/profile/` and
/// `/privacy/` (surfaces this app exists to hide), `/direct` match
/// `/directory`, and `/api` match `/api-status`. The macOS source fixed this
/// for most lists but left two matchers unanchored (audit findings L1/L3);
/// here there is exactly one implementation, mirrored verbatim in JS by
/// <see cref="PolicyScriptBuilder"/>.
/// </summary>
public static class PathMatcher
{
    public static bool MatchesAny(string path, IReadOnlyList<string> prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (Matches(path, prefix))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Matches(string path, string prefix)
    {
        if (path.Length < prefix.Length)
        {
            return false;
        }

        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return path.Length == prefix.Length || path[prefix.Length] == '/';
    }
}
