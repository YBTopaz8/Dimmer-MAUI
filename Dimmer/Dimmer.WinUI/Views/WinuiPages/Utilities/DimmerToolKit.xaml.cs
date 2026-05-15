using Dimmer.Data.ModelView.LibSanityModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CheckBox = Microsoft.UI.Xaml.Controls.CheckBox;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.Utilities;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DimmerToolKit : Page
{
    public DimmerToolKit()
    {
        InitializeComponent();
    }

    public BaseViewModelWin? MyViewModel { get; private set; }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel = e.Parameter as BaseViewModelWin;
        if (MyViewModel is null) return;
        this.DataContext = MyViewModel;
        if(MyViewModel.DuplicateSets.Count < 1)
        {
            _= MyViewModel.FindDuplicatesCommand.ExecuteAsync(true);
        }
        _composite = new();


        MyViewModel.WhenPropertyChange(
            nameof(MyViewModel.IsDuplicateFound),
            isBG => (MyViewModel.IsDuplicateFound))
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(
                async isDupFound =>
                {
                    if (isDupFound)
                    {
                        DuplicateScannedActionPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    }
                    else
                    {
                        DuplicateScannedActionPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    }

                });
    }

    private void StackPanel_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void BorderBrush_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
     

    }
    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _composite.Dispose();

    }
    CompositeDisposable _composite;

    private void ColorComboBox_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {

    }

    private void ActionComboBox_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        var OGItem = ((ComboBox)sender).DataContext as DuplicateItemViewModel;
        var choice = e.AddedItems[0] as string;
        if (OGItem is null) return;
        switch(choice)
        {
            case "Keep":
                OGItem.Action = DuplicateAction.Keep;
                break;
            case "Delete":
                OGItem.Action = DuplicateAction.Delete;
                break;
                OGItem.Action = DuplicateAction.Ignore;
            case "Do Nothing":

                break;

        }

        //MyViewModel
    }
}
