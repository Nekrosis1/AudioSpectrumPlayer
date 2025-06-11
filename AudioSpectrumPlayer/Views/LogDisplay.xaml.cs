using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AudioSpectrumPlayer.Views
{
	public sealed partial class LogDisplay : UserControl
	{
		//private readonly StringBuilder _logBuilder = new();
		private DispatcherQueue _dispatcherQueue;
		public LogViewModel ViewModel { get; private set; }

		public LogDisplay()
		{
			InitializeComponent();
			ViewModel = App.GetRequiredService<LogViewModel>();
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		}

		public void Log(string message)
		{
			if (string.IsNullOrEmpty(message))
				return;

			_dispatcherQueue?.TryEnqueue(() =>
			{
				ViewModel.Log(message);
				ScrollToBottom();
			});
		}
		public void Clear()
		{
			ViewModel.ClearLog();
		}
		private void ScrollToBottom()
		{
			logTextBox.Select(logTextBox.Text.Length, 0);
			logTextBox.Focus(FocusState.Programmatic);
			scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
		}

	}
}
