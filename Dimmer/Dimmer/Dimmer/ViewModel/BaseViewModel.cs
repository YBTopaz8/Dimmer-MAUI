

using CommunityToolkit.Mvvm.Input;
using Dimmer.Interfaces;

namespace Dimmer.ViewModel;
public partial class BaseViewModel : ObservableObject
{
    private readonly IMapper mapper;
    private readonly SongsMgtFlow SongsMgtFlow;

    #region public properties
    [ObservableProperty]
    public partial bool IsShuffle { get; set; }
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
    [ObservableProperty]
    public partial double CurrentPositionPercentage { get; set; }
    [ObservableProperty]
    public partial RepeatMode RepeatMode { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SongModelView> MasterSongs { get; set; } = new ObservableCollection<SongModelView>();
    [ObservableProperty]
    public partial SongModelView? TemporarilyPickedSong { get; set; }
    [ObservableProperty]
    public partial View? MySelectedSongView { get; set; }
    [ObservableProperty]
    public partial double CurrentPositionInSeconds { get; set; }
    [ObservableProperty]
    public partial double VolumeLevel { get; set; }
    
    
    #endregion
    public BaseViewModel(IMapper mapper, SongsMgtFlow songsMgtFlow
        ,IDimmerAudioService dimmerAudioService)
    {
        this.mapper=mapper;
        this.SongsMgtFlow=songsMgtFlow;
        SubscribeToMasterSongs();
        SubscribeToCurrentlyPlayingSong();
        SubscribeToIsPlaying();
    }
    private void SubscribeToIsPlaying()
    {
        SongsMgtFlow.IsPlaying.DistinctUntilChanged()
            .Subscribe(isPlaying =>
        {
            IsPlaying = isPlaying;
            
        });
    }

    public void SubscribeToCurrentlyPlayingSong()
    {
        BaseAppFlow.CurrentSong
            .DistinctUntilChanged()
            .Subscribe(song =>
        {
            var e = mapper.Map<SongModelView>(song);
            //todo load/reload playlist and mostly stats
        });
    }
    private void SubscribeToMasterSongs()
    {
        BaseAppFlow.AllSongs.Subscribe(songs =>
        {
            MasterSongs = mapper.Map<List<SongModelView>>(songs).ToObservableCollection();
        });
    }
    Random random = new Random();

    public SongModelView MySelectedSong { get; set; } = new SongModelView();
    public void SetSelectedSong(SongModelView song)
    {
        SongsMgtFlow.CurrentlyPlayingSong ??=song;

        if (MySelectedSong is not null)
        {
            MySelectedSong.IsCurrentPlayingHighlight = false;
        }

        MySelectedSong = song;
        MySelectedSong.IsCurrentPlayingHighlight = true;
    }
    #region playback controls 
    public void PlayPrevious()
    {
        if (IsShuffle)
        {
           bool isInCol =  IsFoundInCollection(TemporarilyPickedSong!, MasterSongs);
            if (isInCol)
            {
                int index = MasterSongs.IndexOf(TemporarilyPickedSong!);
                int newIndex = random.Next(0, MasterSongs.Count);
                while (newIndex == index)
                {
                    newIndex = random.Next(0, MasterSongs.Count);
                }
                var song = MasterSongs[newIndex];
                PlaySong(song);
            }
            else
            {
                PlaySong(TemporarilyPickedSong!);
            }
        }
        else
        {
            int index = MasterSongs.IndexOf(TemporarilyPickedSong!);
            index--;
            var song = MasterSongs[index];
            if (song != null)
            {
                PlaySong(song);
            }
        }
    }
     public void PlayNext()
    {
        if (IsShuffle)
        {
           bool isInCol =  IsFoundInCollection(TemporarilyPickedSong!, MasterSongs);
            if (isInCol)
            {
                int index = MasterSongs.IndexOf(TemporarilyPickedSong!);
                int newIndex = random.Next(0, MasterSongs.Count);
                while (newIndex == index)
                {
                    newIndex = random.Next(0, MasterSongs.Count);
                }
                var song = MasterSongs[newIndex];
                PlaySong(song);
            }
            else
            {
                PlaySong(TemporarilyPickedSong!);
            }
        }
        else
        {
            int index = MasterSongs.IndexOf(TemporarilyPickedSong!);
            index++;
            var song = MasterSongs[index];
            if (song != null)
            {
                PlaySong(song);
            }
        }
    }

    public void PlaySong(SongModelView song)
    {
        SongsMgtFlow.CurrentlyPlayingSong = song;
        TemporarilyPickedSong = song;     
        
        SongsMgtFlow.PlaySongInAudioService();
    }

    public void PlayPauseSong()
    {
        if (IsPlaying)
        {
            PauseSong();
        }
        else
        {
            if (CurrentPositionPercentage >= 0.98)
            {
                PlaySong(TemporarilyPickedSong);
                return;
            }
            ResumeSong();
        }
    }

    public void PauseSong()
    {
        //CurrentPositionInSeconds = PlayBackService.CurrentPosition;
        SongsMgtFlow.PauseResumeSong(CurrentPositionInSeconds, true);
    }
        public void ResumeSong()
    {
        SongsMgtFlow.PauseResumeSong(CurrentPositionInSeconds);
    }


    #endregion

    static bool IsFoundInCollection(SongModelView song, IEnumerable<SongModelView> songs)
    {
        foreach (var s in songs)
        {
            if (s.LocalDeviceId == song.LocalDeviceId)
                return true;
        }
        return false;
    }
    public void SeekSongPosition(double currPosPer = 0)
    {
        
        if (currPosPer !=0)
        {
            CurrentPositionInSeconds = currPosPer * SongsMgtFlow.CurrentlyPlayingSong.DurationInSeconds;

        }
        SongsMgtFlow.SeekTo(CurrentPositionInSeconds);
    }

    public void ToggleRepeatMode()
    {
        SongsMgtFlow.ToggleRepeatMode();

    }

    public void IncredeVolume()
    {
        SongsMgtFlow.IncreaseVolume();
    }
    public void DecredeVolume()
    {
        SongsMgtFlow.DecreaseVolume();
    }

}

