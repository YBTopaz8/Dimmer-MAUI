using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Window = Microsoft.UI.Xaml.Window;

namespace Dimmer.WinUI.Utils;

public class WinUiErrorPresenter : IUiErrorPresenter
{
    public async Task ShowNotImplementedAlert(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Feature Not Implemented",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetActiveWindowXamlRoot()
        };

        await dialog.ShowAsync();
    }

    private static XamlRoot GetActiveWindowXamlRoot()
    {
        var s = System.Windows.Application.Current
            .Windows;
        var activeWindow = System.Windows.Application.Current
            .Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.Visible)?.Content.XamlRoot;
        return activeWindow == null ? throw new InvalidOperationException("No active window found to attach the dialog.") : activeWindow!;
    }
}