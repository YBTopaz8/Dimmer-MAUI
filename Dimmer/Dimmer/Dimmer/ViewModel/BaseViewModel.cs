

using Dimmer.UIUtils;

namespace Dimmer.ViewModel;
public partial class BaseViewModel : ObservableObject
{
    private readonly IMapper mapper;
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> DisplayedSongs { get; set; } = new ObservableCollection<SongModelView>();

    [ObservableProperty]
    public partial SongModelView MySelectedSong { get; set; }

    [ObservableProperty]
    public partial View MySelectedSongView { get; set; }
    public BaseAppFlow BaseAppFlow { get; }

    public BaseViewModel(BaseAppFlow baseAppFlow, IMapper mapper)
    {
        BaseAppFlow=baseAppFlow;
        this.mapper=mapper;
        SubscribeToDisplayedSongs();
    }

    private void SubscribeToDisplayedSongs()
    {
        BaseAppFlow.AllSongs.Subscribe(songs =>
        {
            DisplayedSongs = mapper.Map<List<SongModelView>>(songs).ToObservableCollection();
        });
    }

    public void SetSelectedSong(SongModelView song)
    {
        if (MySelectedSong is not null)
        {
            MySelectedSong.IsCurrentPlayingHighlight = false;
        }
        song.IsCurrentPlayingHighlight = true;
        MySelectedSong = song;        
    }

    public void PlaySong(SongModelView song)
    {
        
        //BaseAppFlow.AudioService.PlaySong(song.Song);
    }

}
