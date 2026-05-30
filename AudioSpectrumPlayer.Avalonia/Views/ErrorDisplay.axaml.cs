using Avalonia.Controls;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class ErrorDisplay : UserControl
{
	// DataContext is supplied by the host (MainWindow) just like the other controls,
	// so this binds against the same MainWindowViewModel instance.
	public ErrorDisplay()
	{
		InitializeComponent();
	}
}
