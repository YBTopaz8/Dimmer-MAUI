global using System.Reactive.Linq;
global using System.Diagnostics;
global using AutoMapper;
global using CommunityToolkit.Mvvm.ComponentModel;
global using Dimmer.ViewModel;
global using Dimmer.Utilities.CustomAnimations;
global using Dimmer.WinUI.Views;
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
global using CommunityToolkit.Maui.Core.Extensions;

global using Dimmer.Orchestration;
global using Dimmer.Utilities.Enums;
global using System.Threading.Tasks;

global using Dimmer.WinUI.Views.ArtistsSpace.MAUI;

global using Syncfusion.Maui.Toolkit.Chips;
global using Syncfusion.Maui.Toolkit.EffectsView;

global using Dimmer.WinUI.Utils.StaticUtils;
global using Microsoft.UI.Xaml.Input;
global using System.ComponentModel;
global using Dimmer.WinUI.Views.SingleSongPages;

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
global using Dimmer.WinUI.Utils.Helpers;
global using Dimmer.WinUI.Views.SettingsCenter;

global using System.Drawing;
global using System.Reflection;
global using Vanara.PInvoke;
global using Dimmer.DimmerLive.Interfaces;

global using CommunityToolkit.Mvvm.Input;
global using Dimmer.Interfaces.Services;
