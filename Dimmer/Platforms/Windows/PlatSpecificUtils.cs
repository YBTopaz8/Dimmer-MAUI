using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualBasic.FileIO;
using DataTemplate = Microsoft.UI.Xaml.DataTemplate;
using System.Collections.Concurrent;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace Dimmer_MAUI.Platforms.Windows;
public static class PlatSpecificUtils
{

    public static IntPtr DimmerHandle { get; set; }
    public static bool DeleteSongFile(SongModelView song)
    {
        try
        {
            if (File.Exists(song.FilePath))
            {
                    FileSystem.DeleteFile(song.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

            }
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized to delete file: " + e.Message);
            return false;
        }
        catch (IOException e)
        {
            Debug.WriteLine("An IO exception occurred: " + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
            return false;
        }
    }

    public static bool MultiDeleteSongFiles(ObservableCollection<SongModelView> songs)
    {
        try
        {
            foreach (var song in songs)
            {
                if (File.Exists(song.FilePath))
                {
                    FileSystem.DeleteFile(song.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                }
            }
            
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized to delete file: " + e.Message);
            return false;
        }
        catch (IOException e)
        {
            Debug.WriteLine("An IO exception occurred: " + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
            return false;
        }
    }
       
    // Method to set the window on top
    public static void ToggleWindowAlwaysOnTop(bool topMost, AppWindowPresenter appPresenter)
    {
        try
        {

            var OverLappedPres = appPresenter as OverlappedPresenter;
            if (topMost)
            {
                OverLappedPres!.IsAlwaysOnTop = true;
            }
            else
            {
                OverLappedPres!.IsAlwaysOnTop = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}");
        }
    }

    public static void ToggleFullScreenMode(bool IsToFullScreen, AppWindowPresenter appPresenter)
    {
        try
        {
            var OverLappedPres = appPresenter as OverlappedPresenter;
            if (IsToFullScreen)
            {
                OverLappedPres!.IsAlwaysOnTop = true;
                OverLappedPres.SetBorderAndTitleBar(false, false);
                OverLappedPres!.Maximize();
            }
            else
            {
                OverLappedPres!.IsAlwaysOnTop = false;
                OverLappedPres.SetBorderAndTitleBar(true, true);
                OverLappedPres!.Restore();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}");
        }
    }
    public static class DataTemplateConversion
    {
        
        public static DataTemplate ConvertToWindowsDataTemplate(Microsoft.Maui.Controls.DataTemplate mauiTemplate)
        {
            if (mauiTemplate == null)
                return null;

            // Generate a unique id.
            string uniqueId = Guid.NewGuid().ToString();

            // Create XAML that instantiates our MauiDataTemplateHost with its Tag set to the unique id.
            string xaml =
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                "xmlns:local='using:YourNamespace'>" +
                $"<local:MauiDataTemplateHost Tag='{uniqueId}'/>" +
                "</DataTemplate>";

            // Load the WinUI DataTemplate.
            var winDataTemplate = (Microsoft.UI.Xaml.DataTemplate)XamlReader.Load(xaml);

            // Store the MAUI DataTemplate in the helper so that the host can retrieve it when loaded.
            MauiDataTemplateHostHelper.PendingTemplates[uniqueId] = mauiTemplate;

            return winDataTemplate;
        }
    }
    public static class MauiDataTemplateHostHelper
    {
        public static ConcurrentDictionary<string, Microsoft.Maui.Controls.DataTemplate> PendingTemplates { get; } = new ConcurrentDictionary<string, Microsoft.Maui.Controls.DataTemplate>();
    }

    public class MauiDataTemplateHost : ContentControl
    {
        public MauiDataTemplateHost()
        {
            this.Loaded += MauiDataTemplateHost_Loaded;
        }

        private void MauiDataTemplateHost_Loaded(object sender, RoutedEventArgs e)
        {
            // If Tag holds a unique id, use it to look up the pending MAUI DataTemplate.
            if (this.Tag is string id)
            {
                if (MauiDataTemplateHostHelper.PendingTemplates.TryRemove(id, out var mauiTemplate))
                {
                    MauiTemplate = mauiTemplate;
                }
            }
            ApplyMauiTemplate();
        }

        public Microsoft.Maui.Controls.DataTemplate MauiTemplate { get; set; }

        private void ApplyMauiTemplate()
        {
            if (MauiTemplate == null)
                return;

            // Create the MAUI view from the DataTemplate.
            var content = MauiTemplate.CreateContent();
            if (content is Microsoft.Maui.Controls.View mauiView)
            {
                // Get the current MauiContext from Application.Current.
                var mauiContext = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext;
                if (mauiContext == null)
                {
                    throw new InvalidOperationException("No MauiContext available.");
                }
                // Convert the MAUI view to a native WinUI view using the MauiContext.
                this.Content = mauiView.ToPlatform(mauiContext);
            }
        }
    }
}
