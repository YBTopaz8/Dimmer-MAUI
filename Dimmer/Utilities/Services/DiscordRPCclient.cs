using DiscordRPC;


namespace Dimmer_MAUI.Utilities.Services;

public class DiscordRPCclient : IDiscordRPC
{
    private DiscordRpcClient _discordRpcClient;
    bool isConnectionEstablished = false;   
    SongsModelView currentSong;

    public DiscordRPCclient()
    {
        Initialize();

    }

    public bool Initialize()
    {
#if ANDROID
return false;
#endif

        try
        {
            
            _discordRpcClient = new DiscordRpcClient(SecretFilesAndKeys.DiscordKey);
            _discordRpcClient.OnConnectionFailed += _discordRpcClient_OnConnectionFailed;
            _discordRpcClient.OnError += _discordRpcClient_OnError;
            
            _discordRpcClient.OnReady += DiscordRpcClient_OnReady;
            if (!_discordRpcClient.IsInitialized)
            {
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
            
        }
    }

    private void _discordRpcClient_OnError(object sender, DiscordRPC.Message.ErrorMessage args)
    {
        Debug.WriteLine("RPC On Error!!!");
    }

    private void _discordRpcClient_OnConnectionFailed(object sender, DiscordRPC.Message.ConnectionFailedMessage args)
    {
        this.ShutDown();
    }

    private void DiscordRpcClient_OnReady(object sender, DiscordRPC.Message.ReadyMessage args)
    {
        Debug.WriteLine($"RPC is ready and connected to {args.User.Username}");
    }

    public void ClearPresence()
    {
        if (_discordRpcClient.IsInitialized)
        {
            _discordRpcClient.ClearPresence();
        }
    }

    public void ShutDown()
    {
        if (_discordRpcClient.IsInitialized)
        {
            isConnectionEstablished = false;

            _discordRpcClient.Deinitialize();
            _discordRpcClient.OnConnectionFailed -= _discordRpcClient_OnConnectionFailed;
            _discordRpcClient.OnError -= _discordRpcClient_OnError;

            _discordRpcClient.OnReady -= DiscordRpcClient_OnReady;

            _discordRpcClient.Dispose();
        }
    }

    public void UpdatePresence(SongsModelView song, TimeSpan duration, TimeSpan position)
    {
        if(currentSong is not null)
        {
            if (song == currentSong)
                return;
        }

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
        if (_discordRpcClient.IsInitialized)
        {
            _discordRpcClient.UpdateState(Presence.State);
            _discordRpcClient.UpdateDetails(Presence.Details);
            _discordRpcClient.UpdateStartTime((DateTime)Presence.Timestamps.Start);
            _discordRpcClient.UpdateEndTime((DateTime)Presence.Timestamps.End);
            _discordRpcClient.UpdateLargeAsset(Presence.Assets.LargeImageKey, Presence.Assets.LargeImageText);
        }
        else
        {
            this.Initialize();
            position = position.Add(TimeSpan.FromMilliseconds(500));
            var artName = string.IsNullOrEmpty(song.ArtistName) ? "Unknown Artist" : song.ArtistName;
            var albName = string.IsNullOrEmpty(song.AlbumName) ? "Unknown Album" : song.AlbumName;
            _discordRpcClient.SetPresence(Presence);
        }
    }
}
