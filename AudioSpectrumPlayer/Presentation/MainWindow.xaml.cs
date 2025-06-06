using AudioSpectrumPlayer.ViewModels;

namespace AudioSpectrumPlayer.Presentation;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public AudioPlayerViewModel ViewModel { get; }
    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = App.GetRequiredService<AudioPlayerViewModel>();

        // Set DataContext for the window and the page
        //this.DataContext = ViewModel;
        MainPageContent.DataContext = ViewModel;

        // Bind window title
        this.Title = ViewModel.WindowTitle;
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AudioPlayerViewModel.WindowTitle))
            {
                this.Title = ViewModel.WindowTitle;
            }
        };
    }
}
