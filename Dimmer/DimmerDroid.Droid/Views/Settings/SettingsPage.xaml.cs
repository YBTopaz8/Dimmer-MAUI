global using Chip = DevExpress.Maui.Editors.Chip;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace Dimmer.Views.Settings;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(BaseViewModelAnd viewModelAnd,
    LastFMViewModel lastFMVM)
    {
        InitializeComponent();
        BindingContext = viewModelAnd;
        MyViewModel = viewModelAnd;
        MyLastFMViewModel = lastFMVM;
        pageDisposable = new();

    }

    BaseViewModelAnd MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (pageDisposable is null)
        {
            pageDisposable = new();

        }
        MyViewModel.LoadFolderPaths();
    }
    private void RemoveFolderBtn_Clicked(object sender, HandledEventArgs e)
    {
        var chip = (Chip)sender;
        var folderPath = (string)chip.Text;
        MyViewModel.DeleteFolderPath(folderPath);
    }
    protected override bool OnBackButtonPressed()
    {
        GoBackBtn_Clicked(this, new EventArgs());
        return base.OnBackButtonPressed();
    }
    private async void BackupDeviceBtn_Clicked(object sender, EventArgs e)
    {
      _=  MyViewModel.BackUpAppDataAsync();

    }

    private async void RestoreBackupDeviceBtn_Clicked(object sender, EventArgs e)
    {
        FilePickedAndRestoreInProgressActIndic.IsVisible = true;
        FilePickedAndRestoreInProgressActIndic.IsRunning = true;
        await  MyViewModel.PickFolderToRestoreAppDataAsync();

    }


    private void ConnectToLastFM_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        if (send is null) return;
        if (MyViewModel is null) return;


            MyLastFMViewModel.LastFMName = LastFMUname.Text;
        //LoginLastFMBtn.IsEnabled = false;
        MyLastFMViewModel?.LoginToLastfmCommand.Execute(null);

    }

    LastFMViewModel MyLastFMViewModel;
    private async void ConfirmRestoreBtn_Clicked(object sender, EventArgs e)
    {
        FilePickedAndRestoreInProgressActIndic.IsVisible = true;
        FilePickedAndRestoreInProgressActIndic.IsRunning = true;
        await MyViewModel.RestoreCompleteDataAsync();


    }

    private async void GoBackBtn_Clicked(object sender, EventArgs e)
    {
        
    }


    private void FetchLyricsData_Click(object sender, EventArgs e)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        _ = Task.Run(async () => await MyViewModel.LoadAllSongsLyricsFromOnlineAsync(cts));
    }


    private async void OpenFolderScannerBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddMusicFolderViaPickerAsync();
    }


    private async void RescanFolderChip_Tap(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var path = send.BindingContext as string;
        if (path != null)
        {
           await MyViewModel.ReScanMusicFolderByPassingToService(path);
        }
    }
    CompositeDisposable pageDisposable;

    private void ConfirmRestoreBtn_Loaded(object sender, EventArgs e)
    {
        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.PickedUpBackup), v => (MyViewModel.PickedUpBackup))
          .Subscribe(
              e =>
              {
                  if (e is null || e.PlayEvents is null || e.PlayEvents.Count <1)
                  {
                      ConfirmRestoreBtn.IsVisible = false;
                  }
                  else if(e.PlayEvents.Count >0)
                  {
                      ConfirmRestoreBtn.IsVisible = true;

                      FilePickedAndRestoreInProgressActIndic.IsVisible = false;
                      FilePickedAndRestoreInProgressActIndic.IsRunning = false;
                  }
              })
          .DisposeWith(pageDisposable);

        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.IsRestoreDone), v => (MyViewModel.IsRestoreDone))
          .Subscribe(
              e =>
              {
                  if (e)
                  {
                      ConfirmRestoreBtn.IsVisible = false;
                      FilePickedAndRestoreInProgressActIndic.IsVisible = false;
                      FilePickedAndRestoreInProgressActIndic.IsRunning = false;
                  }
                  else
                  {

                  }
              })
          .DisposeWith(pageDisposable);


    }
}