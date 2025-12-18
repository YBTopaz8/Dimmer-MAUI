using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel.DimmerLiveVM;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using DataTemplate = Microsoft.UI.Xaml.DataTemplate;
using DataTemplateSelector = Microsoft.UI.Xaml.Controls.DataTemplateSelector;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.DimmerLive;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>

public sealed partial class SocialPage : Page
{
    public ChatViewModel ChatVM { get; set; }
    public SocialViewModel SocialVM { get; set; }

    public SocialPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        SocialVM = IPlatformApplication.Current!.Services.GetService<SocialViewModel>()!;
        ChatVM = IPlatformApplication.Current!.Services.GetService<ChatViewModel>()!;
        this.DataContext = ChatVM; // Set DataContext for binding within DataTemplates
        this.Name = "RootPage"; // Helper for ElementName binding
        
    }
    // Auto-scroll to bottom when new messages arrive
    private async void MessagesList_Loaded(object sender, RoutedEventArgs e)
    {
        var listView = sender as ListView;
        // Hook into collection changed event if needed, or just scroll on load
        if (listView.Items.Count > 0)
           await listView.SmoothScrollIntoViewWithItemAsync(listView.Items[listView.Items.Count - 1]);
    }
}

public class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate MyMessageTemplate { get; set; }
    public DataTemplate PeerMessageTemplate { get; set; }
    public DataTemplate SongShareTemplate { get; set; }

    // Helper to identify "My" ID. In a real app, pass current user ID via DI or singleton.
    // For now, we assume if "UserSenderId" matches specific criteria or visual layout.
    // NOTE: You might need to add a boolean 'IsMine' to your ChatMessage model wrapper for easier binding.

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is ChatMessage msg)
        {
            if (msg.MessageType == "SongShare")
                return SongShareTemplate;

            // Simplified logic: You need to compare msg.UserSenderId with CurrentUser.ObjectId
            // If you can't easily access CurrentUser here, you might default to Peer or 
            // add a property to the ViewModel wrapper.
            // Assuming ParseChatService.Username holds the device identifier logic used in the VM.

            // For this example, let's assume we bind alignment in XAML and just differentiate types.
            return PeerMessageTemplate;
        }
        return PeerMessageTemplate;
    }
}