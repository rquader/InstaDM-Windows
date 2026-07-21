namespace InstaDM.Core.Settings;

/// <summary>Native chrome appearance. Never forced into Instagram's page.</summary>
public enum AppearancePreference
{
    System = 0,
    Light = 1,
    Dark = 2,
}

/// <summary>Privacy-first notification intensity (M10 consumes this).</summary>
public enum NotificationLevel
{
    Off = 0,
    Badge = 1,
    Standard = 2,
}

/// <summary>
/// All app-owned persisted preferences. Deliberately excludes credentials,
/// cookies, message content, usernames, thread ids, and browsing history.
/// </summary>
public sealed class AppSettings
{
    public const int DefaultPollIntervalSeconds = 30;
    public static readonly int[] AllowedPollIntervals = [15, 30, 60, 120];

    /// <summary>Sage accent inspired by the macOS app / App.xaml (#6B8C5F).</summary>
    public const string SageAccentHex = "#6B8C5F";

    public AppearancePreference Appearance { get; set; } = AppearancePreference.System;

    public NotificationLevel NotificationLevel { get; set; } = NotificationLevel.Standard;

    public int PollIntervalSeconds { get; set; } = DefaultPollIntervalSeconds;

    /// <summary>Opt-in Follow Requests surface. Default off.</summary>
    public bool FollowRequestsEnabled { get; set; }

    /// <summary>Open non-Instagram http(s) links in the system browser after
    /// confirmation. Default <c>false</c> until the handoff is implemented
    /// without transferring cookies, headers, or referrer. When false,
    /// unexpected destinations are dropped.</summary>
    public bool OpenLinksInExternalBrowser { get; set; }

    /// <summary>Clamps poll interval to the allowed set; unknown values become
    /// the default. Used on load and before save.</summary>
    public void Normalize()
    {
        if (Array.IndexOf(AllowedPollIntervals, PollIntervalSeconds) < 0)
        {
            PollIntervalSeconds = DefaultPollIntervalSeconds;
        }

        if (!Enum.IsDefined(Appearance))
        {
            Appearance = AppearancePreference.System;
        }

        if (!Enum.IsDefined(NotificationLevel))
        {
            NotificationLevel = NotificationLevel.Standard;
        }
    }

    public AppSettings Clone() => new()
    {
        Appearance = Appearance,
        NotificationLevel = NotificationLevel,
        PollIntervalSeconds = PollIntervalSeconds,
        FollowRequestsEnabled = FollowRequestsEnabled,
        OpenLinksInExternalBrowser = OpenLinksInExternalBrowser,
    };
}
