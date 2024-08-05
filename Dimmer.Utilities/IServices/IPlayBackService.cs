﻿
namespace Dimmer.Utilities.IServices;
public interface IPlayBackService
{
    IObservable<IList<SongsModelView>> NowPlayingSongs { get; } //to display songs in queue
    Task<bool> PlaySongAsync(SongsModelView song); //to play song
    Task<bool> PlayNextSongAsync(); //to play next song
    Task<bool> PlayPreviousSongAsync(); //to play previous song
    Task<bool> StopSongAsync(); //to stop song
    Task<bool> PauseResumeSongAsync(); //to pause/resume song

    void RemoveSongFromQueue(SongsModelView song); //to remove song from queue
    void AddSongToQueue(SongsModelView song); //to add song to queue

    SongsModelView CurrentlyPlayingSong { get; }
    int LoadProgressPercent { get; }
    string TotalSongsSizes { get; }
    string TotalSongsDuration { get; }
    bool IsShuffleOn { get; set; }
    int CurrentRepeatMode { get; set; }
    IObservable<PlaybackInfo> CurrentPosition { get; } //to read position and update slider
    Task SetSongPosition(double positionFraction); // to set position from slider
    IObservable<MediaPlayerState> PlayerState { get; } //to update play/pause button
    Task<bool> LoadSongsFromFolder(List<string> folderPath);//to load songs from folder
    void ChangeVolume(double newVolumeValue);
    void SearchSong(string songTitleOrArtistName); //to search song with title
    void DecreaseVolume();
    void IncreaseVolume();
    void ToggleShuffle(bool isShuffleOn);
    int ToggleRepeatMode();

    void UpdateSongToFavoritesPlayList(SongsModelView song);

    void UpdateCurrentQueue();

    bool PlaySelectedSongsOutsideApp(string[] filePaths);

}
