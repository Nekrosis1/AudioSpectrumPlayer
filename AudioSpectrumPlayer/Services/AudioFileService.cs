using AudioSpectrumPlayer.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AudioSpectrumPlayer.Services
{
	public class AudioFileService : IAudioFileService
	{
		private static readonly string[] SupportedExtensions = [
			".mp3", ".mpeg", ".wav", ".m4a", ".wma",
			".aac", ".flac", ".ogg", ".aiff"
		];

		public async Task<string?> PickAudioFileAsync(nint windowHandle)
		{
			try
			{
				var picker = new FileOpenPicker();

				// Initialize with window handle for WinUI3
				WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

				foreach (string extension in SupportedExtensions)
				{
					picker.FileTypeFilter.Add(extension);
				}

				StorageFile? file = await picker.PickSingleFileAsync();

				if (file != null)
				{
					Log.Information($"File selected: {file.Path}");
					return file.Path;
				}
				else
				{
					Log.Warning("File selection canceled or failed");
					return null;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error picking audio file");
				return null;
			}
		}

		public bool IsValidAudioFile(string filePath)
		{
			if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
			{
				return false;
			}

			var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
			return Array.Exists(SupportedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
		}
	}
}

