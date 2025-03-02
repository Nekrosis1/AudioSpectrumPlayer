using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AudioSpectrumPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer;
        private string currentFilePath;
        public MainWindow()
        {
            this.InitializeComponent();
            InitializeMediaPlayer();
            MonitorWindowLifetime();
            Title = "Audio Player";

            LogViewer.Log("Application started");
            LogViewer.Log($"Current time: {DateTime.Now}");

            // Log command line arguments
            string[] args = Environment.GetCommandLineArgs();
            LogViewer.Log($"Command line args count: {args.Length}");
            for (int i = 0; i < args.Length; i++)
            {
                LogViewer.Log($"Arg[{i}]: {args[i]}");
            }
        }

        private void InitializeMediaPlayer()
        {
            try
            {
                FileLogger.Log("Initializing MediaPlayer");

                mediaPlayer = new MediaPlayer();
                mediaPlayer.MediaOpened += (sender, args) =>
                {
                    try
                    {
                        FileLogger.Log("Media opened successfully");
                    }
                    catch (Exception ex)
                    {
                        FileLogger.LogException(ex, "MediaOpened event");
                    }
                };

                mediaPlayer.MediaFailed += (sender, args) =>
                {
                    try
                    {
                        FileLogger.Log($"Media failed to load: {args.Error}");
                    }
                    catch (Exception ex)
                    {
                        FileLogger.LogException(ex, "MediaFailed event");
                    }
                };

                mediaPlayer.PlaybackSession.PlaybackStateChanged += (sender, args) =>
                {
                    try
                    {
                        FileLogger.Log($"Playback state changed to: {sender.PlaybackState}");
                    }
                    catch (Exception ex)
                    {
                        FileLogger.LogException(ex, "PlaybackStateChanged event");
                    }
                };

                FileLogger.Log("MediaPlayer initialized successfully");
            }
            catch (Exception ex)
            {
                FileLogger.LogException(ex, "InitializeMediaPlayer");
            }
        }

        public async Task LoadAudioFileFromPathDirectlyAsync(string filePath)
        {
            try
            {
                FileLogger.Log($"LoadAudioFileFromPathDirectlyAsync called with: {filePath}");

                // Use MediaSource.CreateFromUri instead of StorageFile
                var uri = new Uri(filePath);
                var mediaSource = MediaSource.CreateFromUri(uri);

                // Update UI on the dispatcher thread
                DispatcherQueue.GetForCurrentThread()?.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    try
                    {
                        FileLogger.Log("Setting media source via Uri");
                        mediaPlayer.Source = mediaSource;

                        // Update title and log
                        Title = $"Audio Player - {System.IO.Path.GetFileName(filePath)}";
                        LogViewer?.Log($"Audio file loaded: {System.IO.Path.GetFileName(filePath)}");

                        FileLogger.Log("Media source set successfully");
                    }
                    catch (Exception ex)
                    {
                        FileLogger.LogException(ex, "Setting media source on dispatcher");
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogger.LogException(ex, "LoadAudioFileFromPathDirectlyAsync");
            }
        }

        private void MonitorWindowLifetime()
        {
            try
            {
                // Get the window handle
                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

                // Log it
                FileLogger.Log($"Window handle: {windowHandle}");

                // Register for window messages using Win32 interop
                // This requires adding a reference to a Win32 message hook library or using PInvoke

                // For now, we can add some simple tracking
                AppWindow.Changed += (sender, args) =>
                {
                    try
                    {
                        if (sender != null)
                        {
                            FileLogger.Log($"Window state changed: {args}");
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.LogException(ex, "Window state change event");
                    }
                };

                // You can also try this if Closed event is available
                // this.Closed += (s, e) => FileLogger.Log("Window explicitly closed");

                FileLogger.Log("Window lifetime monitoring initialized");
            }
            catch (Exception ex)
            {
                FileLogger.LogException(ex, "MonitorWindowLifetime");
            }
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();

            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".mpeg");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".m4a");
            picker.FileTypeFilter.Add(".wma");
            picker.FileTypeFilter.Add(".aac");
            picker.FileTypeFilter.Add(".flac");
            picker.FileTypeFilter.Add(".ogg");
            picker.FileTypeFilter.Add(".aiff");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                LogViewer.Log($"File selected: {file.Path}");
                LoadAudioFile(file);
            }
            else
            {
                LogViewer.Log("File selection canceled or failed");
            }
        }

        private void LoadAudioFile(StorageFile file)
        {
            LogViewer.Log($"Loading audio file: {file.Path}");
            currentFilePath = file.Path;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            Title = $"Audio Player - {file.Name}";
            LogViewer.Log($"Audio file loaded: {file.Name}");
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                LogViewer.Log("Playing audio");
                mediaPlayer.Play();
            }
            else
            {
                LogViewer.Log("Cannot play: No audio file loaded");
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                LogViewer.Log("Pausing audio");
                mediaPlayer.Pause();
            }
            else
            {
                LogViewer.Log("Cannot pause: No audio file loaded");
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                LogViewer.Log("Stopping audio");
                mediaPlayer.Position = TimeSpan.Zero;
                mediaPlayer.Pause();
            }
            else
            {
                LogViewer.Log("Cannot stop: No audio file loaded");
            }
        }

        public async Task LoadAudioFileFromPathAsync(StorageFile file)
        {
            FileLogger.Log("Trying to Dispatch");
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            FileLogger.Log("Dispatched");
            bool success = dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                LoadAudioFile(file);
                FileLogger.Log("File Loaded");
                Debug.WriteLine("File Loaded");
            });
            if (!success)
            {
                // Fallback handling if the dispatch fails
                await Task.Run(() =>
                {
                    FileLogger.Log("File Load failed");
                    Debug.WriteLine("File Load failed");
                    dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                    {
                        LoadAudioFile(file);
                    });
                });
            }
        }
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogViewer.Clear();
            LogViewer.Log("Log cleared");
        }
    }
}
