using InstaDM.Core.Authentication;
using Microsoft.UI.Dispatching;
using Microsoft.Web.WebView2.Core;

namespace InstaDM.App.Services;

/// <summary>
/// The one place in the entire application that touches cookies. It answers
/// exactly one question — does the Instagram session cookie exist — and by
/// construction cannot answer any other: the cookie list is enumerated for
/// the name only, and no value, domain detail, or expiry ever leaves this
/// method. Never add logging here.
/// </summary>
public sealed class WebViewSessionCookieProbe : ISessionCookieProbe
{
    /// <summary>Instagram's session cookie name (present only while a
    /// session is valid; the web runtime owns its storage and encryption).</summary>
    private const string SessionCookieName = "sessionid";

    private const string InstagramOrigin = "https://www.instagram.com/";

    private readonly CoreWebView2 _core;
    private readonly DispatcherQueue _dispatcher;

    public WebViewSessionCookieProbe(CoreWebView2 core, DispatcherQueue dispatcher)
    {
        _core = core;
        _dispatcher = dispatcher;
    }

    public Task<bool> SessionCookieExistsAsync(CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        // CoreWebView2 is single-threaded; the watcher polls from the pool.
        if (!_dispatcher.TryEnqueue(async () =>
        {
            try
            {
                var cookies = await _core.CookieManager.GetCookiesAsync(InstagramOrigin);
                var exists = false;
                foreach (var cookie in cookies)
                {
                    if (string.Equals(cookie.Name, SessionCookieName, StringComparison.Ordinal))
                    {
                        exists = true;
                        break; // name checked, value never read
                    }
                }
                completion.TrySetResult(exists);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }))
        {
            completion.TrySetResult(false); // dispatcher shut down: fail closed
        }

        return completion.Task;
    }
}
