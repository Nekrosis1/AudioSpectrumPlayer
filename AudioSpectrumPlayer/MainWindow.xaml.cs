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
            mediaPlayer = new MediaPlayer();
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
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            bool success = dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                LoadAudioFile(file);
                Debug.WriteLine("File Loaded, yay");
            });
            if (!success)
            {
                // Fallback handling if the dispatch fails
                await Task.Run(() =>
                {
                    Debug.WriteLine("File Load failed, i am in fail");
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
