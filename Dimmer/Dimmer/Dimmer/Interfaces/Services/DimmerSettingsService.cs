namespace Dimmer.Interfaces.Services;
public partial class DimmerSettingsService : ISettingsService
{
    private readonly Realm _realm;
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
        _model = LoadSettings();
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
    public IList<string> UserMusicFoldersPreference
        => _model.UserMusicFoldersPreference;

    public double LastVolume { get; set; }
    public bool MinimizeToTrayPreference { get; set; }



    // Add a folder
    public void AddMusicFolder(string path)
    {

        _realm.Write(() =>
        {
            _model.UserMusicFoldersPreference.Add(path);


        });
    }

    // Remove a folder
    public bool RemoveMusicFolder(string path)
    {
        if (!_model.UserMusicFoldersPreference.Contains(path))

            return false;
        _realm.Write(() => _model.UserMusicFoldersPreference.Remove(path));
        return true;
    }
    public bool ClearAllFolders()
    {
        _realm.Write(() => _model.UserMusicFoldersPreference.Clear());
        return true;
    }
    // Replace entire list
    public void SetMusicFolders(IEnumerable<string> paths)
    {

        _realm.Write(() =>
        {
            _model.UserMusicFoldersPreference.Clear();
            foreach (var p in paths)
                _model.UserMusicFoldersPreference.Add(p);
        });
    }


}