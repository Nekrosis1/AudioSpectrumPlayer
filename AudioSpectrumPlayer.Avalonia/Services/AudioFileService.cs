using AudioSpectrumPlayer.Avalonia.Interfaces;
using Avalonia.Platform.Storage;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.Services
{
	public class AudioFileService(ITopLevelProvider topLevelProvider) : IAudioFileService
	{
		private static readonly string[] SupportedExtensions = [
			".mp3", ".mpeg", ".wav", ".m4a", ".wma",
			".aac", ".flac", ".ogg", ".aiff"
		];

		private readonly ITopLevelProvider _topLevelProvider = topLevelProvider;

		public async Task<string?> PickAudioFileAsync()
		{
			try
			{
				IStorageProvider? storageProvider = _topLevelProvider.TopLevel?.StorageProvider;

				if (storageProvider is null)
				{
					Log.Warning("File picker unavailable: no active window/TopLevel");
					return null;
				}

				if (!storageProvider.CanOpen)
				{
					Log.Warning("File picker not supported on this platform");
					return null;
				}

				// Create file type filters
				List<FilePickerFileType> fileTypeFilters =
				[
					new("Audio Files")
					{
						Patterns = [.. SupportedExtensions.Select(ext => $"*{ext}")]
					},
					new("All Files")
					{
						Patterns = ["*.*"]
					}
				];

				FilePickerOpenOptions options = new()
				{
					Title = "Select Audio File",
					AllowMultiple = false,
					FileTypeFilter = fileTypeFilters
				};

				IReadOnlyList<IStorageFile> result = await storageProvider.OpenFilePickerAsync(options);

				if (result != null && result.Count > 0)
				{
					var filePath = result[0].Path.LocalPath;
					Log.Information($"File selected: {filePath}");
					return filePath;
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

			string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
			return Array.Exists(SupportedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
		}
	}
}

