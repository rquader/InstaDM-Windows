# InstaDM for Windows — static privacy scan
# Fails the build when a known privacy hazard appears in the source tree.
# This scanner is a tripwire, not a proof: runtime behavior is verified by
# the M11 network audit (docs/NETWORK_AUDIT.md). Keep patterns tight enough
# to avoid false confidence; document gaps in docs/TEST_MATRIX.md §6.

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$failures = @()

# Source files to scan (never scan ignored/runtime folders).
$sourceFiles = Get-ChildItem -Path (Join-Path $repoRoot 'src'), (Join-Path $repoRoot 'tests'), (Join-Path $repoRoot '.github') -Recurse -File -Include *.cs, *.xaml, *.csproj, *.js, *.css, *.yml, *.yaml, *.json -ErrorAction SilentlyContinue

# 1. Forbidden telemetry / analytics / crash-reporting packages.
$forbiddenPackages = @(
    'Microsoft.ApplicationInsights', 'Sentry', 'Bugsnag', 'Raygun',
    'Firebase', 'Amplitude', 'Mixpanel', 'Segment.', 'AppCenter',
    'Countly', 'PostHog', 'RudderStack', 'NewRelic', 'Datadog'
)
foreach ($file in ($sourceFiles | Where-Object Extension -eq '.csproj')) {
    $content = Get-Content $file.FullName -Raw
    foreach ($pkg in $forbiddenPackages) {
        if ($content -match [regex]::Escape($pkg)) {
            $failures += "Forbidden package '$pkg' referenced in $($file.FullName)"
        }
    }
}

# 2. Sensitive-value logging patterns in C# / JS.
#    Cookie values, session ids, auth headers, or bodies must never be logged.
#    Also forbid reading cookie.Value anywhere in production sources.
$logPatterns = @(
    'sessionid\s*[=:].*(Log|Console|Debug|Trace|NSLog|print)',
    '(Log|Console\.Write|Debug\.Write|Trace\.Write).*\b(cookie\.Value|CookieValue|Authorization|csrftoken)\b',
    'console\.log\([^)]*document\.cookie',
    '\.Value\b.*\b(sessionid|csrftoken)\b'
)
foreach ($file in ($sourceFiles | Where-Object { $_.Extension -in '.cs', '.js' })) {
    $content = Get-Content $file.FullName -Raw
    foreach ($pattern in $logPatterns) {
        if ($content -match $pattern) {
            $failures += "Sensitive logging pattern '$pattern' in $($file.FullName)"
        }
    }
}

# 2b. Production sources must never read cookie.Value (existence checks use Name only).
foreach ($file in ($sourceFiles | Where-Object {
    $_.Extension -eq '.cs' -and $_.FullName -match '[\\/]src[\\/]'
})) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match 'cookie\.Value|CookieValue') {
        $failures += "Cookie value access in production source $($file.FullName)"
    }
}

# 3. Hardcoded remote endpoints outside the approved set.
#    Approved: instagram.com / cdninstagram.com / fbcdn.net / facebook.com
#    (first-party Instagram/Meta), plus schema/xmlns/docs URLs.
$urlRegex = 'https?://[a-zA-Z0-9.-]+'
$approvedHosts = @(
    'www.instagram.com', 'instagram.com', 'accounts.instagram.com',
    'schemas.microsoft.com', 'schemas.openxmlformats.org', 'www.w3.org',
    'github.com', 'learn.microsoft.com', 'dot.net', 'aka.ms',
    'localhost', '127.0.0.1'
)
# Endpoint scan covers production sources only. Adversarial fixtures under
# tests/ intentionally contain lookalike/evil hosts and must not trip this.
$endpointScanFiles = $sourceFiles | Where-Object {
    $_.Extension -in '.cs', '.js', '.xaml' -and
    $_.FullName -notmatch '[\\/](tests|Fixtures)[\\/]'
}
foreach ($file in $endpointScanFiles) {
    $content = Get-Content $file.FullName -Raw
    foreach ($match in [regex]::Matches($content, $urlRegex)) {
        $url = $match.Value
        $urlHost = ($url -replace '^https?://', '') -replace '/.*$', ''
        $ok = $false
        foreach ($approved in $approvedHosts) {
            if ($urlHost -eq $approved -or $urlHost.EndsWith('.instagram.com') -or
                $urlHost.EndsWith('.cdninstagram.com') -or $urlHost.EndsWith('.fbcdn.net') -or
                $urlHost.EndsWith('.facebook.com')) { $ok = $true; break }
        }
        if (-not $ok) {
            $failures += "Unapproved endpoint '$url' in $($file.FullName)"
        }
    }
}

# 4. Remote debugging must never appear outside a DEBUG-only context.
foreach ($file in ($sourceFiles | Where-Object Extension -eq '.cs')) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match 'remote-debugging-port') {
        $failures += "remote-debugging-port referenced in $($file.FullName) — forbidden in all configurations"
    }
}

# 5. Accidentally staged sensitive files (checks the git index when available).
$stagedCheck = git -C $repoRoot ls-files 2>$null
if ($LASTEXITCODE -eq 0 -and $stagedCheck) {
    $forbiddenTracked = $stagedCheck | Where-Object {
        $_ -match '\.instadm-local\.env$' -or $_ -match '\.(pcap|pcapng|har|dmp|etl|pfx|p12|snk|key)$' -or
        $_ -match '(^|/)(EBWebView|WebView2Data|UserDataFolder|webview-user-data)(/|$)'
    }
    foreach ($f in $forbiddenTracked) {
        $failures += "Sensitive file tracked by git: $f"
    }
}

if ($failures.Count -gt 0) {
    Write-Host "PRIVACY SCAN FAILED:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Privacy scan passed: no forbidden packages, endpoints, logging patterns, or staged sensitive files."
exit 0
