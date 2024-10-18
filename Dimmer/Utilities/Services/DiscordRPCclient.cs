using DiscordRPC;


namespace Dimmer_MAUI.Utilities.Services;

public class DiscordRPCclient : IDiscordRPC
{
    private DiscordRpcClient _discordRpcClient;
    private bool _isInitialized = false;
    SongsModelView currentSong;
    public void Initialize()
    {
        if (!_isInitialized)
        {
            _discordRpcClient = new DiscordRpcClient(SecretFilesAndKeys.DiscordKey);
            _discordRpcClient.OnReady += DiscordRpcClient_OnReady;
            //_discordRpcClient.OnPresenceUpdate += DiscordRpcClient_OnPresenceUpdate;
            _discordRpcClient.Initialize();
            _isInitialized = true;
        }
    }

    private void DiscordRpcClient_OnPresenceUpdate(object sender, global::DiscordRPC.Message.PresenceMessage args)
    {
        Debug.WriteLine($"Update Presence Name = {args.Name}, Title = {args.Presence.Details}");
    }

    private void DiscordRpcClient_OnReady(object sender, global::DiscordRPC.Message.ReadyMessage args)
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
        if (!AppSettingsService.DiscordRPCPreference.GetDiscordRPC())
        {
            return;
        }
        else
        {
            this.Initialize();
        }
        if (_isInitialized)
        {
            if (song == currentSong)
                return;

            _discordRpcClient.SetPresence(new RichPresence()
            {
                Details = $"Listening to {song.Title}",
                State = $"by {song.ArtistName} | {song.AlbumName}",
                //Buttons = new DiscordRPC.Button()
                
                Timestamps = new Timestamps()
                {
                    Start = DateTime.UtcNow - position,
                    End = DateTime.UtcNow + (duration - position)
                },
                Assets = new Assets()
                {
                    LargeImageKey = song.CoverImagePath,
                    LargeImageText = "Dimmer by Yvan Brunel",
                    SmallImageKey = song.CoverImagePath,
                    SmallImageText = "Dimmer by Yvan Brunel (smol)",
                }
            });
        }
    }
}
