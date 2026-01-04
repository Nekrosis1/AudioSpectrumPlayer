using AudioSpectrumPlayer.Avalonia.Interfaces;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.Services
{
	public class AudioFileService : IAudioFileService
	{
		private static readonly string[] SupportedExtensions = [
			".mp3", ".mpeg", ".wav", ".m4a", ".wma",
			".aac", ".flac", ".ogg", ".aiff"
		];

		public async Task<string?> PickAudioFileAsync(Window window)
		{
			try
			{
				var storageProvider = window.StorageProvider;

				if (!storageProvider.CanOpen)
				{
					Log.Warning("File picker not supported on this platform");
					return null;
				}

				// Create file type filters
				var fileTypeFilters = new List<FilePickerFileType>
				{
					new FilePickerFileType("Audio Files")
					{
						Patterns = SupportedExtensions.Select(ext => $"*{ext}").ToList()
					},
					new FilePickerFileType("All Files")
					{
						Patterns = new List<string> { "*.*" }
					}
				};

				var options = new FilePickerOpenOptions
				{
					Title = "Select Audio File",
					AllowMultiple = false,
					FileTypeFilter = fileTypeFilters
				};

				var result = await storageProvider.OpenFilePickerAsync(options);

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

			var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
			return Array.Exists(SupportedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
		}
	}
}

