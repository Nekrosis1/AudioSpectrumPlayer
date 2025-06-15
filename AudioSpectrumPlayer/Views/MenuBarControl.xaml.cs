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
		private readonly LogViewModel _logViewModel;
		public MainWindowViewModel ViewModel => (DataContext as MainWindowViewModel)!;
		public MenuBarControl()
		{
			InitializeComponent();
			_logViewModel = App.GetRequiredService<LogViewModel>();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "All exceptions get handled")]
		private async void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// Get the main window handle
			var mainWindow = App.GetRequiredService<MainWindow>();
			nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

			await ViewModel.SelectAndLoadAudioFileAsync(hwnd);
			Log.Information("File opened via menu");
		}

		private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Log.Information("Exit requested via menu");
			Application.Current.Exit();
		}

		private void ShowLogMenuItem_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ToggleLogVisibility();
			Log.Information("Show log requested via menu");
		}

		private void ClearLogMenuItem_Click(object sender, RoutedEventArgs e)
		{
			_logViewModel.ClearLog();
			Log.Information("Log cleared via menu");
		}

		private void PreferencesMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Implement preferences dialog
			Log.Information("Preferences requested (not implemented yet)");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Handler only calls ShowAsync as async")]
		private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
		{
			TextBlock content = new TextBlock
			{
				TextWrapping = TextWrapping.Wrap
			};

			content.Inlines.Add(new Run
			{
				Text = "Audio Spectrum Player v0.0.2\n\nAn open source Audio player with frequency spectrum visualization.\n\n"
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
