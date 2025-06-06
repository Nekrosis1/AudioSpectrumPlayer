using AudioSpectrumPlayer.Services;
using LibVLCSharp.Shared;
using Serilog;
//using Windows.Media.Playback;

namespace AudioSpectrumPlayer.ViewModels;

public partial class AudioPlayerViewModel : ObservableObject
{
    private LibVLCSharp.Shared.MediaPlayer _mediaPlayer = null!;
    private LibVLC? _libVLC;
    private DispatcherTimer _playbackTimer = null!;
    private readonly IAudioFileService _audioFileService;

    private readonly Guid _instanceId = Guid.NewGuid();

    // Add this property for debugging
    public string InstanceId => _instanceId.ToString()[..8];


    [ObservableProperty]
    private string _currentFilePath = string.Empty;
    [ObservableProperty]
    private TimeSpan _currentPosition;
    [ObservableProperty]
    private TimeSpan _totalDuration;
    [ObservableProperty]
    private string _windowTitle = "Audio Player";
    [ObservableProperty]
    private double _volume = 1.0;
    [ObservableProperty]
    private string? _mediaSource;

    public AudioPlayerViewModel(IAudioFileService audioFileService)
    {
        _audioFileService = audioFileService;
        InitializeTimer();
        InitializeLibVLC();
    }

    private void InitializeLibVLC()
    {
        try
        {
            Log.Debug("Initializing LibVLC");

            Core.Initialize();
            _libVLC = new LibVLC();

            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _mediaPlayer.SetRole(MediaPlayerRole.Music);
            _playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _playbackTimer.Tick += PlaybackTimer_Tick;

            _mediaPlayer.Playing += (sender, args) =>
            {
                Log.Debug("LibVLC: Media playing");
                _playbackTimer.Start();
            };

            _mediaPlayer.Paused += (sender, args) =>
            {
                Log.Debug("LibVLC: Media paused");
                _playbackTimer.Stop();
            };

            _mediaPlayer.Stopped += (sender, args) =>
            {
                Log.Debug("LibVLC: Media stopped");
                _playbackTimer.Stop();
                CurrentPosition = TimeSpan.Zero;
            };

            Log.Debug("LibVLC initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "InitializeLibVLC");
        }
    }

    private void PlaybackTimer_Tick(object? sender, object e)
    {
        try
        {
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                CurrentPosition = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                if (_mediaPlayer.Length > 0)
                {
                    TotalDuration = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PlaybackTimer_Tick");
        }
    }

    private void InitializeTimer()
    {
        try
        {
            Log.Debug("Initializing Timer");

            _playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _playbackTimer.Tick += PlaybackTimer_Tick;

            Log.Debug("Timer initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "InitializeTimer");
        }
    }


    //private void InitializePlaybackState()
    //{
    //    DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
    //    {
    //        if (_mediaPlayer.Source != null)
    //        {
    //            CurrentPosition = TimeSpan.Zero;
    //            TotalDuration = _mediaPlayer.PlaybackSession.NaturalDuration;

    //            if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
    //            {
    //                _playbackTimer.Start();
    //            }
    //            Log.Debug("Progress Bar Initialized");
    //        }
    //    });
    //}

    public void Play()
    {
        try
        {
            Log.Information($"Play called on instance {InstanceId}");
            Log.Information($"MediaPlayer is null: {_mediaPlayer == null}");
            Log.Information($"MediaPlayer.Media is null: {_mediaPlayer?.Media == null}");

            if (_mediaPlayer?.Media != null)
            {
                Log.Information($"Media duration: {_mediaPlayer.Media.Duration}ms");
                Log.Information($"Media state: {_mediaPlayer.Media.State}");
            }

            if (_mediaPlayer != null)
            {
                Log.Information("Playing audio");
                _mediaPlayer.Play();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Play");
        }
    }

    public void Pause()
    {
        try
        {
            if (_mediaPlayer != null)
            {
                Log.Information("Pausing audio");
                _mediaPlayer.Pause();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Pause");
        }
    }

    public void Stop()
    {
        try
        {
            if (_mediaPlayer != null)
            {
                Log.Information("Stopping audio");
                _mediaPlayer.Stop();
                CurrentPosition = TimeSpan.Zero;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Stop");
        }
    }


    partial void OnVolumeChanged(double value)
    {
        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)(value * 100);
                Log.Debug($"Volume changed to {(int)(value * 100)}%");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnVolumeChanged");
        }
    }

    public async Task<bool> SelectAndLoadAudioFileAsync(nint windowHandle)
    {
        try
        {
            string? filePath = await _audioFileService.PickAudioFileAsync(windowHandle);

            if (!string.IsNullOrEmpty(filePath))
            {
                await LoadAudioFileAsync(filePath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error selecting and loading audio file");
            return false;
        }
    }

    public async Task LoadAudioFileAsync(string filePath)
    {
        try
        {
            Log.Information($"Loading audio file: {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                Log.Error($"Error: File not found: {filePath}");
                return;
            }

            _currentFilePath = filePath;

            if (_mediaPlayer != null && _libVLC != null)
            {

                Media media = new(_libVLC, new Uri(filePath));
                //new Uri(filePath);
                media.ParsedChanged += (sender, args) =>
                {
                    Log.Debug($"Media parsed: {media.IsParsed}");
                    if (media.Duration > 0)
                    {
                        TotalDuration = TimeSpan.FromMilliseconds(media.Duration);
                        Log.Debug($"Media duration: {TotalDuration}");
                    }
                };

                await media.Parse(MediaParseOptions.ParseNetwork);
                _mediaPlayer.Media = media;
                WindowTitle = $"Audio Spectrum Player - {Path.GetFileName(filePath)}";

                Log.Information("Media source set successfully");
            }

            Log.Information("Media source set successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Load Audio File failed");
        }
    }

    public void SeekToPosition(double percentage)
    {
        try
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                float position = (float)percentage;
                _mediaPlayer.Position = position;

                CurrentPosition = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                Log.Debug($"Seeked to position: {FormatTimeSpan(CurrentPosition)}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SeekToPosition");
        }
    }
    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        return timeSpan.Hours > 0
            ? $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
            : $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }

    public void Dispose()
    {
        _playbackTimer?.Stop();
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
    }

}
