using AudioSpectrumPlayer.Avalonia.Logging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;

namespace AudioSpectrumPlayer.Avalonia.ViewModels;

public partial class LogViewModel : ViewModelBase, IDisposable
{
	private readonly StringBuilder _logBuilder = new();
	private bool _disposed;

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

	// LogReceived is a static event, so it outlives every instance: without this
	// unsubscribe a recreated LogViewModel would be pinned alive for the whole
	// process. Disposed when the DI host is torn down at shutdown.
	public void Dispose()
	{
		if (_disposed) return;
		LogDisplaySink.LogReceived -= OnLogReceived;
		_disposed = true;
	}
}
