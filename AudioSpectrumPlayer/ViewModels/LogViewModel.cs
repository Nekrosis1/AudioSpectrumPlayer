using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;

namespace AudioSpectrumPlayer.ViewModels
{
	public class LogViewModel : ObservableObject
	{
		private StringBuilder _logBuilder = new();

		public void Log(string message)
		{
			// Centralized logging logic
			_logBuilder.Append(message);
			OnPropertyChanged(nameof(LogText));
		}

		public string LogText => _logBuilder.ToString();

		public void ClearLog()
		{
			_logBuilder.Clear();
			OnPropertyChanged(nameof(LogText));
		}
	}
}
