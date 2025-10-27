using System.Diagnostics;
using System.Threading.Tasks;

using Syncfusion.Maui.Toolkit.NavigationDrawer;

using Windows.Graphics;

namespace Dimmer.WinUI.Views.CustomViews;

public partial class SyncLyricsPopUpView : Window
{
	public SyncLyricsPopUpView(BaseViewModelWin baseViewModel)
	{
		InitializeComponent();
        MyViewModel = baseViewModel;
        BindingContext = baseViewModel;
        
        
    }

    public BaseViewModelWin MyViewModel { get; }

    private void AllLyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        if (this.Page is null) return;
        //if (this.Page.IsLoaded && AllLyricsColView.IsLoaded)
        //{
            if(MyViewModel.AllLines?.Count == 1)
            {
                Application.Current?.CloseWindow(this);
            }
            var currentList = e.CurrentSelection as IReadOnlyList<object>;
            var current = currentList?.FirstOrDefault() as Dimmer.Data.ModelView.LyricPhraseModelView;
            if (current != null)
            {
                var pastList = e.PreviousSelection as IReadOnlyList<object>;
                if (pastList.Count > 0 && pastList?[0] is Dimmer.Data.ModelView.LyricPhraseModelView past)
                {
                    past?.NowPlayingLyricsFontSize = 25;
                    past?.HighlightColor = Microsoft.Maui.Graphics.Colors.White;
                    past?.IsHighlighted = false;
                }
                current?.NowPlayingLyricsFontSize = 40;
                current?.IsHighlighted = true;
                current?.HighlightColor = Microsoft.Maui.Graphics.Colors.SlateBlue;
                //AllLyricsColView.ScrollTo(item: current, null,ScrollToPosition.Center, animate: true);

            //}
        }
    }

    private async void OpenLyricsViewOnly_Clicked(object sender, EventArgs e)
    {
        await this.myPage.FadeOut();
        Application.Current?.CloseWindow(this);

        MyViewModel.ActivateMainWindow();

    }

    private void Window_Deactivated(object sender, EventArgs e)
    {

    }

    private async void SyncLyricsPopUpWindow_Deactivated(object sender, EventArgs e)
    {
        await Task.Delay(500);

        //await PositionPicker.AnimateHeight(0, 300, Easing.BounceOut);
        //PositionPicker.IsVisible = false;
        Debug.WriteLine("Sync Deactivated");
    }

    private async void SyncLyricsPopUpWindow_Activated(object sender, EventArgs e)
    {
        //BtmVSL.IsVisible = true;
        //await BtmVSL.AnimateHeight(30, 350, Easing.SpringOut);
        Debug.WriteLine("Sync Lyrc Activated");
    }

    private void SetWindowPosition_Clicked(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        var parameter = send.CommandParameter as string ;
        var newPosition = new RectInt32();

        var width = DisplayArea.Primary.WorkArea.Width;
        var height = DisplayArea.Primary.WorkArea.Height;
        var x = DisplayArea.Primary.WorkArea.X;
        var y = DisplayArea.Primary.WorkArea.Y;
        var coordLeft = x + (width - 400);
        var coordRight = x + (width - 400);
        var coordTop = y + (height);
        var coordBtm = y + (height - 400);

        //
        
        switch (parameter)
        {
            case "0":
                newPosition.X = 0;
                newPosition.Y = 0;

                break;
            case "1":
                newPosition.X = coordRight;
                newPosition.Y = 0;
                break;
            case "2":
                newPosition.X = coordLeft;
                newPosition.Y = coordBtm;
                break;
            case "3":
                newPosition.X = 0;
                newPosition.Y = coordBtm;
                break;
            default:
                break;
        }

        //AppWindow.MoveAndResize(new Windows.Graphics.RectInt32
        //{
        //    Height = height,
        //    Width = 340,
        //    X = x,
        //    Y = y
        //});

        // move to left x - (width - 400)
        // move to right x + (width - 400)

        //move to top y - (height - 400)
        //move to top y + (height - 400)
        newPosition.Height = 400; newPosition.Width = 400;
        this.MoveAndResizeWindow(newPosition);
    }

    private async void ShowHidePositioner_Clicked(object sender, EventArgs e)
    {
        try
        {

            //if (PositionPicker.IsVisible)
            //{
            //    await Task.WhenAll(PositionPicker.AnimateHeight(0, 300, Easing.BounceOut),
            //    BtmVSL.AnimateHeight(0, 300, Easing.BounceOut));
                
            //    PositionPicker.IsVisible = false;
            //    BtmVSL.IsVisible = false;
            //}
            //else
            //{
            //    PositionPicker.IsVisible = true;
            //    BtmVSL.IsVisible = true;
            //    await Task.WhenAll(PositionPicker.AnimateHeight(105, 350, Easing.SpringOut), BtmVSL.AnimateHeight(30, 350, Easing.SpringOut));
               
            //}
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}