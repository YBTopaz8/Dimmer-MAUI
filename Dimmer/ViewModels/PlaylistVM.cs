using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.ViewModels;
public partial class PlaylistVM : HomePageVM
{
    [ObservableProperty]
    public partial ObservableCollection<PlaylistModelView>? DisplayedPlaylists { get; set; }
    [ObservableProperty]
    public partial string? SelectedPlaylistPageTitle { get; set; }

    [ObservableProperty]
    public partial PlaylistModelView? SelectedPlaylistToOpenBtmSheet { get; set; } = null;
    public IPlaybackUtilsService PlayBackService { get; }

    public PlaylistVM(IPlaybackUtilsService PlaybackManagerService, IFolderPicker folderPickerService, IFileSaver fileSaver, ILyricsService lyricsService, ISongsManagementService songsMgtService, IPlaylistManagementService playlistManagementService) : base(PlaybackManagerService, folderPickerService, fileSaver, lyricsService, songsMgtService, playlistManagementService)
    {
        PlayBackService =PlaybackManagerService;
        SubscribeToQueueChanges();
    }

    private void SubscribeToQueueChanges()
    {
        PlayBackService.NowPlayingSongs.Subscribe(songs =>
        {
            if (SelectedPlaylist is null)
            {
                SelectedPlaylist??=new();
            }
            SelectedPlaylist.DisplayedSongsFromPlaylist = songs;
        });
    }


    [RelayCommand]
    void SetPickedPlaylist(PlaylistModelView pl)
    {
        SelectedPlaylistToOpenBtmSheet = pl;
    }

    public void UpSertPlayList(PlaylistModelView playlistModel, List<string>? songIDs = null, bool IsAddSong = false, bool IsRemoveSong = false, bool IsDeletePlaylist = false)
    {
        if (songIDs is null)
        {
            
            return;
        }
        SelectedPlaylist = PlayBackService.AllPlaylists!.First(x => x.LocalDeviceId == playlistModel.LocalDeviceId!);
        if (IsAddSong)
        {

            AddToPlaylist(playlistModel, songIDs);
            //await toast.Show(cts.Token);
        }
        else if (IsRemoveSong)
        {
            RemoveFromPlaylist(playlistModel, songIDs);

        }
        else if (IsDeletePlaylist)
        {
            PlayBackService.AllPlaylists?.Remove(SelectedPlaylist);
            PlayBackService.DeletePlaylistThroughID(SelectedPlaylist.LocalDeviceId!);

            DisplayedPlaylists = PlayBackService.GetAllPlaylists();
            //await toast.Show(cts.Token);
        }
    }
    public void LoadFirstPlaylist()
    {
        RefreshPlaylists();
        if (DisplayedPlaylists is not null && DisplayedPlaylists.Count > 0)
        {
            OpenSpecificPlaylistPage(DisplayedPlaylists[0].LocalDeviceId!);
        }
    }

    public void RefreshPlaylists()
    {
        DisplayedPlaylists?.Clear();
        PlaylistManagementService.GetPlaylists();
        DisplayedPlaylists = PlaylistManagementService.AllPlaylists;
    }

    [RelayCommand]
    public void DeletePlaylist()
    {
        PlayBackService.DeletePlaylistThroughID(SelectedPlaylistToOpenBtmSheet.LocalDeviceId);
        //DisplayedPlaylists = PlayListService.GetAllPlaylists();
        //await toast.Show(cts.Token);
    }

    [RelayCommand]
    void OpenPlaylistMenuBtmSheet(PlaylistModelView playlist)
    {
        SelectedPlaylistToOpenBtmSheet = playlist;
    }

    [RelayCommand]
    void RenamePlaylist(string newPlaylistName)
    {
        //PlaylistManagementService.RenamePlaylist(SelectedPlaylistToOpenBtmSheet.LocalDeviceId, newPlaylistName);
        //HiDisplayedPlaylists = PlayListService.GetAllPlaylists();
    }


    public void AddToPlaylist(PlaylistModelView playlist, List<string?> songIDs)
    {
        songIDs = songIDs.ToList();

        if (!EnableContextMenuItems)
            return;

        foreach (var id in songIDs)
        {
            var songg = DisplayedSongs.First(x => x.LocalDeviceId == id);
            if (SelectedPlaylist.DisplayedSongsFromPlaylist.Contains(songg))
            {
                songIDs.Remove(id);
                continue;
            }
            SelectedPlaylist.DisplayedSongsFromPlaylist?.Add(songg);
        }
        PlayBackService.AddSongsToPlaylist(songIDs, playlist);

        RefreshPlaylists();

        GeneralStaticUtilities.ShowNotificationInternally($"Added {songIDs.Count} to AlbumsQueue: {playlist.Name}");
    }

    public void RemoveFromPlaylist(PlaylistModelView playlist, List<string>? songIDs = null)
    {
        songIDs ??= [MySelectedSong!.LocalDeviceId!];
        if (!EnableContextMenuItems)
            return;
        if (DisplayedPlaylists is null)
        {
            RefreshPlaylists();
        }
        foreach (var id in songIDs)
        {
            var songg = DisplayedSongs.First(x => x.LocalDeviceId == id);

            SelectedPlaylist.DisplayedSongsFromPlaylist?.Remove(songg);
        }
        PlayBackService.RemoveSongFromPlayListWithPlayListID(songIDs, playlist);

        GeneralStaticUtilities.ShowNotificationInternally($"Removed {songIDs.Count} to AlbumsQueue: {playlist.Name}");
    }


    [RelayCommand]
    public void OpenSpecificPlaylistPage(string PlaylistID)//string playlistName)
    {
        if (PlayBackService.AllPlaylists is null)
        {
            return;
        }
        SelectedPlaylist = PlayBackService.AllPlaylists.FirstOrDefault(x => x.LocalDeviceId == PlaylistID)!;
        SelectedPlaylist.DisplayedSongsFromPlaylist?.Clear();
        SelectedPlaylist.DisplayedSongsFromPlaylist= PlayBackService.GetSongsFromPlaylistID(PlaylistID).OrderBy(x => x.Title).ToObservableCollection();

        SelectedPlaylistPageTitle = PlayBackService.SelectedPlaylistName;

    }

    [RelayCommand]
    public void OpenPlaylistPage()
    {
        if (PlayBackService.AllPlaylists is null)
        {            
            return;
        }
        DisplayedPlaylists = PlayBackService.GetAllPlaylists();
        SelectedPlaylist = PlayBackService.AllPlaylists.FirstOrDefault()!;
        SelectedPlaylist.DisplayedSongsFromPlaylist?.Clear();
        SelectedPlaylist.DisplayedSongsFromPlaylist = PlayBackService.GetSongsFromPlaylistID(SelectedPlaylist.LocalDeviceId!).OrderBy(x => x.Title).ToObservableCollection();
        SelectedPlaylistPageTitle = PlayBackService.SelectedPlaylistName;
    }
}

