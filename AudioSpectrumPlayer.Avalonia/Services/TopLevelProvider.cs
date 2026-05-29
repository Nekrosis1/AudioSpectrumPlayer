using AudioSpectrumPlayer.Avalonia.Interfaces;
using Avalonia.Controls;

namespace AudioSpectrumPlayer.Avalonia.Services
{
	/// <summary>
	/// Default <see cref="ITopLevelProvider"/> implementation: a simple holder.
	/// <see cref="App"/> assigns <see cref="TopLevel"/> once the main window has
	/// been created. Consumers receive it as the read-only <see cref="ITopLevelProvider"/>.
	/// </summary>
	public class TopLevelProvider : ITopLevelProvider
	{
		public TopLevel? TopLevel { get; set; }
	}
}
