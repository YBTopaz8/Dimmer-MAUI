using Dimmer.Data.Models;
using Dimmer.Interfaces;
using Dimmer.Utilities.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerAudio;
public partial class AudioService : IDimmerAudioService, INotifyPropertyChanged, IAsyncDisposable
{
    public bool IsPlaying => throw new NotImplementedException();

    public double CurrentPosition => throw new NotImplementedException();

    public double Duration => throw new NotImplementedException();

    public double Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public event EventHandler<PlaybackEventArgs> IsPlayingChanged;
    public event EventHandler<PlaybackEventArgs> PlayEnded;
    public event EventHandler PlayPrevious;
    public event EventHandler PlayNext;
    public event EventHandler<double>? PositionChanged;
    public event EventHandler<double>? DurationChanged;
    public event EventHandler<double>? SeekCompleted;
    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    public event EventHandler<PlaybackEventArgs>? ErrorOccurred;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public Task<List<AudioOutputDevice>> GetAvailableAudioOutputsAsync()
    {
        throw new NotImplementedException();
    }

    public Task InitializeAsync(SongModel songModel, byte[]? SongCoverImage)
    {
        throw new NotImplementedException();
    }

    public Task PauseAsync()
    {
        throw new NotImplementedException();
    }

    public Task PlayAsync()
    {
        throw new NotImplementedException();
    }

    public Task SeekAsync(double positionSeconds)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync()
    {
        throw new NotImplementedException();
    }
}
