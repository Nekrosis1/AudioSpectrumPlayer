using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text;

namespace AudioSpectrumPlayer
{
	public sealed partial class LogDisplay : UserControl
	{
		private readonly StringBuilder _logBuilder = new();
		private DispatcherQueue _dispatcherQueue;

		public LogDisplay()
		{
			this.InitializeComponent();
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		}

		public void Log(string message)
		{
			if (string.IsNullOrEmpty(message))
				return;

			_logBuilder.Append(message);

			_dispatcherQueue?.TryEnqueue(() =>
			{

				logTextBox.Text = _logBuilder.ToString();

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
