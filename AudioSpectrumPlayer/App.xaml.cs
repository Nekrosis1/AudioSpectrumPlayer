using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using Windows.Storage;

namespace AudioSpectrumPlayer
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window m_window;
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();


            string[] launchArgs = Environment.GetCommandLineArgs();
            Debug.WriteLine($"Total args: {launchArgs.Length}");
            for (int i = 0; i < launchArgs.Length; i++)
            {
                Debug.WriteLine($"Arg[{i}]: {launchArgs[i]}");
            }
            if (launchArgs.Length > 1)
            {
                Debug.WriteLine("Going to check for args");
                //string filePath = launchArgs[1];
                string filePath = "C:\\Users\\Nekrosis\\Downloads\\Ductos.mp3";
                if (System.IO.File.Exists(filePath))
                {
                    Debug.WriteLine("arg found, file path:");
                    Debug.WriteLine(filePath);
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
                    await mainWindow.LoadAudioFileFromPathAsync(file);
                }
            }
            catch (Exception)
            {
                // TODO: handle ex
            }
        }
    }
}
