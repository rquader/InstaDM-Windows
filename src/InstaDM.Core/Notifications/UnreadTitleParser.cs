using InstaDM.Core.Settings;

namespace InstaDM.Core.Notifications;

/// <summary>
/// Parses Instagram web document titles for an unread count. Never inspects
/// DOM message content. Returns null when the title is unrecognized so the
/// state machine leaves the previous count alone (prevents false-zero badge
/// collapse — docs/SOURCE_BEHAVIOR.md B10).
/// </summary>
public static class UnreadTitleParser
{
    /// <summary>Known forms: "(N) Inbox • Instagram", "(N) Instagram".
    /// Comma-grouped thousands are accepted after stripping separators.
    /// A leading '(' that is not a valid count is treated as unrecognized
    /// (nil), never as zero — macOS defect L9.</summary>
    public static int? TryParse(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var trimmed = title.Trim();
        if (trimmed.StartsWith('('))
        {
            var close = trimmed.IndexOf(')');
            if (close <= 1)
            {
                return null; // "(3 new…" without ')' → nil, not 0
            }

            var inner = trimmed[1..close].Replace(",", "", StringComparison.Ordinal)
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace(".", "", StringComparison.Ordinal);
            if (!int.TryParse(inner, out var count) || count < 0)
            {
                return null;
            }

            return count;
        }

        // Title mentions Instagram but no count → treat as zero unread.
        if (trimmed.Contains("Instagram", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return null;
    }
}

/// <summary>Privacy-safe notification decision — generic text only.</summary>
public enum UnreadNotificationAction
{
    None,
    /// <summary>Update local badge/indicator only.</summary>
    UpdateBadge,
    /// <summary>Show a generic banner (no names/previews) and update badge.</summary>
    ShowGenericBanner,
}

public sealed record UnreadDecision(UnreadNotificationAction Action, int? BadgeCount, int? AddedCount);

/// <summary>
/// Baseline/increase unread state machine. Baseline is nil until the first
/// successful parse after attach or re-enable, so pre-existing unreads never
/// fire a false "new message" banner.
/// </summary>
public sealed class UnreadStateMachine
{
    private int? _baseline;
    private int? _lastCount;
    private bool _suppressBanners;

    public int? Baseline => _baseline;
    public int? LastCount => _lastCount;

    /// <summary>Call when notifications are enabled or the watcher attaches.</summary>
    public void ResetBaseline()
    {
        _baseline = null;
        _lastCount = null;
    }

    /// <summary>Suppress banners while the app is foreground with Messages
    /// visible; badge updates still apply when the level allows.</summary>
    public void SetBannerSuppressed(bool suppressed) => _suppressBanners = suppressed;

    public UnreadDecision Observe(int? parsedCount, NotificationLevel level)
    {
        if (level == NotificationLevel.Off)
        {
            return new UnreadDecision(UnreadNotificationAction.None, null, null);
        }

        if (parsedCount is null)
        {
            return new UnreadDecision(UnreadNotificationAction.None, _lastCount, null);
        }

        var count = parsedCount.Value;
        if (_baseline is null)
        {
            _baseline = count;
            _lastCount = count;
            return new UnreadDecision(UnreadNotificationAction.UpdateBadge, count, null);
        }

        var added = count - _baseline.Value;
        _lastCount = count;

        if (added <= 0)
        {
            // Count dropped or unchanged: move baseline down so a later
            // increase from the new floor still notifies.
            if (count < _baseline.Value)
            {
                _baseline = count;
            }

            return new UnreadDecision(UnreadNotificationAction.UpdateBadge, count, added);
        }

        // Positive increase from baseline.
        _baseline = count;
        if (level == NotificationLevel.Badge || _suppressBanners)
        {
            return new UnreadDecision(UnreadNotificationAction.UpdateBadge, count, added);
        }

        return new UnreadDecision(UnreadNotificationAction.ShowGenericBanner, count, added);
    }
}
