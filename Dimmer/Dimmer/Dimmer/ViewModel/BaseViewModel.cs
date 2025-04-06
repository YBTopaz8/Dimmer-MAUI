

using Dimmer.Interfaces;
using Dimmer.UIUtils;

namespace Dimmer.ViewModel;
public partial class BaseViewModel : ObservableObject
{
    private readonly IMapper mapper;
    private readonly SongsMgtFlow songsMgtFlow;
    private readonly IDimmerAudioService dimmerAudioService;
    
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> DisplayedSongs { get; set; } = new ObservableCollection<SongModelView>();


    [ObservableProperty]
    public partial SongModelView? TemporarilyPickedSong { get; set; }
    [ObservableProperty]
    public partial View? MySelectedSongView { get; set; }

    public BaseViewModel(IMapper mapper, SongsMgtFlow songsMgtFlow
        ,IDimmerAudioService dimmerAudioService)
    {
        this.mapper=mapper;
        this.songsMgtFlow=songsMgtFlow;
        this.dimmerAudioService=dimmerAudioService;
        SubscribeToDisplayedSongs();
    }

    private void SubscribeToDisplayedSongs()
    {
        BaseAppFlow.AllSongs.Subscribe(songs =>
        {
            DisplayedSongs = mapper.Map<List<SongModelView>>(songs).ToObservableCollection();
        });
    }

    public void PlaySong(SongModelView song)
    {
        TemporarilyPickedSong = song;        
        songsMgtFlow.PlaySongInAudioService();
    }
    SongModelView MySelectedSong { get; set; } = new SongModelView();
    public void SetSelectedSong(SongModelView song)
    {
        songsMgtFlow.CurrentlyPlayingSong ??=song;
        
        if (MySelectedSong is not null)
        {
            MySelectedSong.IsCurrentPlayingHighlight = false;
        }

        MySelectedSong = song;
        MySelectedSong.IsCurrentPlayingHighlight = true;
    }
}
