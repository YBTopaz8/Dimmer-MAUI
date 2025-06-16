using System.ComponentModel;

using DevExpress.Maui.Controls;
using DevExpress.Maui.Editors;

using Dimmer.Utilities.CustomAnimations;

using View = Microsoft.Maui.Controls.View;


namespace Dimmer.Views.CustomViewsParts;

public partial class BtmBar : DXBorder
{
    public BtmBar()
    {
        InitializeComponent();
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>();
        this.BindingContext =vm;
        this.MyViewModel =vm;
    }

    public BaseViewModelAnd MyViewModel { get; set; }


    private async void BtmBarTapGest_Tapped(object sender, TappedEventArgs e)
    {
        //DXBorder send = (DXBorder)sender;


        await MyViewModel.BaseVM.PlayPauseToggleAsync();

    }

    private double _startX;
    private double _startY;
    private bool _isPanning;

    double btmBarHeight = 145;


    private async void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        View send = (View)sender;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isPanning = true;
                _startX = this.TranslationX;
                _startY = this.TranslationY;
                break;

            case GestureStatus.Running:
                if (!_isPanning)
                    return; // Safety check

                this.TranslationX = _startX + e.TotalX;
                this.TranslationY = _startY + e.TotalY;
                break;

            case GestureStatus.Completed:
                _isPanning = false;

                double deltaX = this.TranslationX - _startX;
                double deltaY = this.TranslationY - _startY;
                double absDeltaX = Math.Abs(deltaX);
                double absDeltaY = Math.Abs(deltaY);

                if (absDeltaX > absDeltaY) // Horizontal swipe
                {
                    if (absDeltaX > absDeltaY) // Horizontal swipe
                    {
                        try
                        {
                            if (deltaX > 0) // Right
                            {
                                HapticFeedback.Perform(HapticFeedbackType.LongPress);
                                Debug.WriteLine("Swiped Right");

                                await MyViewModel.BaseVM.NextTrack();

                                Task<bool> bounceTask = this.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(bounceTask);
                            }
                            else // Left
                            {
                                Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                                await MyViewModel.BaseVM.PreviousTrack();

                                Task<bool> bounceTask = this.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(bounceTask);
                            }
                        }
                        catch (Exception ex) // Handle exceptions
                        {
                            Debug.WriteLine($"Error: {ex.Message}"); // Log the error
                        }
                        finally
                        {
                            this.TranslationX = 0; // Reset translation
                            this.TranslationY = 0; // Reset translation

                        }
                    }

                    else // Left
                    {
                        try
                        {
                            Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                            await MyViewModel.BaseVM.PreviousTrack();
                            Debug.WriteLine("Swiped left");
                            Task t1 = send.MyBackgroundColorTo(Colors.MediumPurple, length: 300);
                            Task t2 = Task.Delay(500);
                            Task t3 = send.MyBackgroundColorTo(Colors.DarkSlateBlue, length: 300);
                            await Task.WhenAll(t1, t2, t3);
                        }
                        catch { }
                    }
                }
                else  //Vertical swipe
                {
                    if (deltaY > 0) // Down
                    {

                        try
                        {
                            //DXCollectionView songsView = this.Parent.FindByName<DXCollectionView>("SongsColView");
                            //int itemHandle = songsView.FindItemHandle(MyViewModel.BaseVM.CurrentPlayingSongView);
                            //songsView.ScrollTo(itemHandle, DXScrollToPosition.Start);
                            MyViewModel.ScrollToSongCommand.Execute(null);
                            HapticFeedback.Perform(HapticFeedbackType.LongPress);
                        }
                        catch { }
                    }
                    else  // Up
                    {
                        try
                        {
                            btmBarHeight=this.Height;

                            BottomSheet bottomSheet = this.Parent.FindByName<BottomSheet>("NowPlayingBtmSheet");
                            bottomSheet.Show();

                        }
                        catch { }
                    }

                }

                await this.TranslateTo(0, 0, 450, Easing.BounceOut);
                break;


            case GestureStatus.Canceled:
                _isPanning = false;
                await this.TranslateTo(0, 0, 350, Easing.BounceOut); // Return to original position
                break;

        }
    }

    private void DurationAndSearchChip_LongPress(object sender, HandledEventArgs e)
    {
        //TextEdit SearchBy = this.Parent.FindByName<TextEdit>("SearchBy");
        //SearchBy.Focus();
    }
    public static DXCollectionView PageColView { get; set; }
}