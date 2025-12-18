namespace Dimmer.Utilities.ViewsUtils;
// A simple enum to make our view modes clear
public enum SongViewMode
{
    SimpleList,
    DetailedGrid
}

public class SongTemplateSelector : DataTemplateSelector
{
    // These properties will be set from XAML
    public DataTemplate PlayingTemplate { get; set; }
    public DataTemplate LyricsAvailableTemplate { get; set; }
    public DataTemplate DefaultTemplate { get; set; }

    private static int _callCount = 0;

    // This is the core method that MAUI calls for each item
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        _callCount++;
        Debug.WriteLine($"[TemplateSelector] OnSelectTemplate called. Total calls: {_callCount}");

        // The 'container' is the CollectionView. Its BindingContext is our ViewModel!
        var viewModel = container.BindingContext as BaseViewModel;
        if (item is not SongModelView song)
            return DefaultTemplate;

        // 2. Apply our logic in order of priority.
        if (song.IsCurrentPlayingHighlight)
        {
            return PlayingTemplate;
        }

        if (song.HasLyrics)
        {
            return LyricsAvailableTemplate;
        }

        // 3. If no other condition was met, return the default.
        return DefaultTemplate;
    }
}
