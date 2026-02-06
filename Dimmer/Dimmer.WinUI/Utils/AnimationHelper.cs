using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Visibility = Microsoft.UI.Xaml.Visibility;

namespace Dimmer.WinUI.Utils;

public static class AnimationHelper
{
    // Define your keys here to avoid "Magic Strings"
    public const string Key_ListToDetail = "ForwardConnectedAnimation";
    public const string Key_DetailToList = "BackConnectedAnimation";
    public const string Key_ToViewQueue = "ViewNowPlayingQueueAnim";
    public const string Key_NowPlayingPage = "ViewNowPlayingPage";
    public const string Key_ToViewSingleSongPopUp = "ViewSingleSongPopup";
    public const string Key_ArtistToSong = "ArtistToSongDetailsAnim";
    public const string Key_Forward = "ForwardConnectedAnimation";
    public const string Key_Back = "BackConnectedAnimation";

    public const string Key_DetailToEdit = "SwingFromSongDetailToEdit";
    /// <summary>
    /// Prepares the animation BEFORE navigation (Call this on Click)
    /// Can also be called in OnNavigatedTo so as to prepare the objects before they're loaded
    /// </summary>
    public static void Prepare(string key, UIElement? source, bool isList = false)
    {
        if (source == null) return;
        var service = ConnectedAnimationService.GetForCurrentView();

        // If it's a list item, we might want to ensure the service knows that, 
        // though usually passing the specific Image/Element inside the list item is best.
        service.PrepareToAnimate(key, source);
    }

    /// <summary>
    /// Starts the animation on the Destination Page.
    /// Handles Dispatcher, Configuration, and Coordinated Elements automatically.
    /// </summary>
    public static void TryStart(UIElement destination, IEnumerable<UIElement>? coordinatedElements = null, params string[] potentialKeys)
    {
        // Use the Dispatcher to wait for Layout to finish
        destination.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            var service = ConnectedAnimationService.GetForCurrentView();
            ConnectedAnimation? animation = null;

            // 1. Find the first valid animation from the list of keys provided
            foreach (var key in potentialKeys)
            {
                animation = service.GetAnimation(key);
                if (animation != null) break;
            }

            if (animation == null) return;

            // 2. Configure (Shadows, Gravity)
            var config = new GravityConnectedAnimationConfiguration { IsShadowEnabled = true };
            animation.Configuration = config;

            // 3. Setup Destination Opacity (Good practice to ensure it doesn't flicker)
            destination.Opacity = 1;
            destination.Visibility = Visibility.Visible;

            // 4. Start
            if (coordinatedElements != null)
            {
                animation.TryStart(destination, coordinatedElements);
            }
            else
            {
                animation.TryStart(destination);
            }
        });
    }

    /// <summary>
    /// Specialized method for returning to a ListView/GridView/TableView
    /// </summary>
    public static async Task TryStartListReturn(ListViewBase listView, object itemToScrollTo, string childImageName, string key)
    {
        if (itemToScrollTo == null) return;

        var service = ConnectedAnimationService.GetForCurrentView();
        var animation = service.GetAnimation(key);

        // If there is no animation coming in, don't bother scrolling or doing work.
        if (animation == null) return;

        // 1. Start the scroll
        listView.ScrollIntoView(itemToScrollTo, ScrollIntoViewAlignment.Default);

        // 2. Force a layout pass immediately
        listView.UpdateLayout();

        // 3. Enter a Retry Loop to find the container
        // We try 10 times with a 20ms delay (total approx 200ms wait max)
        FrameworkElement? container = null;
        int retries = 0;
        const int maxRetries = 10;

        while (container == null && retries < maxRetries)
        {
            // Check if container exists yet
            container = listView.ContainerFromItem(itemToScrollTo) as FrameworkElement;

            if (container != null) break;

            // If not, wait a tiny bit for the UI thread to catch up with Virtualization
            await Task.Delay(25);
            retries++;
        }

        // 4. Final check
        if (container != null)
        {
            // We found the container! Now find the image.
            // Assuming PlatUtils is your helper class
            var image = PlatUtils.FindChildOfType<Image>(container, childImageName);

            if (image != null)
            {
                // Prepare image for arrival
                image.Opacity = 1;
                image.Visibility = Visibility.Visible; // Ensure it's not collapsed

                var config = new GravityConnectedAnimationConfiguration { IsShadowEnabled = true };
                animation.Configuration = config;

                // Execute
                animation.TryStart(image);
                return;
            }
            else
            {
                // Fallback: Animate to the whole row
                animation.TryStart(container);
                return;
            }
        }

        // 5. If we waited and it's STILL null, cancel gracefully
        animation.Cancel();
    }


    /// <summary>
    /// Scenario 2 (Source): Prepares animation from an item inside a ListView/TableView/GridView.
    /// Automatically finds the Container and the specific Child (e.g., Image).
    /// </summary>
    public static void PrepareFromList(ItemsControl listControl, object item, string childName, string key)
    {
        // 1. Find the Row/Container
        var container = listControl.ContainerFromItem(item) as FrameworkElement;
        if (container == null) return; // Item might be off-screen

        // 2. Find the specific Image/TextBlock inside that row
        var sourceElement = PlatUtils.FindChildOfType<FrameworkElement>(container, childName);
        if (sourceElement == null) return;

        // 3. Prepare
        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(key, sourceElement);
    }

    /// <summary>
    /// Scenario 3 (Source): Prepares animation from a named child inside the sender (e.g. TextBlock inside a Button).
    /// </summary>
    public static void PrepareFromChild(DependencyObject? parent, string childName, string key)
    {
        var child = PlatUtils.FindChildOfType<FrameworkElement>(parent, childName);
        if (child != null)
        {
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(key, child);
        }
    }


}