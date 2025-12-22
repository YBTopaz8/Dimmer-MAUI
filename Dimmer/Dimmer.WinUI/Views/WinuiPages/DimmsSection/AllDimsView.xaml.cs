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

using static Dimmer.DimmerSearch.TQlStaticMethods;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.DimmsSection;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AllDimsView : Page
{
    public AllDimsView()
    {
        InitializeComponent();
    }

    BaseViewModelWin MyViewModel;

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel  = e.Parameter as BaseViewModelWin;
        
        this.DataContext = MyViewModel;

        MyViewModel.LoadPlayEvents();
    }
    private void MoreBtn_Click(object sender, RoutedEventArgs e)
    {
        var MoreBtn = (Button)sender;
        var moreBtnFlyout = new MenuFlyout()
            ;


        var addNoteToSongMFItem = new MenuFlyoutItem { Text = "Add Note to Song" };
        FontIcon iconNote = new FontIcon();
        iconNote.Glyph = "\uF7BB";
        addNoteToSongMFItem.Icon = iconNote;

        moreBtnFlyout.Items.Add(addNoteToSongMFItem);

        FontIcon LoveIcon = new FontIcon();
        LoveIcon.Glyph = "\uEB51";
        var menuItemOne = new MenuFlyoutItem()
        {
            Text = "Love",
            Icon = LoveIcon
        };
        moreBtnFlyout.Items.Add(menuItemOne);


        FontIcon EditIcon = new FontIcon();
        EditIcon.Glyph = "\uE70F";
        var menuItemTwo = new MenuFlyoutItem()
        {
            Text = "Edit",
            Icon = EditIcon
        };

        //moreBtnFlyout.Items.Add(menuItemTwo);


        FontIcon Deleteicon = new FontIcon();
        Deleteicon.Glyph = "\uE74D";
        var menuItemThree = new MenuFlyoutItem()
        {
            Text = "Delete",
            Icon = Deleteicon
            ,
            
          
        };
        

        moreBtnFlyout.Items.Add(menuItemThree);

        FontIcon moreSubSectionicon = new FontIcon();
        moreSubSectionicon.Glyph = "\uE713";
        MenuFlyoutSubItem moreSubSection = new MenuFlyoutSubItem();
        moreSubSection.Text = "More";
        moreSubSection.Icon = moreSubSectionicon;

        FontIcon SearchIcon = new FontIcon();
        SearchIcon.Glyph = "\uE721";
        var searchFlyoutItem = new MenuFlyoutItem()
        { Text = "Search", Icon = SearchIcon };

        FontIcon Shareicon = new FontIcon();
        Shareicon.Glyph = "\uE72D";
        var ShareFlyoutItem = new MenuFlyoutItem()
        { Text = "Share",Icon= Shareicon }; 
        moreSubSection.Items.Add(searchFlyoutItem);
        moreSubSection.Items.Add(ShareFlyoutItem);


        FontIcon BlockIcon = new FontIcon();
        BlockIcon.Glyph = "\uF140";

        MenuFlyoutSubItem BlockSubSection = new MenuFlyoutSubItem();
        BlockSubSection.Text = "Block";
        BlockSubSection.Icon = BlockIcon;

        FontIcon Trackicon = new FontIcon();
        Trackicon.Glyph = "\uEC4F";

        var blockTrackFlyoutItem = new MenuFlyoutItem()
        { Text = "Track",Icon= Trackicon };

        FontIcon blockArtisticon = new FontIcon();
        blockArtisticon.Glyph = "\uE720";
        
        var blockArtistMenuFlyout= new MenuFlyoutItem()
        { Text = "Artist",Icon=blockArtisticon };


        FontIcon blockAlbumMFicon = new FontIcon();
        blockAlbumMFicon.Glyph = "\uE93C";
        var blockAlbumMF = new MenuFlyoutItem()
        { Text = "Album",Icon= blockAlbumMFicon };
        BlockSubSection.Items.Add(blockTrackFlyoutItem);
        BlockSubSection.Items.Add(blockArtistMenuFlyout);
        BlockSubSection.Items.Add(blockAlbumMF);



        moreBtnFlyout.Items.Add(moreSubSection);
        moreSubSection.Items.Add(BlockSubSection);

        FlyoutShowOptions flyoutShowOpt = new FlyoutShowOptions
        {
            Placement = FlyoutPlacementMode.Auto,
            ShowMode = FlyoutShowMode.Auto
        };
        moreBtnFlyout.ShowAt(MoreBtn, flyoutShowOpt);
    }

    private void MoreBtn_Click_1(object sender, RoutedEventArgs e)
    {

    }

    SongModelView? trackModel = null;
    Microsoft.UI.Xaml.Controls.Button? SongTitlebutton;
    private void SongTitle_Click(object sender, RoutedEventArgs e)
    {
        SongTitlebutton = sender as Button;
        var evt = SongTitlebutton?.DataContext as DimmerPlayEventView;
        trackModel = evt.SongViewObject;
        MyViewModel.SelectedSong = trackModel;

        AnimationHelper.Prepare(AnimationHelper.Key_ListToDetail, SongTitlebutton);

        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        Type songDetailType = typeof(SongDetailPage);
        var navParams = new SongDetailNavArgs
        {
            Song = trackModel,
            ViewModel = MyViewModel,
        };

        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };

        Frame?.NavigateToType(songDetailType, navParams, navigationOptions);
    }

    private async void MyEventsTableView_Loaded(object sender, RoutedEventArgs e)
    {
        if (MyViewModel.SelectedSong != null)
        {
            trackModel = MyViewModel.SelectedSong;
            // CLEAN: Handles scrolling, updating layout, finding the image, and starting animation
            await AnimationHelper.TryStartListReturn(
                 MyEventsTableView,
                 trackModel,
                 "coverArtImage",
                 AnimationHelper.Key_DetailToList
             );

            trackModel = null;
        }
    }

    private async void SongArtists_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var evt = btn?.DataContext as DimmerPlayEventView;
        if (evt != null)
        {
            var songModelView = evt.SongViewObject;
            var songInDb = MyViewModel.RealmFactory.GetRealmInstance()
                .Find<SongModel>(evt.SongId);

            try
            {

                var nativeElement = (Microsoft.UI.Xaml.UIElement)sender;
               

                // Navigate to the detail page, passing the selected song object.
                // Suppress the default page transition to let ours take over.
                var supNavTransInfo = new SuppressNavigationTransitionInfo();
                Type pageType = typeof(ArtistPage);
                var navParams = new SongDetailNavArgs
                {
                    Song = songModelView!,
                    ExtraParam = MyViewModel,
                    ViewModel = MyViewModel
                };
                var contextMenuFlyout = new MenuFlyout();

                var dbSongArtists = MyViewModel.RealmFactory.GetRealmInstance();
                var dbSong = dbSongArtists
                    .Find<SongModel>(songModelView!.Id);
                if (dbSong is null) return;
                if ((dbSong.ArtistToSong.Count < 1 || dbSong.Artist is null))
                {

                    var ArtistsInSong = songModelView.OtherArtistsName.
                    Split(",").ToList();
                    await MyViewModel.AssignArtistToSongAsync(songModelView!.Id,
                         ArtistsInSong);


                }
                var selectingg = dbSong.ArtistToSong.ToList();
                var sel2 = selectingg.Select(x => new ArtistModelView()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Bio = x.Bio,
                    ImagePath = x.ImagePath
                });
                var namesOfartists = sel2.Select(a => a.Name);

                bool isSingular = namesOfartists.Count() > 1 ? false : true;
                string artistText = string.Empty;
                if (isSingular)
                {
                    artistText = "artist";
                }
                else
                {
                    artistText = "artists";
                }
                contextMenuFlyout.Items.Add(
                    new MenuFlyoutItem
                    {
                        Text = $"{namesOfartists.Count()} {artistText} linked",
                        IsTapEnabled = false

                    });

                foreach (var artistName in namesOfartists)
                {
                    var root = new MenuFlyoutItem { Text = artistName };

                    root.Click += async (obj, routedEv) =>
                    {

                        var songContext = ((MenuFlyoutItem)obj).Text;

                        var selectedArtist = MyViewModel.RealmFactory.GetRealmInstance()
                        .Find<SongModel>(songModelView.Id).ArtistToSong.First(x => x.Name == songContext)
                        .ToArtistModelView();


                        var nativeElementMenuFlyout = (Microsoft.UI.Xaml.UIElement)obj;
                        
                        await MyViewModel.SetSelectedArtist(selectedArtist);


                        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
                        {
                            TransitionInfoOverride = supNavTransInfo,
                            IsNavigationStackEnabled = true

                        };
                        AnimationHelper.PrepareFromChild(
         sender as DependencyObject,
         "ArtistNameTxt",
         AnimationHelper.Key_Forward
     );

                        Frame?.NavigateToType(pageType, navParams, navigationOptions);
                    };

                    contextMenuFlyout.Items.Add(root);
                }


                try
                {
                    if (namesOfartists.Count() > 1)
                    {
                        contextMenuFlyout.ShowAt(nativeElement, showOptions:new FlyoutShowOptions()
                        { ShowMode = FlyoutShowMode.Auto, Placement = FlyoutPlacementMode.Right});
                    }
                    else
                    {

                        var selectedArtist = MyViewModel.RealmFactory.GetRealmInstance()
                        .Find<SongModel>(songModelView.Id).ArtistToSong.First()
                        .ToArtistModelView();
                        if (selectedArtist is null) return;
                        await MyViewModel.SetSelectedArtist(selectedArtist);


                        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
                        {
                            TransitionInfoOverride = supNavTransInfo,
                            IsNavigationStackEnabled = true

                        };
                        AnimationHelper.PrepareFromChild(
         sender as DependencyObject,
         "ArtistNameTxt",
         AnimationHelper.Key_Forward
     );

                        Frame?.NavigateToType(pageType, navParams, navigationOptions);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MenuFlyout.ShowAt failed: {ex.Message}");
                    // fallback: anchor without position
                    //flyout.ShowAt(nativeElement);
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }


        }
    }

    private void SongAlbum_Click(object sender, RoutedEventArgs e)
    {

    }
}
