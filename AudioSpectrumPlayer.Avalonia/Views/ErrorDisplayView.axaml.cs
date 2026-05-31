using Avalonia.Controls;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class ErrorDisplayView : UserControl
{
	// DataContext is supplied by the host (MainWindow) just like the other controls,
	// so this binds against the same MainWindowViewModel instance.
	public ErrorDisplayView()
	{
		InitializeComponent();
	}
}
