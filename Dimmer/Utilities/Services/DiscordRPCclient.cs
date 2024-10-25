using DiscordRPC;
using System.Diagnostics;


namespace Dimmer_MAUI.Utilities.Services;

public class DiscordRPCclient : IDiscordRPC
{
    private DiscordRpcClient _discordRpcClient;
    private bool _isInitialized = false;
    SongsModelView currentSong;
    public void Initialize()
    {
#if ANDROID
return;
#endif
        if (!_isInitialized)
        {
            _discordRpcClient = new DiscordRpcClient(SecretFilesAndKeys.DiscordKey);
            _discordRpcClient.OnConnectionFailed += _discordRpcClient_OnConnectionFailed;
            _discordRpcClient.OnError += _discordRpcClient_OnError;
            
            _discordRpcClient.OnReady += DiscordRpcClient_OnReady;
            //_discordRpcClient.OnPresenceUpdate += DiscordRpcClient_OnPresenceUpdate;
            _discordRpcClient.Initialize();
            _isInitialized = true;
        }
    }

    private void _discordRpcClient_OnError(object sender, DiscordRPC.Message.ErrorMessage args)
    {
        Debug.WriteLine("RPC On Error!!!");
    }

    private void _discordRpcClient_OnConnectionFailed(object sender, DiscordRPC.Message.ConnectionFailedMessage args)
    {
        Debug.WriteLine("RPC Con Failed!!!");
        this.ShutDown();
        _discordRpcClient.OnConnectionFailed -= _discordRpcClient_OnConnectionFailed;
        _discordRpcClient.OnError -= _discordRpcClient_OnError;

        _discordRpcClient.OnReady -= DiscordRpcClient_OnReady;
    }

    private void DiscordRpcClient_OnReady(object sender, DiscordRPC.Message.ReadyMessage args)
    {
        Debug.WriteLine($"RPC is ready and connected to {args.User.Username}");
    }

    public void ClearPresence()
    {
        if (_isInitialized)
        {
            _discordRpcClient.ClearPresence();
        }
    }

    public void ShutDown()
    {
        if (_isInitialized)
        {
            _discordRpcClient.Dispose();
        }
    }

    public void UpdatePresence(SongsModelView song, TimeSpan duration, TimeSpan position)
    {
        if (song == currentSong)
            return;

        var Presence = new RichPresence()
        {
            Details = $"Listening to {song.Title}",
            State = $"by {song.ArtistName} | {song.AlbumName}",
            Buttons =
                [
                    new() { Label = "Try Dimmer !", Url = @"https://github.com/YBTopaz8/Dimmer-MAUI?tab=readme-ov-file#requirements"}
                ],
            Timestamps = new Timestamps()
            {
                Start = DateTime.UtcNow - position,
                End = DateTime.UtcNow + (duration - position)
            },
            Assets = new Assets()
            {
                LargeImageText = $"{song.Title} by {song.ArtistName}",
                LargeImageKey = "musical_notes",
                SmallImageKey = "jack-o-lantern",
                SmallImageText = "Boo 👻! Happy Halloween !"
            }
        };
#if ANDROID
return;
#endif
        if (!AppSettingsService.DiscordRPCPreference.GetDiscordRPC())
        {
            return;
        }
        else
        {
            this.Initialize();
            position = position.Add(TimeSpan.FromMilliseconds(500));
            var artName = string.IsNullOrEmpty(song.ArtistName) ? "Unknown Artist" : song.ArtistName;
            var albName = string.IsNullOrEmpty(song.AlbumName) ? "Unknown Album" : song.AlbumName;
            _discordRpcClient.SetPresence(Presence);
        }
        if (_isInitialized)
        {
            _discordRpcClient.UpdateState(Presence.State);
            _discordRpcClient.UpdateDetails(Presence.Details);
            _discordRpcClient.UpdateStartTime((DateTime)Presence.Timestamps.Start);
            _discordRpcClient.UpdateEndTime((DateTime)Presence.Timestamps.End);
            _discordRpcClient.UpdateLargeAsset(Presence.Assets.LargeImageKey, Presence.Assets.LargeImageText);
        }
    }
}
