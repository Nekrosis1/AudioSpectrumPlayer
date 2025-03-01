using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
                LoadAudioFile(file);
            }
        }

        private void LoadAudioFile(StorageFile file)
        {
            currentFilePath = file.Path;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            Title = $"Audio Player - {file.Name}";
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Play();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Pause();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Position = TimeSpan.Zero;
                mediaPlayer.Pause();
            }
        }

        public async Task LoadAudioFileFromPathAsync(StorageFile file)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    LoadAudioFile(file);
                });
        }
    }
}
