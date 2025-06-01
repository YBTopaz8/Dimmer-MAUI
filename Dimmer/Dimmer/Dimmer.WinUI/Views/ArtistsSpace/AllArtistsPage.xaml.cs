using System.Numerics;

using Dimmer.Data.Models;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.WinUI.Utils.Models;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.WinuiWindows;
using Dimmer.WinUI.Views.WinuiWindows;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Animation;

using Vanara.Extensions.Reflection;

using Windows.Foundation.Metadata;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Composition;

using ArtistModel = Dimmer.WinUI.Utils.Models.ArtistModelWin;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Page = Microsoft.UI.Xaml.Controls.Page;
using SpringVector3NaturalMotionAnimation = Microsoft.UI.Composition.SpringVector3NaturalMotionAnimation;
using Window = Microsoft.UI.Xaml.Window;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.ArtistsSpace;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AllArtistsPage : Page
{
    public AllArtistsPage()
    {
        InitializeComponent();


        BaseViewModel viewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModel>()!;
        ViewModel=viewModel;
        ArtistsPage.DataContext=ViewModel;
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        LoadArtists();

    }
    List<ArtistModelView>? Artists { get; set; }
    List<SongModelView>? ArtistsSongs { get; set; }
    public void LoadArtists()
    {
        IMapper _mapper = IPlatformApplication.Current!.Services.GetService<IMapper>()!;
        IRepository<Data.Models.ArtistModel> artistsRepo = IPlatformApplication.Current!.Services.GetService<IRepository<Dimmer.Data.Models.ArtistModel>>()!;

        var arts = artistsRepo.GetAll().ToList();


        Artists = _mapper.Map<List<ArtistModelView>>(arts);
        collection.ItemsSource = Artists;

    }
    public BaseViewModel ViewModel { get; }
    public ArtistModelView _storedArtist { get; set; }

    SpringVector3NaturalMotionAnimation _springAnimation;
    private int previousSelectedIndex;
    List<SelectorBarItem> ListOfArtists = new List<SelectorBarItem>();

    private async void collection_Loaded(object sender, RoutedEventArgs e)
    {

        if (_storedArtist != null)
        {
            if (collection.ItemsSource is null)
            {
                return;
            }
            // If the connected item appears outside the viewport, scroll it into view.
            collection.ScrollIntoView(_storedArtist, ScrollIntoViewAlignment.Default);
            collection.UpdateLayout();

            // Play the second connected animation.
            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("BackConnectedAnimation");
            if (animation != null)
            {
                // Setup the "back" configuration if the API is present.
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
                {
                    animation.Configuration = new DirectConnectedAnimationConfiguration();
                }

                await collection.TryStartConnectedAnimationAsync(animation, _storedArtist, "connectedElement");
            }

            // Set focus on the list
            collection.Focus(FocusState.Programmatic);
        }
    }

    private void collection_ItemClick(object sender, ItemClickEventArgs e)
    {
        // Get the collection item corresponding to the clicked item.
        if (collection.ContainerFromItem(e.ClickedItem) is ListViewItem container)
        {
            // Stash the clicked item for use later. We'll need it when we connect back from the detailpage.
            _storedArtist = container.Content as ArtistModelView;

            // Prepare the connected animation.
            // Notice that the stored item is passed in, as well as the name of the connected element.
            // The animation will actually start on the Detailed info page.
            collection.PrepareConnectedAnimation("ForwardConnectedAnimation", _storedArtist, "connectedElement");
        }

        // Navigate to the DetailedInfoPage.
        // Note that we suppress the default animation.
        Dictionary<string, object> parameters = new Dictionary<string, object>
        {
            { "artist", _storedArtist },
            { "frame", Frame }

        };
        Frame.Navigate(typeof(SpecificArtistPage), parameters, new SuppressNavigationTransitionInfo());
    }

    private void SelectorBar2_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        SelectorBarItem selectedItem = sender.SelectedItem;
        int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        System.Type pageType;

        switch (currentSelectedIndex)
        {
            case 0:
                pageType = typeof(AllArtistsPage);
                break;
            case 1:
                pageType = typeof(SpecificArtistPage);
                break;

            default:
                pageType = typeof(AllArtistsPage);
                break;
        }

        var slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        previousSelectedIndex = currentSelectedIndex;
    }
}


