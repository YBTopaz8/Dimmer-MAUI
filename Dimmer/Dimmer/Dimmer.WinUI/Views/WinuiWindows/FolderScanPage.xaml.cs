using System.Numerics;

using Dimmer.WinUI.Utils.Models;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.WinuiWindows;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

using Vanara.Extensions.Reflection;

using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Composition;

using Button = Microsoft.UI.Xaml.Controls.Button;
using Page = Microsoft.UI.Xaml.Controls.Page;
using SpringVector3NaturalMotionAnimation = Microsoft.UI.Composition.SpringVector3NaturalMotionAnimation;
using Window = Microsoft.UI.Xaml.Window;
namespace Dimmer.WinUI.Views.WinuiWindows;
sealed partial class FolderScanPage : Page
{
    SpringVector3NaturalMotionAnimation? _springAnimation;
    private ISettingsWindowManager settingsWindow;
    private BaseViewModel baseViewModel;

    public List<FolderModel> ListOfFolders { get; private set; }

    public FolderScanPage()
    {
        InitializeComponent();
    }
    private void CreateOrUpdateSpringAnimation(float finalValue)
    {
        if (_springAnimation == null)
        {
            Microsoft.UI.Composition.Compositor _compositor = Microsoft.UI.Xaml.Media.CompositionTarget.GetCompositorForCurrentThread();


            _springAnimation = _compositor.CreateSpringVector3Animation();
            _springAnimation.Target = "Scale";
        }

        _springAnimation.FinalValue = new Vector3(finalValue);
    }

    private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(1.5f);

        (sender as UIElement).StartAnimation(_springAnimation);
    }

    private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(1.0f);

        (sender as UIElement).StartAnimation(_springAnimation);
    }
    private void MyStackLayout_Loaded(object sender, RoutedEventArgs e)
    {

        this.settingsWindow = IPlatformApplication.Current.Services.GetService<ISettingsWindowManager>();
        this.baseViewModel =IPlatformApplication.Current.Services.GetService<BaseViewModel>();
        DataContext = baseViewModel;
        Microsoft.UI.Composition.Compositor _compositor = Microsoft.UI.Xaml.Media.CompositionTarget.GetCompositorForCurrentThread();
        var anim = _compositor.CreateExpressionAnimation();
        anim.Expression = "(above.Scale.Y - 1) * 50 + above.Translation.Y % (50 * index)";
        anim.Target = "Translation.Y";

        anim.SetExpressionReferenceParameter("above", Btn1);
        anim.SetScalarParameter("index", 1);
        Btn2.StartAnimation(anim);

        anim.SetExpressionReferenceParameter("above", Btn2);
        anim.SetScalarParameter("index", 2);
        Btn3.StartAnimation(anim);

        anim.SetExpressionReferenceParameter("above", Btn3);
        anim.SetScalarParameter("index", 3);
        Btn4.StartAnimation(anim);

    }

    List<FolderModelTwo> listOfFoldersTwo = new List<FolderModelTwo>();
    private async void AddFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var senderButton = sender as Button;
        senderButton.IsEnabled = false;

        ListOfFolders ??= new List<FolderModel>();

        FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

        var window = settingsWindow.InstanceWindow;

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add("*");

        StorageFolder folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {

            var fold = new FolderModel
            {
                FolderName = folder.Name,
                Path= folder.Path,
                DateCreated=folder.DateCreated,
                DisplayName=folder.DisplayName
            };
            var foldTwo = new FolderModelTwo
            {
                FolderName = folder.Name,
                Path= folder.Path,
                DateCreated=folder.DateCreated,
                DisplayName=folder.DisplayName
            };

            ListOfFolders.Add(fold);
            //ListOfFoldersView.ItemsSource = null;

            listOfFoldersTwo.Add(foldTwo);
            //ListOfFoldersView.ItemsSource = listOfFoldersTwo;
            await baseViewModel.AddMusicFolderAsync(folder.Path);
        }



        senderButton.IsEnabled = true;

    }
}



