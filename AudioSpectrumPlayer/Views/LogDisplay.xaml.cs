using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace AudioSpectrumPlayer.Views
{
	public sealed partial class LogDisplay : UserControl
	{
		private DispatcherQueue _dispatcherQueue;
		public LogViewModel ViewModel { get; private set; }

		public LogDisplay()
		{
			InitializeComponent();
			ViewModel = App.GetRequiredService<LogViewModel>();
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
			_dispatcherQueue = DispatcherQueue;
		}

		public void Log(string message)
		{
			if (string.IsNullOrEmpty(message))
				return;

			_dispatcherQueue?.TryEnqueue(() =>
			{
				ViewModel.Log(message);
			});
		}
		public void Clear()
		{
			ViewModel.ClearLog();
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(LogViewModel.LogText))
			{
				_dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.Low, () =>
				{
					scrollViewer.UpdateLayout();
					scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
				});
			}
		}
	}
}
