using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.UIUtils;

public static class UiDialogs
{
    /// <summary>
    /// Safe DisplayActionSheet for WinUI: marshals to UI thread and finds a real Page.
    /// Returns null if no window/page is available (avoids 'Element not found').
    /// </summary>
    public static Task<string?> SafeDisplayActionSheetAsync(
        string title,
        string cancel,
        string? destruction = null,
        params string[] buttons)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // Try Shell first (if app uses Shell)
            var shellPage = Shell.Current?.CurrentPage;
            if (shellPage is not null)
                return await shellPage.DisplayActionSheet(title, cancel, destruction, buttons);

            // Fallback: first window’s root page
            var win = Application.Current?.Windows?.FirstOrDefault();
            var page = win?.Page;
            if (page is not null)
                return await page.DisplayActionSheet(title, cancel, destruction, buttons);

            // No visual tree yet → gracefully bail out
            return (string?)null;
        });
    }
}
