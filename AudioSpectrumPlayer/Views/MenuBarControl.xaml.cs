using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Serilog;
using System;


namespace AudioSpectrumPlayer.Views
{
	public sealed partial class MenuBarControl : UserControl
	{
		public AudioPlayerViewModel? ViewModel => DataContext as AudioPlayerViewModel;
		public MenuBarControl()
		{
			InitializeComponent();
		}

		private async void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null)
			{
				// Get the main window handle
				var mainWindow = App.GetRequiredService<MainWindow>();
				nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

				await ViewModel.SelectAndLoadAudioFileAsync(hwnd);
				Log.Information("File opened via menu");
			}
		}

		private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Log.Information("Exit requested via menu");
			Application.Current.Exit();
		}

		private void ShowLogMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// Toggle log visibility - you might want to implement this in your ViewModel
			var mainWindow = App.GetRequiredService<MainWindow>();
			// For now, just scroll to the log area
			Log.Information("Show log requested via menu");
		}

		private void ClearLogMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//var mainWindow = App.GetRequiredService<MainWindow>();
			//mainWindow.LogDisplay.Clear();
			//mainWindow.LogDisplay.Log("Log cleared via menu");
			Log.Information("Log cleared via menu");
		}

		private void PreferencesMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Implement preferences dialog
			Log.Information("Preferences requested (not implemented yet)");
		}

		private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//HyperlinkButton githubLink = new HyperlinkButton
			//{
			//	Content = "View on GitHub",
			//	NavigateUri = new Uri("https://github.com/Nekrosis1/AudioSpectrumPlayer"),
			//	Margin = new Thickness(0, 8, 0, 0)
			//};
			//ContentDialog aboutDialog = new ContentDialog
			//{
			//	Title = "About Audio Spectrum Player",
			//	Content = $"Audio Spectrum Player v0.0.1\n\nAn open source Audio player with frequency spectrum visualization.\n\n{githubLink}",
			//	CloseButtonText = "OK",
			//	XamlRoot = this.XamlRoot
			//};


			TextBlock content = new TextBlock
			{
				TextWrapping = TextWrapping.Wrap
			};

			content.Inlines.Add(new Run
			{
				Text = "Audio Spectrum Player v0.0.1\n\nAn open source Audio player with frequency spectrum visualization.\n\n"
			});

			// Add the clickable link inline
			Hyperlink githubLink = new Hyperlink
			{
				NavigateUri = new Uri("https://github.com/Nekrosis1/AudioSpectrumPlayer")
			};
			githubLink.Inlines.Add(new Run { Text = "View on GitHub" });
			content.Inlines.Add(githubLink);

			ContentDialog aboutDialog = new ContentDialog
			{
				Title = "About Audio Spectrum Player",
				Content = content,
				CloseButtonText = "OK",
				XamlRoot = this.XamlRoot
			};




			await aboutDialog.ShowAsync();
		}
	}
}
