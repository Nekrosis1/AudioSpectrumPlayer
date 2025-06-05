using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text;

namespace AudioSpectrumPlayer.Presentation;

public sealed partial class LogDisplay : UserControl
{
    private readonly StringBuilder _logBuilder = new();
    private DispatcherQueue _dispatcherQueue;
    public LogViewModel ViewModel { get; private set; }

    public LogDisplay()
    {
        this.InitializeComponent();
        // Note: In Uno, we'll need to get the LogViewModel from DI differently
        // For now, create a simple instance - we'll improve this later
        ViewModel = new LogViewModel();
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

            // Auto-scroll to the bottom
            logTextBox.Select(logTextBox.Text.Length, 0);
            logTextBox.Focus(FocusState.Programmatic);
            scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
        });
    }

    public void Clear()
    {
        _logBuilder.Clear();
        ViewModel.ClearLog();
        logTextBox.Text = string.Empty;
    }
}
