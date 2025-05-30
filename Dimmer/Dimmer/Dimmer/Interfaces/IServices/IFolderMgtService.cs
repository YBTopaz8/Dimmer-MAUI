
namespace Dimmer.Interfaces;
public partial class FolderModel : ObservableObject
{
    [ObservableProperty]
    public partial string FolderName { get; set; }
    [ObservableProperty]
    public partial string FolderPath { get; set; }
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }
    [ObservableProperty]
    public partial bool IsChecked { get; set; }
}
public interface IFolderMgtService : IDisposable
{
    Task AddFolderToWatchListAndScanAsync(string path);
    Task ClearAllWatchedFoldersAndRescanAsync();
    Task RemoveFolderFromWatchListAsync(string path);
}
