using AudioSpectrumPlayer.Core.Services;
using AudioSpectrumPlayer.Core.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text;

namespace AudioSpectrumPlayer.WinUI.Views
{
	public sealed partial class LogDisplay : UserControl, ILogDisplayService
	{
		private readonly StringBuilder _logBuilder = new();
		private DispatcherQueue _dispatcherQueue;
		public LogViewModel ViewModel { get; private set; }

		public LogDisplay()
		{
			this.InitializeComponent();
			ViewModel = App.GetRequiredService<LogViewModel>();
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		}

		public void Log(string message)
		{
			if (string.IsNullOrEmpty(message))
				return;

			_logBuilder.Append(message);

			_dispatcherQueue?.TryEnqueue(() =>
			{
				ViewModel.Log(message);
				//logTextBox.Text = _logBuilder.ToString();

				// Auto-scroll to the bottom
				logTextBox.Select(logTextBox.Text.Length, 0);
				logTextBox.Focus(FocusState.Programmatic);
				scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
			});
		}
		public void Clear()
		{
			_logBuilder.Clear();
			logTextBox.Text = string.Empty;
		}

	}
}
