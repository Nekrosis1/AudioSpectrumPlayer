using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text;

namespace AudioSpectrumPlayer
{
	public class LogDisplay : UserControl
	{
		private TextBox logTextBox;
		private StringBuilder logBuilder = new StringBuilder();
		private ScrollViewer scrollViewer;

		public LogDisplay()
		{
			this.DefaultStyleKey = typeof(LogDisplay);

			// Create the ScrollViewer for scrollable content
			scrollViewer = new ScrollViewer
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
			};

			// Create the TextBox for displaying logs
			logTextBox = new TextBox
			{
				AcceptsReturn = true,
				IsReadOnly = true,
				TextWrapping = TextWrapping.Wrap,
				FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
				FontSize = 12,
				MinHeight = 100
			};

			scrollViewer.Content = logTextBox;
			this.Content = scrollViewer;
		}

		public void Log(string message)
		{
			if (string.IsNullOrEmpty(message))
				return;

			string timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
			string logMessage = $"[{timeStamp}] {message}{Environment.NewLine}";

			logBuilder.Append(logMessage);

			// Update UI on UI thread
			DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
			{
				logTextBox.Text = logBuilder.ToString();

				// Auto-scroll to the bottom
				logTextBox.Select(logTextBox.Text.Length, 0);
				logTextBox.Focus(FocusState.Programmatic);
				scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
			});
		}

		public void Clear()
		{
			logBuilder.Clear();
			DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
			{
				logTextBox.Text = string.Empty;
			});
		}
	}
}