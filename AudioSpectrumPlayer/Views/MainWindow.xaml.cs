using AudioSpectrumPlayer.Interfaces;
using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using System;

namespace AudioSpectrumPlayer.Views
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window
	{
		private readonly DispatcherQueue _uiDispatcher;
		public MainWindowViewModel ViewModel { get; }
		public IAudioStateService _audioStateService;
		public MainWindow(MainWindowViewModel viewModel)
		{
			InitializeComponent();
			ViewModel = viewModel;
			_audioStateService = App.GetRequiredService<IAudioStateService>();
			_audioStateService.PlaybackStateChanged += OnPlaybackStateChanged;
			_uiDispatcher = DispatcherQueue;
			MonitorWindowLifetime();
			Log.Information("Application started");
		}

		private void MonitorWindowLifetime()
		{
			try
			{
				var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

				Log.Debug($"Window handle: {windowHandle}");

				// Register for window messages using Win32 interop
				// This requires adding a reference to a Win32 message hook library or using PInvoke
				AppWindow.Changed += (sender, args) =>
				{
					try
					{
						if (sender != null)
						{
							Log.Debug($"Window state changed: {args}");
						}
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Window state change event");
					}
				};

				this.Closed += (s, e) => Log.Debug("Window explicitly closed");

				Log.Debug("Window lifetime monitoring initialized");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "MonitorWindowLifetime");
			}
		}

		#region UI Buttons


		private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.TogglePlayPause();
		}
		private void OnPlaybackStateChanged(object? sender, bool isPlaying)
		{
			_uiDispatcher?.TryEnqueue(() =>
			{
				PlayPauseIcon.Glyph = isPlaying ? "\uE769" : "\uE768";
			});
		}

		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Stop();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "All exceptions get handled")]
		private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
		{
			nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
			await ViewModel.SelectAndLoadAudioFileAsync(hwnd);
		}
		#endregion

		private Visibility ConvertBoolToVisibility(bool isVisible)
		{
			return isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
