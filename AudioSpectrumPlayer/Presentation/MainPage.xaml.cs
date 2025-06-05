using AudioSpectrumPlayer.ViewModels;

namespace AudioSpectrumPlayer.Presentation;

public sealed partial class MainPage : Page
{
    public AudioPlayerViewModel ViewModel => (AudioPlayerViewModel)DataContext;
    public MainPage()
    {
        this.InitializeComponent();
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.Play();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.Pause();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.Stop();
    }

    private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Get window handle in Uno way
        // await ViewModel?.SelectAndLoadAudioFileAsync(hwnd);
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Clear log
    }
}
