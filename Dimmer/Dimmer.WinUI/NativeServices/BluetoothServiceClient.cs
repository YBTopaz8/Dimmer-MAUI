using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Dimmer.DimmerLive;
using Dimmer.DimmerLive.Models;

using Hqub.Lastfm.Services;

namespace Dimmer.WinUI.NativeServices;

public class BluetoothServiceClient
{
    private readonly IBluetoothService _btService;
    private readonly IAuthenticationService _userService; // Your local DB service

    public BaseViewModel BaseVM { get; }

    public BluetoothServiceClient(IBluetoothService btService, IAuthenticationService userService, BaseViewModel baseVM)
    {
        _btService = btService;
        _userService = userService;
        BaseVM = baseVM;
        _btService.DataReceived += OnBluetoothDataReceived;
    }

     //1. BACKUP FUNCTION
    public async Task SendFullBackupAsync()
    {
        Debug.WriteLine(BaseVM.SearchResults.Count);
        BaseVM.SearchToTQL(TQlStaticMethods.PresetQueries.ShowMyFav());
        // Gather Data locally
        var currentUser = _userService.CurrentUserValue;
        BaseVM.OnAppClosing();
        var appstate = BaseVM.RealmFactory.GetRealmInstance().All<AppStateModel>().FirstOrDefaultNullSafe();
        var favoriteSongsTitleAndDurationId = BaseVM.RealmFactory.GetRealmInstance().All<SongModel>().AsEnumerable().Where(x => x.IsFavorite).Select(x => x.TitleDurationKey).ToList();
        Debug.WriteLine(BaseVM.SearchResults.Count);
        var backup = new FullBackupData()
        {
            AppState = appstate.ToAppStateModelView()!
            ,
            FavoriteSongsTitleAndDurationId = favoriteSongsTitleAndDurationId,
            PlayBackSongTitleAndDurationId = BaseVM.PlaybackQueue.Select(x => x.TitleDurationKey).ToList(),
            
            User = BaseVM.CurrentUserLocal
        };
        Debug.WriteLine(BaseVM.SearchResults.Count);
        //var backup = new FullBackupData
        //{
        //    User = new UserModelView(currentUser), // Convert to View Model
        //    PlayEvents = allEvents.Select(e => new DimmerPlayEventView(e)).ToList()
        //};

        var package = new BluetoothDataPackage
        {
            Type = DataPackageType.Backup,
            PayloadJson = JsonSerializer.Serialize(backup)
        };

        await _btService.SendDataAsync(package);
    }

    // 2. SESSION TRANSFER FUNCTION
    public async Task SendSessionTransferAsync(DimmerSharedSong songDetails)
    {
        // We only send the metadata, not the file
        var package = new BluetoothDataPackage
        {
            Type = DataPackageType.SessionTransfer,
            PayloadJson = JsonSerializer.Serialize(songDetails)
        };

        await _btService.SendDataAsync(package);
    }

    // 3. RECEIVE LOGIC
    private void OnBluetoothDataReceived(object? sender, string jsonString)
    {
        try
        {
            var package = JsonSerializer.Deserialize<BluetoothDataPackage>(jsonString);

            switch (package.Type)
            {
                case DataPackageType.Backup:
                    var backupData = JsonSerializer.Deserialize<FullBackupData>(package.PayloadJson);
                    HandleBackupRestore(backupData);
                    break;

                case DataPackageType.SessionTransfer:
                    var songData = JsonSerializer.Deserialize<DimmerSharedSong>(package.PayloadJson);
                    HandleSessionTransfer(songData);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Log JSON parsing error
        }
    }

    private void HandleBackupRestore(FullBackupData data)
    {
        // Logic to save 'data.User' and 'data.PlayEvents' to your local SQLite/Realm
        Console.WriteLine($"Restoring {data.AppState.LastKnownPlaybackQuery} events for user {data.User.Username}");
    }

    private void HandleSessionTransfer(DimmerSharedSong song)
    {
        // Logic to trigger UI popup: "Resume {Song} at {Position}?"
        Console.WriteLine($"Incoming Session: {song.Title} at {song.SharedPositionInSeconds}s");
    }
}