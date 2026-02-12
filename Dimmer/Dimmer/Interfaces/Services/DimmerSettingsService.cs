namespace Dimmer.Interfaces.Services;
public partial class DimmerSettingsService : ISettingsService
{
    private Realm _realm;
    private readonly IRealmFactory factory;
    private AppStateModel _model;

    public AppStateModel CurrentAppStateModel
    {
        get
        {
            if (_model == null)
            {
                return LoadSettings();
            }
            return _model;
        }
    }
    public DimmerSettingsService(IRealmFactory factory)
    {
        _realm = factory.GetRealmInstance();
        //_model = LoadSettings();
        this.factory=factory;
    }

    public AppStateModel LoadSettings()
    {
        var list = _realm.All<AppStateModel>().ToList();
        if (list.Count == 0)
        {
            _model = new AppStateModel();

            _realm.Write(() =>
            {
                _realm.Add(_model);
            });
            return _model;
        }
        else
            _model = list[0];

        return _model;

    }
    public RepeatMode RepeatMode
    {
        get
        {
            return (RepeatMode)_model.RepeatModePreference;
        }

        set
        {

            _realm.Write(() => _model.RepeatModePreference = (int)value);
        }
    }

    public bool ShuffleOn
    {
        get
        {
            return _model.ShuffleStatePreference;
        }

        set
        {

            _realm.Write(() => _model.ShuffleStatePreference = value);
        }
    }

    public ShuffleMode ShuffleMode
    {
        get
        {
            return (ShuffleMode)_model.ShuffleModePreference;
        }

        set
        {
            _realm.Write(() => _model.ShuffleModePreference = (int)value);
        }
    }

    public bool IsStickToTop
    {
        get
        {
            return _model.IsStickToTop;
        }

        set
        {


            _realm.Write(() => _model.IsStickToTop = value);
        }
    }

    public double VolumeLevel
    {
        get
        {
            return _model.VolumeLevelPreference;
        }

        set
        {

            _realm.Write(() => _model.VolumeLevelPreference = value);
        }
    }

    public double LastKnownPosition
    {
        get
        {
            return _model.LastKnownPosition;
        }
        set
        {
            _realm.Write(() => _model.LastKnownPosition = value);
        }
    }

    public string LastPlayedSong
    {
        get
        {
            return _model.CurrentSongId;
        }

        set
        {

            _realm.Write(() => _model.CurrentSongId = value);
        }
    }// Expose the live list
    
    public double LastVolume { get; set; }
    public bool MinimizeToTrayPreference { get; set; }


    public void SaveLastFMUserSession(string sessionTok)
    {
        _realm.Write(() => _model.LastFMSessionKey = sessionTok);
    }

    public string? GetLastFMUserSession()
    {
        return _model.LastFMSessionKey;
    }
  

   
    public bool ClearAllFolders()
    {
        _realm.Write(() => _model.UserMusicFolders.Clear());
        return true;
    }
   


}