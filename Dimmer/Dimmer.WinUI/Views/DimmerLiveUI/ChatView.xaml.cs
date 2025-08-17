namespace Dimmer.WinUI.Views.DimmerLiveUI;

public partial class ChatView : ContentPage
{
	public ChatView( ChatViewModelWin chatViewModelWin)
	{
		InitializeComponent();
        BindingContext = chatViewModelWin;
        ChatViewModelWin=chatViewModelWin;
    }

    public ChatViewModelWin ChatViewModelWin { get; }
}