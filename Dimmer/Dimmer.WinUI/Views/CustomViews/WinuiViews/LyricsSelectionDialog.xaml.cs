using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class LyricsSelectionDialog : ContentDialog
{
    private const int MaxSelection = 5;
    private readonly List<string> _allLyrics;
    private readonly List<CheckBox> _checkBoxes = new();

    public List<string> SelectedLyrics { get; private set; } = new();

    public LyricsSelectionDialog(List<string> allLyrics)
    {
        _allLyrics = allLyrics;
        InitializeComponent();
        SetupLyrics();

        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
    }

    private void SetupLyrics()
    {
        var items = new List<CheckBox>();

        foreach (var lyric in _allLyrics)
        {
            var checkBox = new CheckBox
            {
                Content = lyric,
                Margin = new Thickness(0, 4, 0, 4),
                MaxWidth = 550
            };
            checkBox.Checked += OnCheckBoxChanged;
            checkBox.Unchecked += OnCheckBoxChanged;
            _checkBoxes.Add(checkBox);
            items.Add(checkBox);
        }

        LyricsRepeater.ItemsSource = items;
        UpdateSelectionCount();
    }

    private void OnCheckBoxChanged(object sender, RoutedEventArgs e)
    {
        var checkedCount = _checkBoxes.Count(cb => cb.IsChecked == true);

        // If we've reached max selection, disable unchecked boxes
        if (checkedCount >= MaxSelection)
        {
            foreach (var cb in _checkBoxes.Where(cb => cb.IsChecked != true))
            {
                cb.IsEnabled = false;
            }
        }
        else
        {
            // Re-enable all checkboxes
            foreach (var cb in _checkBoxes)
            {
                cb.IsEnabled = true;
            }
        }

        UpdateSelectionCount();
    }

    private void UpdateSelectionCount()
    {
        var count = _checkBoxes.Count(cb => cb.IsChecked == true);
        SelectionCountText.Text = $"{count} / {MaxSelection} lines selected";
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Collect selected lyrics
        SelectedLyrics = _checkBoxes
            .Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Content.ToString())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList()!;
    }

    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedLyrics = new List<string>();
    }
}
