using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using DevExpress.Maui.Controls;
using DevExpress.Maui.Editors;

using Dimmer.Data.Models;
using Dimmer.Utilities;
using Dimmer.Utilities.CustomAnimations;
using Dimmer.ViewModel;

namespace Dimmer.Views.CustomViewsParts;

public partial class NowPlayingbtmsheet : BottomSheet
{
    public NowPlayingbtmsheet()
    {
        InitializeComponent();

        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>();
        this.BindingContext =vm;
        this.MyViewModel =vm;
    }

    public BaseViewModelAnd MyViewModel { get; set; }

    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        var send = (DXSlider)sender;


        MyViewModel.BaseVM.SeekTrackPosition(ProgressSlider.Value);
    }

    private void NowPlayingBtmSheet_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        //if (MyViewModel.BaseVM.IsPlaying)
        //{
        //    SongPicture.StartHeartbeat();
        //}
    }

    private async void Chip_Tap(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;

        var song = send.TapCommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        var artRepo = IPlatformApplication.Current!.Services.GetService<IRepository<ArtistModel>>();

        var allArtists = artRepo.GetAll().Where(x => x.Songs.Any(s => s.Id == song.Id));

        var listOfArtNames = allArtists.Select(x => x.Name).ToArray();

        var result = await Shell.Current.DisplayActionSheet("Select Artist To View", "Cancel", null, listOfArtNames);

        if (result is null)
        {
            return;
        }
        if (result == "Cancel" || string.IsNullOrEmpty(result))
            return;


        var art = song.ArtistIds?.FirstOrDefault(x => x?.Name==result);
        DeviceStaticUtils.SelectedArtistOne = art;
        //await SongsMenuPopup.CloseAsync();
        await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        await this.CloseAsync();
    }

    private void NowPlayingBtmSheet_Unloaded(object sender, EventArgs e)
    {
        //SongPicture.StopHeartbeat();

    }

    private async void SongTitleChip_Tap(object sender, HandledEventArgs e)
    {
        await CloseAsync();

        MyViewModel.BaseVM.SelectedSongForContext = MyViewModel.BaseVM.CurrentPlayingSongView;

        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }
}