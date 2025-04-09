using Dimmer.Interfaces;

namespace Dimmer.ViewModel;
public partial class BaseViewModel : ObservableObject
{
    private readonly IMapper mapper;
    public readonly SongsMgtFlow songsMgtFlow;

    #region public properties
    [ObservableProperty]
    public partial bool IsShuffle { get; set; }
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
    [ObservableProperty]
    public partial int CurrentPositionPercentage { get; set; }
    [ObservableProperty]
    public partial RepeatMode RepeatMode { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SongModelView> MasterSongs { get; internal set; }
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
        this.songsMgtFlow=songsMgtFlow;
        SubscribeToMasterSongs();
        SubscribeToCurrentlyPlayingSong();
        SubscribeToIsPlaying();
        SubscribeToCurrentPosition();
    }

    partial void OnMasterSongsChanging(ObservableCollection<SongModelView> oldValue, ObservableCollection<SongModelView> newValue)
    {
        if (newValue is null || newValue.Count<1)
        {

        }
    }
    void SubscribeToCurrentPosition()
    {
        songsMgtFlow.CurrentSongPosition.Subscribe(position =>
        {
            CurrentPositionInSeconds = position;

            CurrentPositionPercentage = (int)(position / songsMgtFlow.CurrentlyPlayingSong.DurationInSeconds);
        }); 
    }

    public void SubscribeToIsPlaying()
    {
        songsMgtFlow.IsPlaying.DistinctUntilChanged()
        .Subscribe(isPlaying =>
        {
            IsPlaying = isPlaying;
            switch (isPlaying)
            {
                case true:
                    CurrentPositionInSeconds = songsMgtFlow.CurrentPositionInSec;
                    break;
                default:
                    IsPlaying = false;
                    if (songsMgtFlow.IsPlayedCompletely)
                    {
                        PlayNext();
                    }
                    break;
            }
            
            
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
        song.IsCurrentPlayingHighlight = false;
        songsMgtFlow.CurrentlyPlayingSong ??=song;
        MySelectedSong = song;        
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
            if (index <= 0)
            {
                index = MasterSongs.Count - 1;
            }
            else
            {
                index--;

            }

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
        if(TemporarilyPickedSong is not null)
        {
            TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
        }
        songsMgtFlow.CurrentlyPlayingSong = song;
        TemporarilyPickedSong = song;     
        
        songsMgtFlow.PlaySongInAudioService();
    }

    public void PlayPauseSong()
    {
        if ( IsPlaying)
        {

            songsMgtFlow.PauseResumeSong(CurrentPositionInSeconds, true);
            
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
    }
        public void ResumeSong()
    {
        songsMgtFlow.PauseResumeSong(CurrentPositionInSeconds);
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
        double newPos = 0;
        if (currPosPer !=0)
        {
            newPos = songsMgtFlow.CurrentlyPlayingSong.DurationInSeconds*currPosPer;
            

        }
        songsMgtFlow.SeekTo(newPos);
    }

    public void ToggleRepeatMode()
    {
       RepeatMode = songsMgtFlow.ToggleRepeatMode();

    }

    public void IncredeVolume()
    {
        songsMgtFlow.IncreaseVolume();
        VolumeLevel = songsMgtFlow.VolumeLevel * 100;
    }
    public void DecredeVolume()
    {
        songsMgtFlow.DecreaseVolume();
        VolumeLevel = songsMgtFlow.VolumeLevel * 100;
    }

    public void SetVolume(double vol)
    {
        songsMgtFlow.ChangeVolume(vol);
        VolumeLevel = songsMgtFlow.VolumeLevel*100;
    }
}

