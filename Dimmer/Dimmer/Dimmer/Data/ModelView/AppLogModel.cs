namespace Dimmer.Data.ModelView;
public class AppLogModel
{
    public string Log { get; set; } = string.Empty;
    public SongModel? AppSongModel { get; set; } 
    public SongModelView? ViewSongModel { get; set; }
    public UserModelView? UserModel { get; set; }
    public UserDeviceSession? DeviceModelSession { get; set; }
    public ChatMessage? ChatMsg { get; set; }
    public ChatConversation? ChatConvo { get; set; }
    public DimmerSharedSong? SharedSong { get; set; }
    public AppScanLogModel? AppScanLogModel { get; set; } 
}


public class AppScanLogModel
{
    public int TotalFiles { get; set; }
    public int CurrentFilePosition { get; set; }
    
}