using AudioSpectrumPlayer.Services;
using AudioSpectrumPlayer.ViewModels;
using Serilog;

namespace AudioSpectrumPlayer.Presentation;

public sealed partial class MainPage : Page
{
    public AudioPlayerViewModel ViewModel { get; }
    public MainPage()
    {
        this.InitializeComponent();

        ViewModel = App.GetRequiredService<AudioPlayerViewModel>();

        // Set it as the DataContext
        this.DataContext = ViewModel;
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
        try
        {
            // In Uno Platform, you might need to handle this differently per platform
            // For now, let's try a simple approach
            var audioFileService = App.GetRequiredService<IAudioFileService>();
            if (audioFileService != null)
            {
                // You'll need to modify IAudioFileService to work without window handle for Uno
                var filePath = await audioFileService.PickAudioFileAsync(IntPtr.Zero);
                if (!string.IsNullOrEmpty(filePath))
                {
                    await ViewModel.LoadAudioFileAsync(filePath);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in OpenFileButton_Click");
        }
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Clear log
    }
}
