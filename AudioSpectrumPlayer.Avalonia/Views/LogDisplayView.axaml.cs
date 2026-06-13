using AudioSpectrumPlayer.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using System.ComponentModel;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class LogDisplayView : UserControl
{
	public LogViewModel ViewModel { get; }

	public LogDisplayView()
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
