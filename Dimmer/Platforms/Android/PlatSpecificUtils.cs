﻿#if ANDROID
using Android.Graphics;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Color.Utilities;
using Microsoft.Maui.Controls;
using Color = Microsoft.Maui.Graphics.Color;
#endif

namespace Dimmer_MAUI.Platforms.Android;
public static class PlatSpecificUtils
{
    public static bool DeleteSongFile(SongsModelView song)
    {
        try
        {
            if (File.Exists(song.FilePath))
            {
                File.Delete(song.FilePath);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("An error occurred: " + ex.Message);
            return false;
        }
    }
    public static async Task<bool> MultiDeleteSongFiles(ObservableCollection<SongsModelView> songs)
    {
        try
        {
            
            foreach (var song in songs)
            {
                if (File.Exists(song.FilePath))
                {
                    File.Delete(song.FilePath);                    
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("An error occurred: " + ex.Message);
            return false;
        }
    }
    public static async Task<Color[]> GetDominantColorsAsync(Stream imageStream)
    {
#if ANDROID
        // Decode the stream to a bitmap
        Bitmap bitmap = await BitmapFactory.DecodeStreamAsync(imageStream);
        // Get the prominent color (for simplicity, we'll use the pixel at the center)
        int centerX = bitmap.Width / 2;
        int centerY = bitmap.Height / 2;
        int prominentColorInt = bitmap.GetPixel(centerX, centerY);
#endif
        // Convert to .NET MAUI color
        Color prominentColor = Color.FromUint((uint)prominentColorInt);
        // Generate a CorePalette based on the prominent color
        CorePalette corePalette = CorePalette.Of(prominentColorInt);
        // Using A1 (Primary Accent) and N1 (Primary Neutral)
        TonalPalette primaryAccentPalette = corePalette.A1;
        TonalPalette neutralPalette = corePalette.N1;
        // Extract tone colors from primary accent and neutral palette
        int primaryAccentColor = primaryAccentPalette.Tone(40); // Darker tone
        int neutralColor = neutralPalette.Tone(90); // Lighter tone
        // Convert the uint colors to .NET MAUI Colors
        var startColor = Color.FromRgb(
            (primaryAccentColor >> 16) & 0xFF, // Red component
            (primaryAccentColor >> 8) & 0xFF,  // Green component
            primaryAccentColor & 0xFF          // Blue component
        );
        var endColor = Color.FromRgb(
            (neutralColor >> 16) & 0xFF,       // Red component
            (neutralColor >> 8) & 0xFF,        // Green component
            neutralColor & 0xFF                // Blue component
        );
        return [startColor, endColor];
    }


    public static void ToggleWindowAlwaysOnTop(bool bof, nint nativeWindowHandle = 0)
    {
        Debug.WriteLine("Nothing"); ;
    }

    public static bool IsItemVisible (this CollectionView colView, object item)
    {
        var platformCollectionView = colView.Handler.PlatformView as RecyclerView;

        if (platformCollectionView == null)
            return false;

        var layoutManager = platformCollectionView.GetLayoutManager() as LinearLayoutManager;
        if (layoutManager == null)
            return false;

        var index = (colView.ItemsSource as System.Collections.IList).IndexOf(item);

        // Check if the item is within the visible range
        int firstVisibleItemPosition = layoutManager.FindFirstVisibleItemPosition();
        int lastVisibleItemPosition = layoutManager.FindLastVisibleItemPosition();

        return index >= firstVisibleItemPosition && index <= lastVisibleItemPosition;
    }
}