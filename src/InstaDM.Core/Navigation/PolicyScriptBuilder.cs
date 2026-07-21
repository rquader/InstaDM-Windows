using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstaDM.Core.Navigation;

/// <summary>
/// Generates the JSON policy payload the document-start containment guard
/// consumes. The guard ships as a static JS file with a
/// <c>__INSTADM_POLICY__</c> placeholder; at WebView initialization the host
/// replaces the placeholder with this payload. That makes the C# policy the
/// only authored source — the JS side has data, not a second hand-written
/// allowlist (the macOS app maintained both by hand and they drifted;
/// docs/DECISIONS.md ADR-004).
///
/// The payload contains only static path prefixes and hostnames — no user
/// data, no session state, nothing sensitive.
/// </summary>
public static class PolicyScriptBuilder
{
    public const string PolicyPlaceholder = "__INSTADM_POLICY__";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>The wire shape consumed by containment-guard.js. Keep in
    /// lockstep with the guard's `policy` usage; tests assert the exact keys.</summary>
    public sealed record PolicyPayload
    {
        [JsonPropertyName("version")]
        public required int Version { get; init; }

        [JsonPropertyName("allowedHosts")]
        public required IReadOnlyList<string> AllowedHosts { get; init; }

        [JsonPropertyName("authOnlyHost")]
        public required string AuthOnlyHost { get; init; }

        [JsonPropertyName("dmPrefixes")]
        public required IReadOnlyList<string> DmPrefixes { get; init; }

        [JsonPropertyName("authPrefixes")]
        public required IReadOnlyList<string> AuthPrefixes { get; init; }

        [JsonPropertyName("optionalPrefixes")]
        public required IReadOnlyList<string> OptionalPrefixes { get; init; }

        [JsonPropertyName("inboxUrl")]
        public required string InboxUrl { get; init; }
    }

    /// <summary>Current wire version. Bump when the payload shape changes so
    /// a stale cached guard can fail closed instead of misreading data.</summary>
    public const int PayloadVersion = 1;

    public static PolicyPayload BuildPayload(NavigationPolicy policy)
    {
        var optional = new List<string>();
        if (policy.Options.FollowRequestsEnabled)
        {
            optional.AddRange(NavigationPolicy.FollowRequestsPathPrefixes);
        }

        // SharedPosts prefixes are deliberately NOT exported to the guard:
        // the guard cannot verify source-gating (click origin) reliably, so
        // shared-post clicks always go to the native layer for the decision.
        // Fail closed in JS; the native policy grants the exception.

        return new PolicyPayload
        {
            Version = PayloadVersion,
            AllowedHosts = NavigationPolicy.AllowedHosts,
            AuthOnlyHost = NavigationPolicy.AuthOnlyHost,
            DmPrefixes = NavigationPolicy.DirectMessagingPathPrefixes,
            AuthPrefixes = NavigationPolicy.AuthSurfacePathPrefixes,
            OptionalPrefixes = optional,
            InboxUrl = NavigationPolicy.InboxUrl,
        };
    }

    public static string BuildPayloadJson(NavigationPolicy policy) =>
        JsonSerializer.Serialize(BuildPayload(policy), SerializerOptions);

    /// <summary>Splices the payload into the guard script template.
    /// Throws if the placeholder is missing — a silent no-op here would ship
    /// a guard with no policy.</summary>
    public static string InjectIntoScript(string guardScriptTemplate, NavigationPolicy policy)
    {
        if (!guardScriptTemplate.Contains(PolicyPlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Guard script template does not contain the '{PolicyPlaceholder}' placeholder.");
        }

        return guardScriptTemplate.Replace(
            PolicyPlaceholder, BuildPayloadJson(policy), StringComparison.Ordinal);
    }
}
