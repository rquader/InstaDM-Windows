namespace InstaDM.Core.WebHost;

/// <summary>
/// The complete, data-only description of how the embedded WebView2 runtime
/// must be configured. Kept in the platform-neutral core so every value is
/// unit-testable on any host; the WinUI layer applies these values verbatim
/// and adds no judgment of its own. Each setting implements
/// docs/DECISIONS.md ADR-006 (threat-model gates G1/G2).
/// </summary>
public sealed record WebViewHostConfiguration
{
    /// <summary>Dedicated, local, non-roaming, app-exclusive user-data
    /// folder. Never a shared/browser profile.</summary>
    public required string UserDataFolder { get; init; }

    /// <summary>Environment-level Chromium switches. SmartScreen is disabled
    /// here (not only via Settings) because environment arguments cannot be
    /// re-enabled at runtime. The service-hardening flags are candidates to
    /// be verified empirically in the M11 audit.</summary>
    public required string AdditionalBrowserArguments { get; init; }

    /// <summary>DevTools: Debug builds only. Never true in Release.</summary>
    public required bool AreDevToolsEnabled { get; init; }

    // ---- invariant values (properties so the host applies them by name,
    // ---- and tests can assert nobody "temporarily" flipped one) ----

    /// <summary>Reputation checking (SmartScreen) off — URL reporting to
    /// Microsoft violates the invariant; the policy layer already restricts
    /// navigation to Instagram.</summary>
    public bool IsReputationCheckingRequired => false;

    /// <summary>Custom crash reporting ON means dumps stay local instead of
    /// being sent to Microsoft.</summary>
    public bool IsCustomCrashReportingEnabled => true;

    /// <summary>Credentials belong to the user and Instagram's page only.</summary>
    public bool IsPasswordAutosaveEnabled => false;

    public bool IsGeneralAutofillEnabled => false;

    public bool AreBrowserExtensionsEnabled => false;

    public bool AllowSingleSignOnUsingOSPrimaryAccount => false;

    /// <summary>Tracking prevention stays on (privacy-positive, local);
    /// audit verifies it makes no external calls.</summary>
    public bool IsTrackingPreventionEnabled => true;

    /// <summary>Status bar shows target URLs on hover — harmless, but the
    /// focused shell keeps chrome minimal.</summary>
    public bool IsStatusBarEnabled => false;

    /// <summary>The browser's own accelerators (print, save-page, open-file)
    /// escape the messaging shell; disabled.</summary>
    public bool AreBrowserAcceleratorKeysEnabled => false;

    /// <summary>Fixed Chromium switch set from ADR-006. Order is stable so
    /// the value can be asserted verbatim.</summary>
    public const string BrowserArguments =
        "--disable-features=msSmartScreenProtection " +
        "--disable-domain-reliability " +
        "--disable-background-networking " +
        "--disable-component-update";

    /// <summary>Builds the canonical configuration.</summary>
    /// <param name="localAppDataRoot">Resolved LocalApplicationData (never
    /// Roaming) directory; the host passes
    /// Environment.SpecialFolder.LocalApplicationData.</param>
    /// <param name="isDebugBuild">Compile-time flag from the host.</param>
    public static WebViewHostConfiguration Create(string localAppDataRoot, bool isDebugBuild)
    {
        if (string.IsNullOrWhiteSpace(localAppDataRoot))
        {
            throw new ArgumentException(
                "A local (non-roaming) app-data root is required.", nameof(localAppDataRoot));
        }

        return new WebViewHostConfiguration
        {
            UserDataFolder = Path.Combine(localAppDataRoot, "InstaDM", "WebView2"),
            AdditionalBrowserArguments = BrowserArguments,
            AreDevToolsEnabled = isDebugBuild,
        };
    }
}
