using AudioSpectrumPlayer.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Serilog;
using System;
using System.Reflection;

namespace AudioSpectrumPlayer.Views;

public partial class MenuBarControlView : UserControl
{
	public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

	public MenuBarControlView()
	{
		InitializeComponent();
	}

	private void OnExitClick(object? sender, RoutedEventArgs e)
	{
		Log.Information("Exit requested via menu");
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.Shutdown();
		}
	}

	private void OnPreferencesClick(object? sender, RoutedEventArgs e)
	{
		// TODO: Implement preferences dialog
		Log.Information("Preferences requested (not implemented yet)");
	}

	private async void OnAboutClick(object? sender, RoutedEventArgs e)
	{
		try
		{
			if (TopLevel.GetTopLevel(this) is not Window window) return;

			Window dialog = new()
			{
				Title = "About Audio Spectrum Player",
				Width = 360,
				Height = 200,
				CanResize = false,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Content = new StackPanel
				{
					Margin = new Thickness(16),
					Spacing = 8,
					Children =
					{
						new TextBlock
						{
							Text = $"Audio Spectrum Player v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}\n\nAn open source audio player with frequency spectrum visualization.",
							TextWrapping = TextWrapping.Wrap,
						},
						new TextBlock
						{
							Text = "https://github.com/Nekrosis1/AudioSpectrumPlayer",
							Foreground = Brushes.SteelBlue,
						},
					},
				},
			};

			await dialog.ShowDialog(window);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "About dialog failed");
		}
	}
}
