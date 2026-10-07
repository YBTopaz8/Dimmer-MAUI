using DevWinUI;
using Dimmer.Data.ModelView;
using Dimmer.WinUI.Utils;
using Dimmer.WinUI.ViewModel.SingleSongVMSection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Dimmer.WinUI.Views.WinuiPages.SingleSongPage;

public sealed partial class EditSongPage : UserControl
{
    public EditSongViewModel? ViewModel => DataContext as EditSongViewModel;

    public SongModelView? DetailedSong
    {
        get => (SongModelView?)GetValue(DetailedSongProperty);
        set => SetValue(DetailedSongProperty, value);
    }

    public static readonly DependencyProperty DetailedSongProperty =
        DependencyProperty.Register(
            nameof(DetailedSong),
            typeof(SongModelView),
            typeof(EditSongPage),
            new PropertyMetadata(null, OnDetailedSongChanged));

    public EditSongPage()
    {
        InitializeComponent();
        Loaded += OnEditSongPageLoaded;
    }

    private static void OnDetailedSongChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EditSongPage page && e.NewValue is SongModelView song)
        {
            page.InitializeViewModel(song);
        }
    }

    private void OnEditSongPageLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext == null && DetailedSong != null)
        {
            InitializeViewModel(DetailedSong);
        }
    }

    private void InitializeViewModel(SongModelView song)
    {
        if (IPlatformApplication.Current == null) return;

        var baseVm = IPlatformApplication.Current.Services.GetRequiredService<BaseViewModelWin>();
        DataContext = new EditSongViewModel(baseVm, song);
    }

    public event EventHandler IsBackBtnClicked;
    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        IsBackBtnClicked?.Invoke(this, EventArgs.Empty);
        //// If your host FlipSide has a way to flip back, call it or find parent FlipSide
        //var parentFlipSide = VisualTreeHelperExtensions.FindAscendant<FlipSide>(this);
        //if (parentFlipSide != null)
        //{
        //    parentFlipSide.IsFlipped = false;
        //}
    }

    private void DetailedImage_Loaded(object sender, RoutedEventArgs e)
    {
        AnimationHelper.TryStart(
            detailedImage,
            new List<UIElement> { BackBtn },
            "SwingFromSongDetailToEdit",
            AnimationHelper.Key_ListToDetail,
            AnimationHelper.Key_ArtistToSong
        );
    }

    private void ArtistSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && ViewModel != null)
        {
            ViewModel.FilterArtistSuggestionsCommand.Execute(sender.Text);
        }
    }

    private void ArtistSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (ViewModel == null) return;

        var artistName = args.ChosenSuggestion as string ?? sender.Text;
        if (!string.IsNullOrWhiteSpace(artistName))
        {
            ViewModel.AddArtistCommand.Execute(artistName);
            sender.Text = string.Empty;
        }
    }
}