namespace Dimmer_MAUI.Utilities.IServices;

public interface IDiscordRPC
{
    bool Initialize();
    void UpdatePresence(SongModelView song, TimeSpan duration, TimeSpan position); //add a pause method
    void ClearPresence();
    void ShutDown();
}
