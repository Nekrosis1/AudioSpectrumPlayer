using AudioSpectrumPlayer.Avalonia.Logging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using System.Text;

namespace AudioSpectrumPlayer.Avalonia.ViewModels;

public partial class LogViewModel : ViewModelBase
{
	private readonly StringBuilder _logBuilder = new();

	public LogViewModel()
	{
		LogDisplaySink.LogReceived += OnLogReceived;
	}

	private void OnLogReceived(object? sender, string message)
	{
		Dispatcher.UIThread.Post(() =>
		{
			_logBuilder.Append(message);
			OnPropertyChanged(nameof(LogText));
		});
	}

	public string LogText => _logBuilder.ToString();

	[RelayCommand]
	public void ClearLog()
	{
		_logBuilder.Clear();
		OnPropertyChanged(nameof(LogText));
	}
}
