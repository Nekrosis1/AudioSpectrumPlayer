using System.Text;

namespace AudioSpectrumPlayer.ViewModels;

public partial class LogViewModel : ObservableObject
{
    private readonly StringBuilder _logBuilder = new();

    public void Log(string message)
    {
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
