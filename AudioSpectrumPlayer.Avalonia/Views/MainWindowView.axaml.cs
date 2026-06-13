using AudioSpectrumPlayer.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Serilog;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class MainWindowView : Window
{
	public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

	// Parameterless constructor for Avalonia XAML loader / designer
	public MainWindowView()
	{
		InitializeComponent();

		// Media-player shortcuts. Handled in the TUNNEL phase (window sees the key
		// before any focused child) so the arrow keys aren't swallowed by the progress
		// slider. The handlers just forward to the ViewModel commands — the actual logic
		// stays in the VM. Space is deliberately NOT handled here: it's the standard
		// "activate the focused control" key, so it toggles whatever the user has tabbed
		// to (and the play/pause button by default — see below), like VLC and other apps.
		AddHandler(KeyDownEvent, OnShortcutKeyDown, RoutingStrategies.Tunnel);

		// Default keyboard focus to the play/pause button so Space toggles playback when
		// the user hasn't tabbed elsewhere. Tabbing to another control then makes Space
		// activate that control instead.
		Loaded += (_, _) => PlayPauseButton.Focus();
	}

	private void OnShortcutKeyDown(object? sender, KeyEventArgs e)
	{
		if (ViewModel is null)
		{
			return;
		}

		// Don't hijack ordinary typing/navigation inside a text field (e.g. the log).
		// Modifier combos (Ctrl+O) still pass through.
		if (e.Source is TextBox && e.KeyModifiers == KeyModifiers.None)
		{
			return;
		}

		bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

		switch (e.Key)
		{
			case Key.O when ctrl:
				ViewModel.OpenFileCommand.Execute(null);
				break;
			case Key.L when ctrl:
				ViewModel.ToggleLogVisibilityCommand.Execute(null);
				break;
			case Key.Right:
				ViewModel.SeekForwardCommand.Execute(null);
				break;
			case Key.Left:
				ViewModel.SeekBackwardCommand.Execute(null);
				break;
			case Key.OemPlus:
			case Key.Add:
				ViewModel.VolumeUpCommand.Execute(null);
				break;
			case Key.OemMinus:
			case Key.Subtract:
				ViewModel.VolumeDownCommand.Execute(null);
				break;
			default:
				return; // not one of ours — leave the event unhandled
		}

		e.Handled = true;
	}

	public MainWindowView(MainWindowViewModel viewModel) : this()
	{
		DataContext = viewModel;
		Log.Information("MainWindowView constructed with ViewModel");
		Closed += (_, _) => Log.Debug("Window closed");
	}
}
