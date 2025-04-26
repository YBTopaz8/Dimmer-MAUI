using Dimmer.Data.Models;
using Dimmer.Data.ModelView;
using Dimmer.Interfaces;
using Dimmer.Utilities;
using System.Threading.Tasks;

namespace Dimmer.Views;

public partial class HomePage : ContentPage
{
    private readonly IDimmerAudioService dimmeraudio;
    private readonly IFilePicker filePicker;

    public HomePage(IDimmerAudioService dimmeraudio, IFilePicker filePicker)
	{
		InitializeComponent();
        this.dimmeraudio=dimmeraudio;
        this.filePicker=filePicker;
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var res = await filePicker.PickAsync();

        if (res != null)
        {
            SongModel song = new SongModel()
            {
                Title = res.FileName,
                FilePath = res.FullPath,
                
                IsPlaying = false,
                
            };
            var img= PlayBackStaticUtils.GetCoverImage(song.FilePath, true);
            await dimmeraudio.InitializeAsync(song, img);
            await dimmeraudio.PlayAsync();
        }
    }
}