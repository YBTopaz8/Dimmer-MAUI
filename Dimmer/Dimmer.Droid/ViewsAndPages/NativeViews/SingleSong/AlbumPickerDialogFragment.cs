using Google.Android.Material.Dialog;

using DialogFragment = AndroidX.Fragment.App.DialogFragment;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

internal class AlbumPickerDialogFragment:DialogFragment
{
    private List<string> AlbumNames;
    private string CurrentAlbumName;
    private BaseViewModelAnd MyViewModel;
    public AlbumPickerDialogFragment(List<string> albumNames, string currentAlbumName
        , BaseViewModelAnd myViewModel)
    {
        AlbumNames = albumNames;
        CurrentAlbumName = currentAlbumName;
        MyViewModel = myViewModel;
        
        var enterTransition = new AndroidX.Transitions.TransitionSet()
            .AddTransition(new AndroidX.Transitions.Fade((int)FadingMode.In))
            .SetDuration(300);
        var exitTransition = new AndroidX.Transitions.TransitionSet()
            .AddTransition(new AndroidX.Transitions.Fade((int)FadingMode.Out))
            .SetDuration(300);
        EnterTransition = enterTransition;
        ExitTransition = exitTransition;
        //SharedElementEnterTransition = new AndroidX.Transitions.ChangeBounds().SetDuration(300);
        //SharedElementReturnTransition = new AndroidX.Transitions.ChangeBounds().SetDuration(300);
    }

    public override Android.App.Dialog OnCreateDialog(Android.OS.Bundle? savedInstanceState)
    {
        var builder = new MaterialAlertDialogBuilder(Activity);
        builder.SetTitle("Select New Album");
        
        var albumNamesArray = AlbumNames.ToArray();
        int currentIndex = AlbumNames.IndexOf(CurrentAlbumName);
        builder.SetSingleChoiceItems(albumNamesArray, currentIndex, async (sender, args) =>
        {
            // Handle album selection
            string selectedAlbum = albumNamesArray[args.Which];

            await MyViewModel.AssignSongToAlbumAsync((MyViewModel.SelectedSong,selectedAlbum) );
            // You can pass this selected album back to the ViewModel or activity as needed
            Dismiss();
        });
        builder.SetNegativeButton("Cancel", (sender, args) =>
        {
            Dismiss();
        });
        return builder.Create();
    }

    public override void OnCancel(Android.Content.IDialogInterface dialog)
    {
        base.OnCancel(dialog);
    }
    public override void OnDismiss(Android.Content.IDialogInterface dialog)
    {
        base.OnDismiss(dialog);
    }

    
}
