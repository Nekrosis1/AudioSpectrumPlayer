using Avalonia.Controls;
using Serilog;

namespace AudioSpectrumPlayer.Avalonia.Views;

// Dumb view: the slider and time text bind directly to MainWindowViewModel
// (ProgressValue / TimeDisplay). The two-way slider binding drives seeking, so
// there is no code-behind state, event wiring, or seek-suppression here anymore.
public partial class PlaybackProgressControlView : UserControl
{
	public PlaybackProgressControlView()
	{
		InitializeComponent();
		Log.Debug("PlaybackProgressControlView INIT");
	}
}
