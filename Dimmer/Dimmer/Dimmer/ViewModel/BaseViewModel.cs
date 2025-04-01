

namespace Dimmer.ViewModel;
public partial class BaseViewModel : ObservableObject
{
    private readonly IMapper mapper;
    [ObservableProperty]
    public partial ObservableCollection<SongModel> DisplayedSongs { get; set; } = new ObservableCollection<SongModel>();
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
            DisplayedSongs = mapper.Map<List<SongModel>>(songs).ToObservableCollection();
        });
    }

    public BaseAppFlow BaseAppFlow { get; }

    public virtual void Sum()
    {
        Debug.WriteLine("Yvan");
    }
}
