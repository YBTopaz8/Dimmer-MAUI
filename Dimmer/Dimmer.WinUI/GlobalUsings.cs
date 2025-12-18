global using System.Reactive.Linq;
global using System.Diagnostics;
global using Dimmer.ViewModel;
global using Dimmer.WinUI.Utils;
global using System.Runtime.InteropServices;
global using Microsoft.UI.Windowing;
global using Microsoft.VisualBasic.FileIO;
global using System.Collections.ObjectModel;
global using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
global using Dimmer.WinUI.DimmerAudio;
global using Dimmer.WinUI.ViewModel;
global using Microsoft.Maui.LifecycleEvents;
global using Microsoft.UI;
global using WinRT.Interop;
global using System.Numerics;
global using WinUIVisibility = Microsoft.UI.Xaml.Visibility;
global using CommunityToolkit.Maui;

global using Dimmer.WinUI.Views.MAUIPages;
global using Dimmer.WinUI.Views.WinuiPages;
global using Xabe.FFmpeg;
global using WinUI.TableView;
global using Dimmer.Data;
global using Dimmer.DimmerSearch;
global using static Dimmer.WinUI.Utils.AppUtil;
global using Microsoft.UI.Composition;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Hosting;
global using Microsoft.UI.Xaml.Media;
global using Microsoft.UI.Xaml.Media.Animation;
global using Microsoft.UI.Xaml.Navigation;

global using Page = Microsoft.UI.Xaml.Controls.Page;
global using System.Windows.Media.Imaging;

global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Maui.Storage;
global using CommunityToolkit.Mvvm.Input;
global using Dimmer.Data.Models;
global using Dimmer.Data.ModelView.DimmerSearch;
global using Dimmer.DimmerSearch.TQL;
global using Dimmer.Interfaces;
global using Dimmer.Interfaces.IDatabase;
global using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
global using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
global using Dimmer.LastFM;
global using Dimmer.WinUI.Animations.UiComponentAnims;
global using Microsoft.Extensions.Logging;
global using Dimmer.Orchestration;
global using Dimmer.Utilities.Enums;
global using System.Threading.Tasks;
global using Dimmer.WinUI.Views;

global using IValueConverter = Microsoft.UI.Xaml.Data.IValueConverter;
global using Dimmer.WinUI.Utils.StaticUtils;
global using Microsoft.UI.Xaml.Input;
global using System.ComponentModel;

global using Dimmer.Data.ModelView;
#nullable enable // Enable nullable reference types for better compile-time safety

global using Dimmer.Utilities.Events; // Assuming PlaybackEventArgs, ErrorEventArgs are here
global using Microsoft.Maui.Controls;
global using Microsoft.UI.Dispatching; // For potential UI thread marshaling
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Runtime.CompilerServices;
global using System.Threading; // For CancellationToken
global using Windows.Devices.Enumeration; // For GetAvailableAudioOutputsAsync
global using Windows.Media;
global using Windows.Media.Core;
global using Windows.Media.Devices; // For GetAvailableAudioOutputsAsync, DefaultAudioRenderDeviceChangedEventArgs
global using Windows.Media.Playback;
global using Windows.Storage;
global using Windows.Storage.Streams;
global using MediaPlayer = Windows.Media.Playback.MediaPlayer;
global using Dimmer.Utilities;

global using System.Drawing;
global using Dimmer.DimmerLive.Interfaces;

global using Dimmer.Interfaces.Services;
global using Dimmer.WinUI.Utils.CustomHandlers.CollectionView;
global using Dimmer.WinUI.Utils.WinMgt;
global using Windows.ApplicationModel.DataTransfer;
global using Windows.Storage.Pickers;

global using BitmapImage = Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
global using Button = Microsoft.UI.Xaml.Controls.Button;
global using Colors = Microsoft.UI.Colors;
global using DataPackageOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation;
global using DragEventArgs = Microsoft.UI.Xaml.DragEventArgs;
global using Image = Microsoft.UI.Xaml.Controls.Image;
global using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
global using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
global using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

global using CommunityToolkit.Maui.Extensions;
global using CommunityToolkit.WinUI;

global using Dimmer.Utilities.Extensions;
global using Dimmer.WinUI.Views.CustomViews.MauiViews;


global using FieldType = Dimmer.DimmerSearch.TQL.FieldType;
global using MenuFlyoutSeparator = Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator;
global using MenuFlyoutSubItem = Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem;
global using TableView = WinUI.TableView.TableView;
global using Thickness = Microsoft.UI.Xaml.Thickness;
global using ToggleMenuFlyoutItem = Microsoft.UI.Xaml.Controls.ToggleMenuFlyoutItem;
namespace Dimmer.WinUI;

internal class GlobalUsings
{
}
