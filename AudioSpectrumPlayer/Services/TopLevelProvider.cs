using AudioSpectrumPlayer.Interfaces;
using Avalonia.Controls;

namespace AudioSpectrumPlayer.Services
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
