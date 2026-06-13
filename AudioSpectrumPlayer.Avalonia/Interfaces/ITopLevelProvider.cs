using Avalonia.Controls;

namespace AudioSpectrumPlayer.Avalonia.Interfaces
{
	/// <summary>
	/// Supplies the application's active <see cref="TopLevel"/> (the window) to
	/// services that need View-layer facilities such as the file-picker
	/// <c>StorageProvider</c>, without those services depending on a concrete
	/// <see cref="Window"/>. The window does not exist when the DI container is
	/// built, so the value starts null and is populated at startup.
	/// </summary>
	public interface ITopLevelProvider
	{
		TopLevel? TopLevel { get; }
	}
}
