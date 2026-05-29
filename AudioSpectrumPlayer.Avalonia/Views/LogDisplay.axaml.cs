using AudioSpectrumPlayer.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using System.ComponentModel;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class LogDisplay : UserControl
{
	public LogViewModel ViewModel { get; }

	public LogDisplay()
	{
		InitializeComponent();
		ViewModel = App.GetRequiredService<LogViewModel>();
		DataContext = ViewModel;
		ViewModel.PropertyChanged += OnViewModelPropertyChanged;
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(LogViewModel.LogText))
			return;

		Dispatcher.UIThread.Post(() =>
		{
			scrollViewer.ScrollToEnd();
		}, DispatcherPriority.Background);
	}
}
