using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Dimmer.CustomShellRenderers;
internal class PlayerViewModel : INotifyPropertyChanged
{
    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            // This is how we link the native sheet's state to our UI's appearance.
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(IsMiniPlayerVisible));
                OnPropertyChanged(nameof(IsFullPlayerVisible));
            }
        }
    }

    // These properties will drive the visibility of our layouts in XAML.
    public bool IsMiniPlayerVisible => !_isExpanded;
    public bool IsFullPlayerVisible => _isExpanded;

    // --- Communication Bridge ---
    // This event is for the ViewModel to tell the native UI what to do.
    public event EventHandler<bool> RequestSheetStateChange;

    // Call this from your MAUI UI (e.g., a tap gesture) to expand the sheet.
    public void TriggerExpand() => RequestSheetStateChange?.Invoke(this, true);

    // Call this from your MAUI UI (e.g., a "down arrow" button) to collapse.
    public void TriggerCollapse() => RequestSheetStateChange?.Invoke(this, false);


    // --- Your Player Logic ---
    // Example properties for a music player.
    public string CurrentTrackTitle { get; set; } = "The House of the Rising Sun";
    public string CurrentTrackArtist { get; set; } = "The Animals";
    public ICommand PlayPauseCommand { get; }
    public ICommand SkipNextCommand { get; }

    public PlayerViewModel()
    {
        // Example command logic
        PlayPauseCommand = new Command(() => System.Diagnostics.Debug.WriteLine("Play/Pause Tapped"));
        SkipNextCommand = new Command(() => System.Diagnostics.Debug.WriteLine("Skip Tapped"));
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}