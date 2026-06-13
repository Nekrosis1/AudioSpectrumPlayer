using AudioSpectrumPlayer.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Serilog;
using System;

namespace AudioSpectrumPlayer.Avalonia.Views;

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
		if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
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
					Margin = new global::Avalonia.Thickness(16),
					Spacing = 8,
					Children =
					{
						new TextBlock
						{
							Text = "Audio Spectrum Player v0.0.2\n\nAn open source audio player with frequency spectrum visualization.",
							TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
						},
						new TextBlock
						{
							Text = "https://github.com/Nekrosis1/AudioSpectrumPlayer",
							Foreground = global::Avalonia.Media.Brushes.SteelBlue,
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
