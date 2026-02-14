using AudioSpectrumPlayer.Avalonia.ViewModels;
using Avalonia.Controls;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class MainWindow : Window
{
	// Parameterless constructor for Avalonia XAML loader / designer
	public MainWindow()
	{
		InitializeComponent();
	}

	public MainWindow(MainWindowViewModel viewModel) : this()
	{
		DataContext = viewModel;
	}
}
