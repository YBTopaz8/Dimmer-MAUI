// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class SearchHelpDialog : ContentDialog
{
    public SearchHelpDialog()
    {
        InitializeComponent();
        LoadFields();
        LoadExamples();
        LoadNaturalHints();
    }

    void LoadFields()
    {
        var groups = FieldRegistry.AllFields.GroupBy(f => f.Type);

        List<(string Key, List<string> Description)> fields = new List<(string Key, List<string> Description)>();

        foreach (var g in groups)
        {
            fields.Add((g.Key.ToString(), g.Select(f => $"{f.PrimaryName} ({string.Join(", ", f.Aliases)}) — {f.Description}").ToList()));
            
        }

        FieldsList.ItemsSource = fields;
    }

    void LoadExamples()
    {
        var examples = new[]{
            "favorite songs → fav:true",
            "songs by eminem → artist:Eminem",
            "rock songs → genre:rock",
            "released in 2020 → year:2020",
            "longer than 5 minutes → duration:>5m",
            "added yesterday → added:yesterday"
        };

        foreach (var ex in examples)
            ExamplesList.Children.Add(new TextBlock
            {
                Text = ex,
                Margin = new Thickness(0, 3, 0, 3)
            });
    }

    void LoadNaturalHints()
    {
        var hints = new[]{
            "Play Adele songs",
            "Show songs with lyrics",
            "Unplayed tracks",
            "Top rated songs",
            "Instrumental only"
        };

        foreach (var h in hints)
            NaturalList.Items.Add(h);
    }
}