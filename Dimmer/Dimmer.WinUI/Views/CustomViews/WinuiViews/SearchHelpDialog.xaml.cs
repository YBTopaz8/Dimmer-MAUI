using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

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

        //foreach (var g in groups)
        //{
        //    FieldsList.Children.Add(new TextBlock
        //    {
        //        Text = g.Key.ToString(),
        //        FontWeight = new Windows.UI.Text.FontWeight(1),
        //        Margin = new Thickness(0, 10, 0, 4)
        //    });

        //    foreach (var field in g)
        //    {
        //        FieldsList.Children.Add(new TextBlock
        //        {
        //            Text = $"{field.PrimaryName} ({string.Join(", ", field.Aliases)}) — {field.Description}",
        //            TextWrapping = TextWrapping.Wrap,
        //            Margin = new Thickness(8, 0, 0, 2)
        //        });
        //    }
        //}
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