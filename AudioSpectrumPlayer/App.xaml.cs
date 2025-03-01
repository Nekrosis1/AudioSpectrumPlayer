using Microsoft.UI.Xaml;
using System;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AudioSpectrumPlayer
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window m_window;
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

            // Process file arguments if any were passed
            string[] launchArgs = Environment.GetCommandLineArgs();
            if (launchArgs.Length > 1)
            {
                string filePath = launchArgs[1];
                if (System.IO.File.Exists(filePath))
                {
                    // We need to get the StorageFile from the path
                    LoadAudioFileAsync(filePath);
                }
            }
        }

        private async void LoadAudioFileAsync(string filePath)
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                if (m_window is MainWindow mainWindow)
                {
                    // You'll need to add this method to MainWindow
                    await mainWindow.LoadAudioFileFromPathAsync(file);
                }
            }
            catch (Exception ex)
            {
                // Handle exception
            }
        }
    }
}
